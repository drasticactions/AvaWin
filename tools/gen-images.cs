#!/usr/bin/env dotnet
#:package SkiaSharp@3.119.4
#:property ManagePackageVersionsCentrally=false
#:property TreatWarningsAsErrors=false
// Generates the gallery's placeholder photos (samples/AvaWin.Gallery/Assets/Photos/photo-NN.jpg).
// Abstract, deterministic, license-free. Run from the repo root: dotnet run tools/gen-images.cs
using System;
using System.IO;
using SkiaSharp;

const int Count = 24;
const int Width = 640;
const int Height = 400;
var outDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "samples", "AvaWin.Gallery", "Assets", "Photos"));
Directory.CreateDirectory(outDir);

// Tile palette pairs (base, highlight).
(string, string)[] palettes =
[
    ("#4617B4", "#8C4BFF"), ("#2672EC", "#5FA2FF"), ("#00A600", "#5FD35F"), ("#D24726", "#FF8A5B"),
    ("#AA40FF", "#D69CFF"), ("#008299", "#3FC1D6"), ("#82BA00", "#B8E24A"), ("#EE1111", "#FF6A6A"),
    ("#1E3A5F", "#3C7BC8"), ("#5C2D91", "#B37FE0"), ("#B4009E", "#F16DDC"), ("#0072C6", "#4FB3FF"),
    ("#D8A200", "#FFDB55"), ("#2E8B57", "#6FD59A"), ("#8B4513", "#D28A4A"), ("#333333", "#8A8A8A"),
];

for (var i = 0; i < Count; i++)
{
    var rnd = new Random(1000 + i);
    var (a, b) = palettes[i % palettes.Length];
    using var surface = SKSurface.Create(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
    var canvas = surface.Canvas;
    var start = SKColor.Parse(a);
    var end = SKColor.Parse(b);
    var angle = rnd.NextDouble() * Math.PI;
    var dx = (float)Math.Cos(angle) * Width;
    var dy = (float)Math.Sin(angle) * Height;
    using (var bg = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(Width / 2f - dx / 2, Height / 2f - dy / 2), new SKPoint(Width / 2f + dx / 2, Height / 2f + dy / 2), [start, end], SKShaderTileMode.Clamp) })
    {
        canvas.DrawRect(0, 0, Width, Height, bg);
    }

    // Soft translucent shapes: circles, bands and triangles, like a stylised landscape.
    var kind = i % 4;
    using var shape = new SKPaint { IsAntialias = true };
    for (var s = 0; s < 5; s++)
    {
        var alpha = (byte)rnd.Next(28, 80);
        shape.Color = (s % 2 == 0 ? SKColors.White : SKColors.Black).WithAlpha(alpha);
        switch (kind)
        {
            case 0:
                canvas.DrawCircle(rnd.Next(0, Width), rnd.Next(0, Height), rnd.Next(60, 220), shape);
                break;
            case 1:
                var y = rnd.Next(0, Height);
                canvas.Save();
                canvas.RotateDegrees(rnd.Next(-25, 25), Width / 2f, Height / 2f);
                canvas.DrawRect(-Width, y, Width * 3, rnd.Next(20, 90), shape);
                canvas.Restore();
                break;
            case 2:
                using (var tri = new SKPath())
                {
                    var x0 = rnd.Next(-100, Width);
                    var w = rnd.Next(200, 500);
                    var h = rnd.Next(100, 320);
                    tri.MoveTo(x0, Height);
                    tri.LineTo(x0 + w / 2f, Height - h);
                    tri.LineTo(x0 + w, Height);
                    tri.Close();
                    canvas.DrawPath(tri, shape);
                }

                break;
            default:
                canvas.DrawRoundRect(rnd.Next(0, Width), rnd.Next(0, Height), rnd.Next(80, 260), rnd.Next(80, 260), 6, 6, shape);
                break;
        }
    }

    // A horizon "sun" on every fourth image for variety.
    if (kind == 2)
    {
        shape.Color = SKColors.White.WithAlpha(90);
        canvas.DrawCircle(rnd.Next(100, Width - 100), rnd.Next(60, 160), rnd.Next(30, 70), shape);
    }

    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 72);
    var path = Path.Combine(outDir, $"photo-{i + 1:00}.jpg");
    using var file = File.OpenWrite(path);
    file.SetLength(0);
    data.SaveTo(file);
    Console.WriteLine($"{Path.GetFileName(path)}  {data.Size / 1024} KB");
}
