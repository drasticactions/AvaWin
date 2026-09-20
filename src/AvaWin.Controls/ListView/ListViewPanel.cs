using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaWin.Controls;

/// <summary>A group of consecutive items, with its header data.</summary>
public sealed class ListViewGroupInfo
{
    internal ListViewGroupInfo(object? key, object? header, int start, int count)
    {
        Key = key;
        Header = header;
        Start = start;
        Count = count;
    }

    /// <summary>The group key returned by <see cref="ListView.GroupKeySelector"/>.</summary>
    public object? Key { get; }

    /// <summary>The header data: the matching item of <see cref="ListView.GroupsSource"/>, or the key.</summary>
    public object? Header { get; }

    /// <summary>The index of the first item.</summary>
    public int Start { get; }

    /// <summary>The number of items.</summary>
    public int Count { get; }
}

/// <summary>
/// The virtualizing panel behind every ListView layout. In uniform mode (<see cref="ListLayout"/>,
/// <c>GridLayout</c>) every item has the same cell, <see cref="MaxItemsPerLine"/> items fill a line, lines wrap along
/// the cross axis, and groups are consecutive blocks with a header and a 70px leader gap. Positions are index
/// arithmetic. In cell-spanning mode (<c>CellSpanningLayout</c>) each item spans whole multiples of the cell of its
/// group and is packed first-fit along the scroll axis. Only the items in the viewport, plus a buffer, are realized.
/// </summary>
public sealed class ListViewPanel : VirtualizingPanel
{
    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<ListViewPanel, Orientation>(nameof(Orientation), Orientation.Vertical);

    /// <summary>Defines the <see cref="MaxItemsPerLine"/> property.</summary>
    public static readonly StyledProperty<int> MaxItemsPerLineProperty = AvaloniaProperty.Register<ListViewPanel, int>(nameof(MaxItemsPerLine), 1);

    /// <summary>Defines the <see cref="StretchCrossAxis"/> property.</summary>
    public static readonly StyledProperty<bool> StretchCrossAxisProperty = AvaloniaProperty.Register<ListViewPanel, bool>(nameof(StretchCrossAxis), true);

    /// <summary>Defines the <see cref="GroupLeaderMargin"/> property.</summary>
    public static readonly StyledProperty<double> GroupLeaderMarginProperty = AvaloniaProperty.Register<ListViewPanel, double>(nameof(GroupLeaderMargin), 70);

    /// <summary>Defines the <see cref="HeaderPosition"/> property.</summary>
    public static readonly StyledProperty<GroupHeaderPosition> HeaderPositionProperty = AvaloniaProperty.Register<ListViewPanel, GroupHeaderPosition>(nameof(HeaderPosition));

    private static readonly AttachedProperty<object?> RecycleKeyProperty = AvaloniaProperty.RegisterAttached<ListViewPanel, Control, object?>("RecycleKey");
    private static readonly object OwnContainer = new();

    private readonly SortedDictionary<int, Control> _realized = new();
    private readonly Dictionary<int, Control> _headers = new();
    private readonly Dictionary<object, Stack<Control>> _pool = new();
    private readonly Stack<Control> _headerPool = new();
    private IReadOnlyList<ListViewGroupInfo>? _groups;
    private Func<ListViewGroupInfo, Control>? _headerFactory;
    private Func<int, GridItemInfo>? _itemInfo;
    private Func<int, GroupInfo>? _groupInfo;
    private bool _cellSpanning;
    private Rect _viewport = new(0, 0, double.PositiveInfinity, double.PositiveInfinity);
    private Size _cell;
    private Size _headerSize;
    private double _crossAvail = double.NaN;
    private int _perLine = 1;
    private double[] _groupStarts = [];
    private double[] _groupEnds = [];
    private double _extentMain;
    private double _extentCross;
    private (double main, double cross, double mainLen, double crossLen)[] _rects = [];

    static ListViewPanel()
    {
        AffectsMeasure<ListViewPanel>(OrientationProperty, MaxItemsPerLineProperty, StretchCrossAxisProperty, GroupLeaderMarginProperty, HeaderPositionProperty);
    }

