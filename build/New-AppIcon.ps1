# This Source Code Form is subject to the terms of the MIT License.
# If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
# Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
# All Rights Reserved.

<#
.SYNOPSIS
Draws the application's icon from the Pallas mark in build/nuget.svg.

.DESCRIPTION
The package icon is the mark - a square root holding a division sign - above the word "pallas". At the sizes Windows
shows an application at, 16 to 48 pixels, the word is a grey smudge, so the icon is the mark alone: the polyline, the
line and the circles of the SVG, drawn with GDI+ on a white rounded tile at every size the icon carries and written as
one .ico of PNG frames. Drawing each size rather than scaling one picture keeps the strokes whole; below 1.6 pixels a
stroke is widened to 1.6, or the square root would fade at 16 pixels.

Run it again when the SVG changes; its output is committed.

.PARAMETER Svg
The SVG the mark is read from.

.PARAMETER Output
The .ico to write.
#>
[CmdletBinding()]
param(
    [string] $Svg = (Join-Path $PSScriptRoot 'nuget.svg'),
    [string] $Output = (Join-Path $PSScriptRoot '..\src\app\Barbatos.Pallas.Wpf\Assets\Pallas.ico')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$minimumStroke = 1.6

[xml] $document = Get-Content -Path $Svg -Raw
$polylines = @($document.svg.polyline)
$lines = @($document.svg.line)
$circles = @($document.svg.circle)

function Get-Color([string] $hex) { [Drawing.ColorTranslator]::FromHtml($hex) }

# The mark's bounds, strokes included, so it can be centred on the tile.
$left = [double]::MaxValue; $top = [double]::MaxValue; $right = [double]::MinValue; $bottom = [double]::MinValue
function Add-Bounds([double] $x, [double] $y, [double] $radius) {
    $script:left = [Math]::Min($script:left, $x - $radius); $script:right = [Math]::Max($script:right, $x + $radius)
    $script:top = [Math]::Min($script:top, $y - $radius); $script:bottom = [Math]::Max($script:bottom, $y + $radius)
}
foreach ($polyline in $polylines) {
    foreach ($point in $polyline.points.Trim() -split '\s+') {
        $x, $y = $point -split ','
        Add-Bounds ([double] $x) ([double] $y) ([double] $polyline.'stroke-width' / 2)
    }
}
foreach ($line in $lines) {
    Add-Bounds ([double] $line.x1) ([double] $line.y1) ([double] $line.'stroke-width' / 2)
    Add-Bounds ([double] $line.x2) ([double] $line.y2) ([double] $line.'stroke-width' / 2)
}
foreach ($circle in $circles) { Add-Bounds ([double] $circle.cx) ([double] $circle.cy) ([double] $circle.r) }

# The 256-pixel frame is a PNG, as Windows expects; the others are 32-bit bitmaps, which every reader of an .ico
# understands - System.Drawing's own Icon, for one, cannot read a PNG frame of any other size.
function ConvertTo-Png([Drawing.Bitmap] $bitmap) {
    $stream = New-Object IO.MemoryStream
    $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
    return , $stream.ToArray()
}

function ConvertTo-Dib([Drawing.Bitmap] $bitmap) {
    $size = $bitmap.Width
    $rectangle = New-Object Drawing.Rectangle 0, 0, $size, $size
    $bits = $bitmap.LockBits($rectangle, [Drawing.Imaging.ImageLockMode]::ReadOnly, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $pixels = New-Object byte[] ($bits.Stride * $size)
    [Runtime.InteropServices.Marshal]::Copy($bits.Scan0, $pixels, 0, $pixels.Length)
    $bitmap.UnlockBits($bits)

    $stream = New-Object IO.MemoryStream
    $writer = New-Object IO.BinaryWriter $stream
    # BITMAPINFOHEADER: the height counts the colour rows and the mask rows together.
    $writer.Write([uint32] 40); $writer.Write([int32] $size); $writer.Write([int32] (2 * $size))
    $writer.Write([uint16] 1); $writer.Write([uint16] 32); $writer.Write([uint32] 0)
    $writer.Write([uint32] 0); $writer.Write([int32] 0); $writer.Write([int32] 0); $writer.Write([uint32] 0); $writer.Write([uint32] 0)
    # BGRA rows, bottom-up - which is how GDI+ already lays out Format32bppArgb in memory, row by row.
    for ($row = $size - 1; $row -ge 0; $row--) { $writer.Write($pixels, $row * $bits.Stride, $size * 4) }
    # The 1-bit mask, all clear: the alpha channel says what is transparent.
    $writer.Write((New-Object byte[] ([Math]::Ceiling($size / 32) * 4 * $size)))
    $writer.Flush()
    return , $stream.ToArray()
}

function New-Pen([string] $color, [double] $width, [double] $scale) {
    $pen = New-Object Drawing.Pen (Get-Color $color), ([float] [Math]::Max($width, $minimumStroke / $scale))
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    return $pen
}

$frames = foreach ($size in $sizes) {
    $bitmap = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([Drawing.Color]::Transparent)

    # The tile: white, so the dark stroke reads on a dark taskbar as well as a light one.
    $radius = $size * 0.2
    $tile = New-Object Drawing.Drawing2D.GraphicsPath
    $edge = $size - 1
    $tile.AddArc(0, 0, 2 * $radius, 2 * $radius, 180, 90)
    $tile.AddArc($edge - 2 * $radius, 0, 2 * $radius, 2 * $radius, 270, 90)
    $tile.AddArc($edge - 2 * $radius, $edge - 2 * $radius, 2 * $radius, 2 * $radius, 0, 90)
    $tile.AddArc(0, $edge - 2 * $radius, 2 * $radius, 2 * $radius, 90, 90)
    $tile.CloseFigure()
    $graphics.FillPath([Drawing.Brushes]::White, $tile)
    # A faint edge, or the tile disappears on a white window.
    $outline = New-Object Drawing.Pen (Get-Color '#dfe6e9'), ([float] [Math]::Max(1, $size / 64))
    $graphics.DrawPath($outline, $tile)
    $outline.Dispose()

    $scale = ($size * 0.78) / ($right - $left)
    $graphics.TranslateTransform($size / 2, $size / 2)
    $graphics.ScaleTransform($scale, $scale)
    $graphics.TranslateTransform(-($left + $right) / 2, -($top + $bottom) / 2)

    foreach ($polyline in $polylines) {
        $points = foreach ($point in $polyline.points.Trim() -split '\s+') {
            $x, $y = $point -split ','
            New-Object Drawing.PointF ([float] $x), ([float] $y)
        }
        $pen = New-Pen $polyline.stroke ([double] $polyline.'stroke-width') $scale
        $graphics.DrawLines($pen, [Drawing.PointF[]] $points)
        $pen.Dispose()
    }
    foreach ($line in $lines) {
        $pen = New-Pen $line.stroke ([double] $line.'stroke-width') $scale
        $graphics.DrawLine($pen, [float] $line.x1, [float] $line.y1, [float] $line.x2, [float] $line.y2)
        $pen.Dispose()
    }
    foreach ($circle in $circles) {
        $r = [Math]::Max([double] $circle.r, $minimumStroke / $scale)
        $brush = New-Object Drawing.SolidBrush (Get-Color $circle.fill)
        $graphics.FillEllipse($brush, [float] ([double] $circle.cx - $r), [float] ([double] $circle.cy - $r), [float] (2 * $r), [float] (2 * $r))
        $brush.Dispose()
    }

    $graphics.Dispose()
    $data = if ($size -ge 256) { ConvertTo-Png $bitmap } else { ConvertTo-Dib $bitmap }
    $bitmap.Dispose()
    , @($size, $data)
}

# ICONDIR, then one ICONDIRENTRY per frame, then the PNG data. A dimension of 256 is written as 0.
$icon = New-Object IO.MemoryStream
$writer = New-Object IO.BinaryWriter $icon
$writer.Write([uint16] 0); $writer.Write([uint16] 1); $writer.Write([uint16] $frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
    $size, $data = $frame
    $dimension = if ($size -ge 256) { 0 } else { $size }
    $writer.Write([byte] $dimension); $writer.Write([byte] $dimension); $writer.Write([byte] 0); $writer.Write([byte] 0)
    $writer.Write([uint16] 1); $writer.Write([uint16] 32)
    $writer.Write([uint32] $data.Length); $writer.Write([uint32] $offset)
    $offset += $data.Length
}
foreach ($frame in $frames) { $writer.Write([byte[]] $frame[1]) }
$writer.Flush()

$directory = Split-Path -Parent $Output
if (-not (Test-Path $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
[IO.File]::WriteAllBytes($Output, $icon.ToArray())
Write-Host "Wrote $Output ($($frames.Count) sizes, $($icon.Length) bytes)."
