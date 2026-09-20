#!/usr/bin/env python3
"""Generates src/AvaWin.Theme/Glyphs/AppBarIcon.cs and AppBarIcons.cs from appBarIcons.json.

Usage: python3 tools/gen-appbar-icons.py [path/to/winjs/src/strings/en-us/appBarIcons.json]
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
DEFAULT_JSON = "/Users/da/Developer/Personal/winjs/src/strings/en-us/appBarIcons.json"

# The icon names in the JSON are lowercase without separators. This table gives the word breaks that cannot be
# inferred. A name that is not listed only gets its first letter capitalized.
WORD_BREAKS = {
    "fourbars": "FourBars", "likedislike": "LikeDislike", "movetofolder": "MoveToFolder", "postupdate": "PostUpdate",
    "repeatall": "RepeatAll", "repeatone": "RepeatOne", "reporthacked": "ReportHacked", "showresults": "ShowResults",
    "slideshow": "SlideShow", "stopslideshow": "StopSlideShow", "threebars": "ThreeBars", "uploadskydrive": "UploadSkyDrive",
    "zerobars": "ZeroBars", "onebar": "OneBar", "twobars": "TwoBars", "openinnewwindow": "OpenInNewWindow",
    "hangup": "HangUp", "twopage": "TwoPage", "onepage": "OnePage", "mailreplyall": "MailReplyAll",
    "addfriend": "AddFriend", "aligncenter": "AlignCenter", "alignleft": "AlignLeft", "alignright": "AlignRight",
    "allapps": "AllApps", "attachcamera": "AttachCamera", "backtowindow": "BackToWindow", "blockcontact": "BlockContact",
    "browsephotos": "BrowsePhotos", "calendarday": "CalendarDay", "calendarreply": "CalendarReply",
    "calendarweek": "CalendarWeek", "cellphone": "CellPhone", "clearselection": "ClearSelection", "closepane": "ClosePane",
    "contactinfo": "ContactInfo", "contactpresence": "ContactPresence", "disableupdates": "DisableUpdates",
    "disconnectdrive": "DisconnectDrive", "dockbottom": "DockBottom", "dockleft": "DockLeft", "dockright": "DockRight",
    "fontcolor": "FontColor", "fontdecrease": "FontDecrease", "fontincrease": "FontIncrease", "fontsize": "FontSize",
    "fullscreen": "FullScreen", "globalnavbutton": "GlobalNavButton", "hangup": "HangUp", "highlight": "Highlight",
    "importall": "ImportAll", "leavechat": "LeaveChat", "mailfilled": "MailFilled", "mailforward": "MailForward",
    "mailreply": "MailReply", "mailreplyall": "MailReplyAll", "mapdrive": "MapDrive", "mappin": "MapPin",
    "moviecamera": "MovieCamera", "musicinfo": "MusicInfo", "mutedchat": "MutedChat", "newfolder": "NewFolder",
    "newwindow": "NewWindow", "openfile": "OpenFile", "openlocal": "OpenLocal", "openpane": "OpenPane",
    "openwith": "OpenWith", "otheruser": "OtherUser", "outlinestar": "OutlineStar", "pagesolid": "PageSolid",
    "phonebook": "PhoneBook", "priority": "Priority", "protectedocument": "ProtectedDocument",
    "protecteddocument": "ProtectedDocument", "readingmode": "ReadingMode", "rotatecamera": "RotateCamera",
    "savelocal": "SaveLocal", "selectall": "SelectAll", "setlockscreen": "SetLockScreen", "settile": "SetTile",
    "solidstar": "SolidStar", "switchapps": "SwitchApps", "textalign": "TextAlign", "touchpointer": "TouchPointer",
    "twopage": "TwoPage", "unfavorite": "Unfavorite", "unpin": "Unpin", "viewall": "ViewAll", "webcam": "Webcam",
    "zoomin": "ZoomIn", "zoomout": "ZoomOut", "keyboardclassic": "KeyboardClassic", "keyboardleftalign": "KeyboardLeftAlign",
    "keyboardrightalign": "KeyboardRightAlign", "keyboardsplit": "KeyboardSplit", "keyboardstandard": "KeyboardStandard",
    "keyboardfull": "KeyboardFull", "keyboardleftdock": "KeyboardLeftDock", "keyboardrightdock": "KeyboardRightDock",
    "backspace": "Backspace", "closedcaption": "ClosedCaption", "videochat": "VideoChat", "worldclock": "WorldClock",
    "wifi": "Wifi", "bluetooth": "Bluetooth", "clearchat": "ClearChat", "newchat": "NewChat", "characters": "Characters",
    "dislike": "Dislike", "emoji2": "Emoji2", "contact2": "Contact2", "sortlist": "SortList", "previewlink": "PreviewLink",
    "streetview": "StreetView", "chromeback": "ChromeBack", "gotostart": "GoToStart", "gototoday": "GoToToday",
    "bulletedlist": "BulletedList", "openinnewwindow": "OpenInNewWindow", "hidebcc": "HideBcc", "showbcc": "ShowBcc",
    "sendtogroup": "SendToGroup", "addto": "AddTo", "removefrom": "RemoveFrom", "radiobtnoff": "RadioBtnOff",
    "radiobtnon": "RadioBtnOn", "radiobullet": "RadioBullet", "multiselect": "MultiSelect", "listmirrored": "ListMirrored",
    "mailreplymirrored": "MailReplyMirrored", "mailforwardmirrored": "MailForwardMirrored",
    "mailreplyallmirrored": "MailReplyAllMirrored", "contactpresence": "ContactPresence", "unsyncfolder": "UnsyncFolder",
    "syncfolder": "SyncFolder", "reportcontact": "ReportContact", "removeuser": "RemoveUser", "browsephotos": "BrowsePhotos",
    "hangup": "HangUp", "importantbadge": "ImportantBadge", "italic": "Italic", "underline": "Underline",
    "musicinfo": "MusicInfo", "openfile": "OpenFile", "openlocal": "OpenLocal", "savelocal": "SaveLocal",
    "unlock": "Unlock", "undo": "Undo", "redo": "Redo", "addfriend": "AddFriend", "video": "Video", "audio": "Audio",
    "readingmode": "ReadingMode", "presence": "Presence", "gototoday": "GoToToday", "help": "Help",
    "newfolder": "NewFolder", "trim": "Trim", "font": "Font", "mapdrive": "MapDrive",
    "nextprevious": "NextPrevious", "previewlink": "PreviewLink", "reshare": "Reshare", "leavechat": "LeaveChat",
    "openpane": "OpenPane", "sortlist": "SortList", "leavechat": "LeaveChat", "clearchat": "ClearChat",
}


def pascal(name: str) -> str:
    if name in WORD_BREAKS:
        return WORD_BREAKS[name]
    return name[:1].upper() + name[1:]


def main() -> None:
    path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_JSON
    with open(path, encoding="utf-8") as f:
        icons = json.load(f)
    items = [(pascal(k), k, ord(v)) for k, v in icons.items()]
    names = [n for n, _, _ in items]
    assert len(names) == len(set(names)), "duplicate PascalCase names"

    enum_lines = [
        "// <auto-generated>",
        "//   Generated by tools/gen-appbar-icons.py from appBarIcons.json.",
        "//   Do not edit by hand. Run the script again instead.",
        "// </auto-generated>",
        "",
        "namespace AvaWin;",
        "",
        "/// <summary>",
        "/// The 196 named glyphs of Symbols.ttf that an AppBarCommand, NavBarCommand or SymbolIcon accepts.",
        "/// Resolve a member to its glyph with <see cref=\"AppBarIcons.Glyph(AppBarIcon)\"/>.",
        "/// </summary>",
        "public enum AppBarIcon",
        "{",
    ]
    for n, k, cp in items:
        enum_lines.append(f"    /// <summary>The <c>{k}</c> glyph (U+{cp:04X}).</summary>")
        enum_lines.append(f"    {n},")
    enum_lines.append("}")
    enum_lines.append("")

    glyph_lines = [
        "// <auto-generated>",
        "//   Generated by tools/gen-appbar-icons.py from appBarIcons.json.",
        "//   Do not edit by hand. Run the script again instead.",
        "// </auto-generated>",
        "",
        "#nullable enable",
        "",
        "using System;",
        "using System.Collections.Generic;",
        "",
        "namespace AvaWin;",
        "",
        "/// <summary>Glyph lookup for <see cref=\"AppBarIcon\"/>.</summary>",
        "public static class AppBarIcons",
        "{",
        "    private static readonly string[] s_glyphs =",
        "    [",
    ]
    for n, k, cp in items:
        glyph_lines.append(f"        \"\\u{cp:04X}\", // {n} ({k})")
    glyph_lines += [
        "    ];",
        "",
        "    private static readonly Dictionary<string, AppBarIcon> s_byWinJsName = new(StringComparer.OrdinalIgnoreCase)",
        "    {",
    ]
    for n, k, cp in items:
        glyph_lines.append(f"        [\"{k}\"] = AppBarIcon.{n},")
    glyph_lines += [
        "    };",
        "",
        "    /// <summary>Number of icons in the set.</summary>",
        "    public static int Count => s_glyphs.Length;",
        "",
        "    /// <summary>Returns the single-character glyph string for <paramref name=\"icon\"/>.</summary>",
        "    public static string Glyph(AppBarIcon icon)",
        "    {",
        "        var i = (int)icon;",
        "        if ((uint)i >= (uint)s_glyphs.Length)",
        "        {",
        "            throw new ArgumentOutOfRangeException(nameof(icon));",
        "        }",
        "",
        "        return s_glyphs[i];",
        "    }",
        "",
        "    /// <summary>Returns the codepoint for <paramref name=\"icon\"/>.</summary>",
        "    public static int Codepoint(AppBarIcon icon) => Glyph(icon)[0];",
        "",
        "    /// <summary>Resolves a lowercase icon name (<c>\"mailreplyall\"</c>) or a PascalCase member name to an icon.</summary>",
        "    public static bool TryParse(string? name, out AppBarIcon icon)",
        "    {",
        "        if (!string.IsNullOrEmpty(name) && (s_byWinJsName.TryGetValue(name, out icon) || Enum.TryParse(name, true, out icon)))",
        "        {",
        "            return true;",
        "        }",
        "",
        "        icon = default;",
        "        return false;",
        "    }",
        "",
        "    /// <summary>All icons in declaration order.</summary>",
        "    public static IReadOnlyList<AppBarIcon> All { get; } = Enum.GetValues<AppBarIcon>();",
        "}",
        "",
    ]

    out = os.path.join(ROOT, "src", "AvaWin.Theme", "Glyphs")
    with open(os.path.join(out, "AppBarIcon.cs"), "w", encoding="utf-8") as f:
        f.write("\n".join(enum_lines))
    with open(os.path.join(out, "AppBarIcons.cs"), "w", encoding="utf-8") as f:
        f.write("\n".join(glyph_lines))
    print(f"wrote {len(items)} icons")


if __name__ == "__main__":
    main()