    /// <summary>Initializes a new instance.</summary>
    public ListViewPanel()
    {
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    /// <summary>The scroll direction.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The maximum number of items per line. 1 means a list. 0 means as many as fit.</summary>
    public int MaxItemsPerLine { get => GetValue(MaxItemsPerLineProperty); set => SetValue(MaxItemsPerLineProperty, value); }

    /// <summary>Whether items fill the cross axis (a list) or keep their uniform size (a grid).</summary>
    public bool StretchCrossAxis { get => GetValue(StretchCrossAxisProperty); set => SetValue(StretchCrossAxisProperty, value); }

    /// <summary>The gap before every group after the first.</summary>
    public double GroupLeaderMargin { get => GetValue(GroupLeaderMarginProperty); set => SetValue(GroupLeaderMarginProperty, value); }

    /// <summary>Where group headers sit: above their group, or to the left of it.</summary>
    public GroupHeaderPosition HeaderPosition { get => GetValue(HeaderPositionProperty); set => SetValue(HeaderPositionProperty, value); }

    /// <summary>The uniform cell size after the last measure.</summary>
    public Size CellSize => _cell;

    /// <summary>The number of items per line after the last measure (uniform mode).</summary>
    public int ItemsPerLine => _perLine;

    /// <summary>The realized group headers.</summary>
    public IReadOnlyDictionary<int, Control> RealizedHeaders => _headers;

    /// <summary>The number of realized item containers.</summary>
    public int RealizedCount => _realized.Count;

    /// <summary>Whether the panel packs items of different sizes (cell-spanning mode).</summary>
    public bool IsCellSpanning => _cellSpanning;

    /// <summary>
    /// A header is leading when it sits before its group along the scroll axis (vertical and Top, or horizontal and
    /// Left). Otherwise it is a band across the leading cross edge, and every group starts after that band.
    /// </summary>
    private bool HeaderIsLeading => (Orientation == Orientation.Vertical) == (HeaderPosition == GroupHeaderPosition.Top);

    internal void Configure(IReadOnlyList<ListViewGroupInfo>? groups, Func<ListViewGroupInfo, Control>? headerFactory, Func<int, GridItemInfo>? itemInfo, Func<int, GroupInfo>? groupInfo, bool cellSpanning)
    {
        _groups = groups;
        _headerFactory = headerFactory;
        _itemInfo = itemInfo;
        _groupInfo = groupInfo;
        _cellSpanning = cellSpanning && itemInfo is not null && groupInfo is not null;
        _cell = default;
        RecycleAll();
        InvalidateMeasure();
    }

    /// <summary>Realizes the item at <paramref name="index"/>, scrolls it into view and returns its container.</summary>
    public Control? BringIndexIntoView(int index) => ScrollIntoView(index);

    /// <summary>Measures the uniform cell again.</summary>
    public void Recalculate()
    {
        _cell = default;
        RecycleAll();
        InvalidateMeasure();
    }

    /// <summary>The rectangle of the item at <paramref name="index"/> in panel coordinates.</summary>
    public Rect RectOf(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return default;
        }

        if (_cellSpanning)
        {
            if (index >= _rects.Length)
            {
                return default;
            }

            var r = _rects[index];
            return ToRect(r.main, r.cross, r.mainLen, r.crossLen);
        }

        if (_perLine <= 0)
        {
            return default;
        }

        var (g, local) = Locate(index);
        var line = local / _perLine;
        var slot = local % _perLine;
        var main = GroupItemsStart(g) + line * Main(_cell);
        var cross = ItemsCrossOrigin() + slot * Cross(_cell);
        return ToRect(main, cross, Main(_cell), Cross(_cell));
    }

