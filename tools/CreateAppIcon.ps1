Add-Type -AssemblyName System.Drawing

$scriptRoot = Split-Path -Parent $PSScriptRoot
$assetsDirectory = Join-Path $scriptRoot 'Assets'
$iconPath = Join-Path $assetsDirectory 'DartLeague.ico'
New-Item -ItemType Directory -Path $assetsDirectory -Force | Out-Null

$bitmap = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)

function New-RoundedRectanglePath([float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

$basePath = New-RoundedRectanglePath 8 8 240 240 58
$graphics.FillPath([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#5B5CE2')), $basePath)

$centerX = 123
$centerY = 136
$graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#172033')), 37, 50, 172, 172)
$graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#F7F8FC')), 49, 62, 148, 148)
$graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#172033')), 62, 75, 122, 122)
$graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#F7F8FC')), 78, 91, 90, 90)
$graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#5B5CE2')), 96, 109, 54, 54)
$graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#172033')), 112, 125, 22, 22)

$dartPen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 8)
$dartPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$dartPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawLine($dartPen, 193, 49, 128, 126)
$dartPen.Dispose()

$dartHead = [System.Drawing.PointF[]]@(
    [System.Drawing.PointF]::new(197, 43),
    [System.Drawing.PointF]::new(209, 31),
    [System.Drawing.PointF]::new(202, 57)
)
$graphics.FillPolygon([System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#C9CAFF')), $dartHead)
$graphics.Dispose()

$pngBuffer = [System.IO.MemoryStream]::new()
$bitmap.Save($pngBuffer, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()
$pngBytes = $pngBuffer.ToArray()
$pngBuffer.Dispose()

$stream = [System.IO.File]::Open($iconPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = [System.IO.BinaryWriter]::new($stream)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]1)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]32)
$writer.Write([UInt32]$pngBytes.Length)
$writer.Write([UInt32]22)
$writer.Write($pngBytes)
$writer.Dispose()

Write-Output "Created $iconPath"