    /// <summary>The index of the item nearest to a point along the scroll axis, or -1.</summary>
    public int IndexAt(Point point)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return -1;
        }

        var main = Orientation == Orientation.Vertical ? point.Y : point.X;
        var cross = Orientation == Orientation.Vertical ? point.X : point.Y;
        if (_cellSpanning)
        {
            var best = -1;
            var bestDist = double.MaxValue;
            for (var i = 0; i < _rects.Length; i++)
            {
                var r = _rects[i];
                var dm = main < r.main ? r.main - main : main > r.main + r.mainLen ? main - (r.main + r.mainLen) : 0;
                var dc = cross < r.cross ? r.cross - cross : cross > r.cross + r.crossLen ? cross - (r.cross + r.crossLen) : 0;
                var d = dm * 4 + dc;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            return best;
        }

        if (Main(_cell) <= 0)
        {
            return -1;
        }

        var g = 0;
        if (_groups is { Count: > 0 })
        {
            for (var i = 1; i < _groups.Count; i++)
            {
                if (_groupStarts[i] <= main)
                {
                    g = i;
                }
            }
        }

        var start = _groups is { Count: > 0 } ? _groups[g].Start : 0;
        var gcount = _groups is { Count: > 0 } ? _groups[g].Count : count;
        var line = (int)Math.Floor((main - GroupItemsStart(g)) / Main(_cell));
        var slot = Cross(_cell) > 0 ? (int)Math.Floor((cross - ItemsCrossOrigin()) / Cross(_cell)) : 0;
        var local = Math.Clamp(line, 0, Math.Max(0, (gcount - 1) / _perLine)) * _perLine + Math.Clamp(slot, 0, _perLine - 1);
        return Math.Clamp(start + Math.Min(local, gcount - 1), 0, count - 1);
    }

    /// <summary>The index a key moves focus to. A grid moves by lines along the scroll axis. Cell spanning moves by geometry.</summary>
    public int NextIndex(int from, NavigationDirection direction)
    {
        var count = Items.Count;
        if (count == 0)
        {
            return -1;
        }

        if (from < 0)
        {
            return 0;
        }

        var vertical = Orientation == Orientation.Vertical;
        var viewMain = vertical ? _viewport.Height : _viewport.Width;
        if (double.IsInfinity(viewMain))
        {
            viewMain = 0;
        }

        if (_cellSpanning)
        {
            return direction switch
            {
                NavigationDirection.Next => Math.Min(from + 1, count - 1),
                NavigationDirection.Previous => Math.Max(from - 1, 0),
                NavigationDirection.First => 0,
                NavigationDirection.Last => count - 1,
                NavigationDirection.PageDown => NearestInDirection(from, +1, 0, viewMain),
                NavigationDirection.PageUp => NearestInDirection(from, -1, 0, viewMain),
                NavigationDirection.Down => vertical ? NearestInDirection(from, +1, 0, 0) : NearestInDirection(from, 0, +1, 0),
                NavigationDirection.Up => vertical ? NearestInDirection(from, -1, 0, 0) : NearestInDirection(from, 0, -1, 0),
                NavigationDirection.Right => vertical ? NearestInDirection(from, 0, +1, 0) : NearestInDirection(from, +1, 0, 0),
                NavigationDirection.Left => vertical ? NearestInDirection(from, 0, -1, 0) : NearestInDirection(from, -1, 0, 0),
                _ => from,
            };
        }

        var pageLines = Math.Max(1, (int)Math.Floor(viewMain / Math.Max(1, Main(_cell))));
        var next = direction switch
        {
            NavigationDirection.Next => from + 1,
            NavigationDirection.Previous => from - 1,
            NavigationDirection.First => 0,
            NavigationDirection.Last => count - 1,
            NavigationDirection.Down => vertical ? from + _perLine : from + 1,
            NavigationDirection.Up => vertical ? from - _perLine : from - 1,
            NavigationDirection.Right => vertical ? from + 1 : from + _perLine,
            NavigationDirection.Left => vertical ? from - 1 : from - _perLine,
            NavigationDirection.PageDown => from + pageLines * _perLine,
            NavigationDirection.PageUp => from - pageLines * _perLine,
            _ => from,
        };
        return Math.Clamp(next, 0, count - 1);
    }

    /// <summary>The insertion index for a drop at <paramref name="point"/>: before the nearest item, or after it when the point is past its middle.</summary>
    public int InsertionIndexAt(Point point)
    {
        var index = IndexAt(point);
        if (index < 0)
        {
            return 0;
        }

        var r = RectOf(index);
        var main = Orientation == Orientation.Vertical ? point.Y : point.X;
        var mid = Orientation == Orientation.Vertical ? r.Y + r.Height / 2 : r.X + r.Width / 2;
        var perLine = _cellSpanning ? 1 : _perLine;
        if (perLine > 1)
        {
            var cross = Orientation == Orientation.Vertical ? point.X : point.Y;
            var cmid = Orientation == Orientation.Vertical ? r.X + r.Width / 2 : r.Y + r.Height / 2;
            return cross > cmid ? index + 1 : index;
        }

        return main > mid ? index + 1 : index;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var items = Items;
        var count = items.Count;
        if (count == 0 || ItemContainerGenerator is null)
        {
            RecycleAll();
            _extentMain = 0;
            _extentCross = 0;
            _groupStarts = [];
            _groupEnds = [];
            _rects = [];
            return default;
        }

        var vertical = Orientation == Orientation.Vertical;
        var crossAvail = vertical ? availableSize.Width : availableSize.Height;
        if (double.IsInfinity(crossAvail))
        {
            crossAvail = double.NaN;
        }

        if (!Equals(crossAvail, _crossAvail))
        {
            _crossAvail = crossAvail;
            if (StretchCrossAxis)
            {
                _cell = default;
            }
        }

        EnsureHeaderSize(crossAvail);
        var grouped = _groups is { Count: > 0 };
        var bandCross = grouped && !HeaderIsLeading ? Cross(_headerSize) : 0;
        var itemsCross = double.IsNaN(crossAvail) ? double.NaN : Math.Max(0, crossAvail - bandCross);

        if (_cellSpanning)
        {
            MeasureCellSpanning(items, itemsCross, bandCross);
        }
        else
        {
            MeasureUniform(items, itemsCross, bandCross);
        }

        RealizeVisible(items, crossAvail);
        var extentCross = StretchCrossAxis && !double.IsNaN(crossAvail) ? crossAvail : _extentCross;
        return vertical ? new Size(extentCross, _extentMain) : new Size(_extentMain, extentCross);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var (index, control) in _realized)
        {
            control.Arrange(RectOf(index));
        }

        if (_groups is { Count: > 0 })
        {
            foreach (var (g, header) in _headers)
            {
                header.Arrange(HeaderRect(g, finalSize));
            }
        }

        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(items, e);
        RecycleAll();
        InvalidateMeasure();
    }

    /// <inheritdoc/>
    protected override void OnItemsControlChanged(ItemsControl? oldValue)
    {
        base.OnItemsControlChanged(oldValue);
        RecycleAll();
        _pool.Clear();
        _cell = default;
    }

    /// <inheritdoc/>
    protected override Control? ScrollIntoView(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return null;
        }

        var rect = RectOf(index);
        if (!_viewport.Contains(rect) && ItemsControl is { } owner)
        {
            this.BringIntoView(rect);
            owner.UpdateLayout();
        }

        var element = GetOrCreate(Items, index);
        UpdateLayout();
        return element;
    }

    /// <inheritdoc/>
    protected override Control? ContainerFromIndex(int index) => _realized.TryGetValue(index, out var c) ? c : null;

    /// <inheritdoc/>
    protected override int IndexFromContainer(Control container)
    {
        foreach (var (index, c) in _realized)
        {
            if (ReferenceEquals(c, container))
            {
                return index;
            }
        }

        return -1;
    }

    /// <inheritdoc/>
    protected override IEnumerable<Control>? GetRealizedContainers() => _realized.Values;

    /// <inheritdoc/>
    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var index = from is Control c ? IndexFromContainer(c) : -1;
        var next = NextIndex(index, direction);
        return next < 0 ? null : ScrollIntoView(next);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Uniform layout
    // ---------------------------------------------------------------------------------------------------------

    private void MeasureUniform(IReadOnlyList<object?> items, double itemsCross, double bandCross)
    {
        EnsureCell(items, itemsCross);
        var cellCross = Cross(_cell);
        var cellMain = Main(_cell);
        _perLine = MaxItemsPerLine == 1 || cellCross <= 0 || double.IsNaN(itemsCross)
            ? 1
            : Math.Max(1, (int)Math.Floor(itemsCross / cellCross));
        if (MaxItemsPerLine > 1)
        {
            _perLine = Math.Min(_perLine, MaxItemsPerLine);
        }

        var leading = HeaderIsLeading ? Main(_headerSize) : 0;
        if (_groups is { Count: > 0 })
        {
            _groupStarts = new double[_groups.Count];
            _groupEnds = new double[_groups.Count];
            var main = 0d;
            for (var g = 0; g < _groups.Count; g++)
            {
                if (g > 0)
                {
                    main += GroupLeaderMargin;
                }

                _groupStarts[g] = main;
                var lines = (int)Math.Ceiling(_groups[g].Count / (double)_perLine);
                main += leading + lines * cellMain;
                _groupEnds[g] = main;
            }

            _extentMain = main;
        }
        else
        {
            _groupStarts = [0];
            _groupEnds = [Math.Ceiling(items.Count / (double)_perLine) * cellMain];
            _extentMain = _groupEnds[0];
        }

        _extentCross = bandCross + _perLine * cellCross;
        _rects = [];
    }

    private void EnsureCell(IReadOnlyList<object?> items, double crossAvail)
    {
        if (_itemInfo is { } info)
        {
            var i = info(0);
            _cell = new Size(i.Width, i.Height);
            return;
        }

        if (_cell.Width > 0 && _cell.Height > 0)
        {
            return;
        }

        var probe = GetOrCreate(items, 0);
        var measureSize = StretchCrossAxis && !double.IsNaN(crossAvail)
            ? (Orientation == Orientation.Vertical ? new Size(crossAvail, double.PositiveInfinity) : new Size(double.PositiveInfinity, crossAvail))
            : Size.Infinity;
        probe.Measure(measureSize);
        var d = probe.DesiredSize;
        _cell = StretchCrossAxis && !double.IsNaN(crossAvail)
            ? (Orientation == Orientation.Vertical ? new Size(crossAvail, d.Height) : new Size(d.Width, crossAvail))
            : d;
        if (Main(_cell) <= 0)
        {
            _cell = Orientation == Orientation.Vertical ? new Size(_cell.Width, 1) : new Size(1, _cell.Height);
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Cell-spanning layout
    // ---------------------------------------------------------------------------------------------------------

    private void MeasureCellSpanning(IReadOnlyList<object?> items, double itemsCross, double bandCross)
    {
        var count = items.Count;
        _rects = new (double, double, double, double)[count];
        var groups = _groups is { Count: > 0 } ? _groups : new[] { new ListViewGroupInfo(null, null, 0, count) };
        _groupStarts = new double[groups.Count];
        _groupEnds = new double[groups.Count];
        var leading = HeaderIsLeading && _groups is { Count: > 0 } ? Main(_headerSize) : 0;
        var main = 0d;
        var maxCross = 0d;
        for (var g = 0; g < groups.Count; g++)
        {
            if (g > 0)
            {
                main += GroupLeaderMargin;
            }

            _groupStarts[g] = main;
            var group = groups[g];
            var gi = _groupInfo!(g);
            var cellMain = Orientation == Orientation.Vertical ? gi.CellHeight : gi.CellWidth;
            var cellCross = Orientation == Orientation.Vertical ? gi.CellWidth : gi.CellHeight;
            if (cellMain <= 0 || cellCross <= 0)
            {
                cellMain = Math.Max(1, cellMain);
                cellCross = Math.Max(1, cellCross);
            }

            var slots = double.IsNaN(itemsCross) ? int.MaxValue / 2 : Math.Max(1, (int)Math.Floor(itemsCross / cellCross));
            if (MaxItemsPerLine > 0)
            {
                slots = Math.Min(slots, MaxItemsPerLine);
            }

            _cell = Orientation == Orientation.Vertical ? new Size(cellCross, cellMain) : new Size(cellMain, cellCross);
            var map = new OccupancyMap(slots);
            var itemsStart = main + leading;
            for (var k = 0; k < group.Count; k++)
            {
                var index = group.Start + k;
                var info = _itemInfo!(index);
                var wMain = Orientation == Orientation.Vertical ? info.Height : info.Width;
                var wCross = Orientation == Orientation.Vertical ? info.Width : info.Height;
                var spanMain = Math.Max(1, (int)Math.Ceiling(wMain / cellMain));
                var spanCross = Math.Max(1, (int)Math.Ceiling(wCross / cellCross));
                spanCross = Math.Min(spanCross, slots);
                var (line, slot) = map.Place(spanMain, spanCross);
                _rects[index] = (itemsStart + line * cellMain, bandCross + slot * cellCross, spanMain * cellMain, spanCross * cellCross);
                maxCross = Math.Max(maxCross, bandCross + (slot + spanCross) * cellCross);
            }

            main = itemsStart + map.Lines * cellMain;
            _groupEnds[g] = main;
        }

        _extentMain = main;
        _extentCross = maxCross;
        _perLine = 1;
    }

    /// <summary>Lines along the scroll axis by slots across the other axis, with first-fit placement.</summary>
    private sealed class OccupancyMap
    {
        private readonly int _slots;
        private readonly List<bool[]> _lines = new();
        private int _lastLine;
        private int _lastSlot;

        public OccupancyMap(int slots)
        {
            _slots = slots;
        }

        public int Lines => _lines.Count;

        public (int line, int slot) Place(int spanMain, int spanCross)
        {
            // Scan from the last placed cell onwards and take the first fit.
            for (var line = _lastLine; ; line++)
            {
                for (var slot = line == _lastLine ? _lastSlot : 0; slot + spanCross <= _slots; slot++)
                {
                    if (IsFree(line, slot, spanMain, spanCross))
                    {
                        Mark(line, slot, spanMain, spanCross);
                        _lastLine = line;
                        _lastSlot = slot;
                        return (line, slot);
                    }
                }
            }
        }

        private bool IsFree(int line, int slot, int spanMain, int spanCross)
        {
            for (var l = line; l < line + spanMain; l++)
            {
                if (l >= _lines.Count)
                {
                    return true;
                }

                for (var s = slot; s < slot + spanCross && s < _lines[l].Length; s++)
                {
                    if (_lines[l][s])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void Mark(int line, int slot, int spanMain, int spanCross)
        {
            while (_lines.Count < line + spanMain)
            {
                _lines.Add(new bool[_slots == int.MaxValue / 2 ? 1024 : _slots]);
            }

            for (var l = line; l < line + spanMain; l++)
            {
                for (var s = slot; s < slot + spanCross && s < _lines[l].Length; s++)
                {
                    _lines[l][s] = true;
                }
            }
        }
    }

    private int NearestInDirection(int from, int mainDir, int crossDir, double minDistance)
    {
        if (from >= _rects.Length)
        {
            return from;
        }

        var r = _rects[from];
        var cm = r.main + r.mainLen / 2;
        var cc = r.cross + r.crossLen / 2;
        var best = from;
        var bestScore = double.MaxValue;
        for (var i = 0; i < _rects.Length; i++)
        {
            if (i == from)
            {
                continue;
            }

            var o = _rects[i];
            var om = o.main + o.mainLen / 2;
            var oc = o.cross + o.crossLen / 2;
            var dm = om - cm;
            var dc = oc - cc;
            if (mainDir != 0 && (Math.Sign(dm) != mainDir || Math.Abs(dm) < minDistance))
            {
                continue;
            }

            if (crossDir != 0 && (Math.Sign(dc) != crossDir || Math.Abs(dm) > r.mainLen / 2 + o.mainLen / 2))
            {
                continue;
            }

            var score = mainDir != 0 ? Math.Abs(dm) * 4 + Math.Abs(dc) : Math.Abs(dc) * 4 + Math.Abs(dm);
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        return best;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Headers, realization and recycling
    // ---------------------------------------------------------------------------------------------------------

    private double GroupItemsStart(int g)
    {
        var start = _groupStarts.Length > g ? _groupStarts[g] : 0;
        return start + (_groups is { Count: > 0 } && HeaderIsLeading ? Main(_headerSize) : 0);
    }

    private double ItemsCrossOrigin() => _groups is { Count: > 0 } && !HeaderIsLeading ? Cross(_headerSize) : 0;

    private Rect HeaderRect(int g, Size finalSize)
    {
        var start = _groupStarts.Length > g ? _groupStarts[g] : 0;
        var end = _groupEnds.Length > g ? _groupEnds[g] : start;
        if (HeaderIsLeading)
        {
            var crossLen = Orientation == Orientation.Vertical ? finalSize.Width : finalSize.Height;
            return ToRect(start, 0, Main(_headerSize), crossLen);
        }

        return ToRect(start, 0, Math.Max(Main(_headerSize), end - start), Cross(_headerSize));
    }

    private void EnsureHeaderSize(double crossAvail)
    {
        if (_groups is not { Count: > 0 } || _headerFactory is null)
        {
            _headerSize = default;
            return;
        }

        var header = GetOrCreateHeader(0);
        header.Measure(Orientation == Orientation.Vertical
            ? new Size(double.IsNaN(crossAvail) ? double.PositiveInfinity : crossAvail, double.PositiveInfinity)
            : new Size(double.PositiveInfinity, double.IsNaN(crossAvail) ? double.PositiveInfinity : crossAvail));
        _headerSize = header.DesiredSize;
    }

    private void RealizeVisible(IReadOnlyList<object?> items, double crossAvail)
    {
        var vertical = Orientation == Orientation.Vertical;
        var viewStart = vertical ? _viewport.Top : _viewport.Left;
        var viewEnd = vertical ? _viewport.Bottom : _viewport.Right;
        if (double.IsInfinity(viewEnd))
        {
            viewEnd = _extentMain;
        }

        var buffer = Math.Max(Main(_cell), 1);
        var lo = Math.Max(0, viewStart - buffer);
        var hi = Math.Min(_extentMain, viewEnd + buffer);
        var wanted = new HashSet<int>();
        var wantedHeaders = new HashSet<int>();
        var groups = _groups is { Count: > 0 } ? _groups : null;

        if (_cellSpanning)
        {
            for (var i = 0; i < _rects.Length; i++)
            {
                var r = _rects[i];
                if (r.main + r.mainLen >= lo && r.main <= hi)
                {
                    wanted.Add(i);
                }
            }

            if (groups is not null)
            {
                for (var g = 0; g < groups.Count; g++)
                {
                    if (_groupEnds[g] >= lo && _groupStarts[g] <= hi)
                    {
                        wantedHeaders.Add(g);
                    }
                }
            }
        }
        else
        {
            var cellMain = Main(_cell);
            void Want(int start, int count, double blockStart)
            {
                var firstLine = Math.Max(0, (int)Math.Floor((lo - blockStart) / cellMain));
                var lastLine = (int)Math.Floor((hi - blockStart) / cellMain);
                var maxLine = (count - 1) / _perLine;
                if (lastLine < 0 || firstLine > maxLine)
                {
                    return;
                }

                lastLine = Math.Min(lastLine, maxLine);
                for (var line = firstLine; line <= lastLine; line++)
                {
                    for (var slot = 0; slot < _perLine; slot++)
                    {
                        var local = line * _perLine + slot;
                        if (local >= count)
                        {
                            break;
                        }

                        wanted.Add(start + local);
                    }
                }
            }

            if (groups is not null)
            {
                for (var g = 0; g < groups.Count; g++)
                {
                    if (_groupEnds[g] < lo || _groupStarts[g] > hi)
                    {
                        continue;
                    }

                    wantedHeaders.Add(g);
                    Want(groups[g].Start, groups[g].Count, GroupItemsStart(g));
                }
            }
            else
            {
                Want(0, items.Count, 0);
            }
        }

        foreach (var (index, control) in _realized)
        {
            if (control.IsKeyboardFocusWithin)
            {
                wanted.Add(index);
            }
        }

        foreach (var index in _realized.Keys.Where(i => !wanted.Contains(i)).ToList())
        {
            Recycle(index);
        }

        foreach (var g in _headers.Keys.Where(g => !wantedHeaders.Contains(g)).ToList())
        {
            RecycleHeader(g);
        }

        foreach (var index in wanted)
        {
            if (index >= items.Count)
            {
                continue;
            }

            var e = GetOrCreate(items, index);
            e.Measure(RectOf(index).Size);
        }

        foreach (var g in wantedHeaders)
        {
            var h = GetOrCreateHeader(g);
            var rect = HeaderRect(g, Bounds.Size == default ? (Orientation == Orientation.Vertical ? new Size(double.IsNaN(crossAvail) ? 0 : crossAvail, 0) : new Size(0, double.IsNaN(crossAvail) ? 0 : crossAvail)) : Bounds.Size);
            h.Measure(new Size(Math.Max(rect.Width, 0), Math.Max(rect.Height, 0)));
        }
    }

    private Control GetOrCreate(IReadOnlyList<object?> items, int index)
    {
        if (_realized.TryGetValue(index, out var existing))
        {
            return existing;
        }

        var generator = ItemContainerGenerator!;
        var item = items[index];
        Control element;
        if (generator.NeedsContainer(item, index, out var recycleKey))
        {
            if (recycleKey is not null && _pool.TryGetValue(recycleKey, out var stack) && stack.Count > 0)
            {
                element = stack.Pop();
                element.SetCurrentValue(IsVisibleProperty, true);
            }
            else
            {
                element = generator.CreateContainer(item, index, recycleKey);
            }

            element.SetValue(RecycleKeyProperty, recycleKey);
            generator.PrepareItemContainer(element, item, index);
            AddInternalChild(element);
            generator.ItemContainerPrepared(element, item, index);
        }
        else
        {
            element = (Control)item!;
            element.SetValue(RecycleKeyProperty, OwnContainer);
            if (element.GetVisualParent() != this)
            {
                AddInternalChild(element);
            }

            element.SetCurrentValue(IsVisibleProperty, true);
            generator.PrepareItemContainer(element, item, index);
            generator.ItemContainerPrepared(element, item, index);
        }

        _realized[index] = element;
        return element;
    }

    private void Recycle(int index)
    {
        if (!_realized.Remove(index, out var element))
        {
            return;
        }

        var key = element.GetValue(RecycleKeyProperty);
        if (ReferenceEquals(key, OwnContainer))
        {
            element.SetCurrentValue(IsVisibleProperty, false);
            return;
        }

        ItemContainerGenerator?.ClearItemContainer(element);
        RemoveInternalChild(element);
        if (key is not null)
        {
            if (!_pool.TryGetValue(key, out var stack))
            {
                _pool[key] = stack = new Stack<Control>();
            }

            element.SetCurrentValue(IsVisibleProperty, false);
            stack.Push(element);
        }
    }

    private Control GetOrCreateHeader(int groupIndex)
    {
        if (_headers.TryGetValue(groupIndex, out var h))
        {
            return h;
        }

        var group = _groups![groupIndex];
        Control header;
        if (_headerPool.Count > 0)
        {
            header = _headerPool.Pop();
            header.SetCurrentValue(IsVisibleProperty, true);
        }
        else
        {
            header = _headerFactory!(group);
        }

        if (header is ListViewGroupHeader lgh)
        {
            lgh.Bind(group, groupIndex);
        }

        AddInternalChild(header);
        _headers[groupIndex] = header;
        return header;
    }

    private void RecycleHeader(int groupIndex)
    {
        if (!_headers.Remove(groupIndex, out var header))
        {
            return;
        }

        RemoveInternalChild(header);
        header.SetCurrentValue(IsVisibleProperty, false);
        _headerPool.Push(header);
    }

    private void RecycleAll()
    {
        foreach (var index in _realized.Keys.ToList())
        {
            Recycle(index);
        }

        foreach (var g in _headers.Keys.ToList())
        {
            RecycleHeader(g);
        }
    }

    private (int group, int local) Locate(int index)
    {
        if (_groups is not { Count: > 0 })
        {
            return (0, index);
        }

        var lo = 0;
        var hi = _groups.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (_groups[mid].Start <= index)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return (lo, index - _groups[lo].Start);
    }

    private Rect ToRect(double main, double cross, double mainLen, double crossLen) =>
        Orientation == Orientation.Vertical ? new Rect(cross, main, crossLen, mainLen) : new Rect(main, cross, mainLen, crossLen);

    private double Main(Size s) => Orientation == Orientation.Vertical ? s.Height : s.Width;

    private double Cross(Size s) => Orientation == Orientation.Vertical ? s.Width : s.Height;

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var vp = e.EffectiveViewport.Intersect(new Rect(Bounds.Size));
        if (vp != _viewport)
        {
            _viewport = vp;
            InvalidateMeasure();
        }
    }
}
