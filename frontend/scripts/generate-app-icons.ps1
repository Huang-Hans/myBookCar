param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$frontendRoot = Split-Path -Parent $PSScriptRoot
$logoPath = Join-Path $frontendRoot 'LMS-ClientApp/src/static/figma/melco-logo.png'
$logo = [System.Drawing.Bitmap]::new($logoPath)
$densitySizes = [ordered]@{
    'android-mdpi' = 48
    'android-hdpi' = 72
    'android-xhdpi' = 96
    'android-xxhdpi' = 144
    'android-xxxhdpi' = 192
    'ios-app-2x' = 120
    'ios-app-3x' = 180
    'ios-ipad' = 76
    'ios-ipad-2x' = 152
    'ios-ipad-pro-2x' = 167
}

function New-LauncherIcon([string]$role, [string]$backgroundHex, [string]$accentHex, [int]$size, [string]$outputPath) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $accentBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($accentHex))
    $labelBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $font = [System.Drawing.Font]::new('Segoe UI Semibold', [single](92 * $size / 1024), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = [System.Drawing.StringFormat]::new()

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml($backgroundHex))

        $accentTop = [int][Math]::Round(724 * $size / 1024)
        $graphics.FillRectangle($accentBrush, 0, $accentTop, $size, $size - $accentTop)

        $logoBounds = [System.Drawing.Rectangle]::new(
            [int][Math]::Round(170 * $size / 1024),
            [int][Math]::Round(188 * $size / 1024),
            [int][Math]::Round(684 * $size / 1024),
            [int][Math]::Round(424 * $size / 1024)
        )
        $logoSource = [System.Drawing.Rectangle]::new(0, 0, 500, 310)
        $graphics.DrawImage($logo, $logoBounds, $logoSource, [System.Drawing.GraphicsUnit]::Pixel)

        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $labelBounds = [System.Drawing.RectangleF]::new(0, [single](742 * $size / 1024), [single]$size, [single](236 * $size / 1024))
        $graphics.DrawString($role, $font, $labelBrush, $labelBounds, $format)

        $directory = Split-Path -Parent $outputPath
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $format.Dispose()
        $font.Dispose()
        $labelBrush.Dispose()
        $accentBrush.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function New-HarmonyLayers([string]$role, [string]$backgroundHex, [string]$accentHex, [string]$brandRoot) {
    $size = 1024
    $background = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $backgroundGraphics = [System.Drawing.Graphics]::FromImage($background)
    $foreground = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $foregroundGraphics = [System.Drawing.Graphics]::FromImage($foreground)
    $accentBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($accentHex))
    $labelBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $font = [System.Drawing.Font]::new('Segoe UI Semibold', 92, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = [System.Drawing.StringFormat]::new()

    try {
        $backgroundGraphics.Clear([System.Drawing.ColorTranslator]::FromHtml($backgroundHex))
        $foregroundGraphics.Clear([System.Drawing.Color]::Transparent)
        $foregroundGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $foregroundGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $foregroundGraphics.FillRectangle($accentBrush, 0, 724, $size, 300)
        $foregroundGraphics.DrawImage($logo, [System.Drawing.Rectangle]::new(170, 188, 684, 424), [System.Drawing.Rectangle]::new(0, 0, 500, 310), [System.Drawing.GraphicsUnit]::Pixel)
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $foregroundGraphics.DrawString($role, $font, $labelBrush, [System.Drawing.RectangleF]::new(0, 742, 1024, 236), $format)
        $background.Save((Join-Path $brandRoot 'launcher/harmony-background.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        $foreground.Save((Join-Path $brandRoot 'launcher/harmony-foreground.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $format.Dispose()
        $font.Dispose()
        $labelBrush.Dispose()
        $accentBrush.Dispose()
        $foregroundGraphics.Dispose()
        $foreground.Dispose()
        $backgroundGraphics.Dispose()
        $background.Dispose()
    }
}

try {
    foreach ($app in @(
        @{ Name = 'LMS-ClientApp'; Role = 'CLIENT'; Background = '#FFF8E9'; Accent = '#E8A329' },
        @{ Name = 'LMS-DriverApp'; Role = 'DRIVER'; Background = '#EDF2F5'; Accent = '#263B58' },
        @{ Name = 'LMS-UserApp'; Role = 'USER'; Background = '#EEF0FA'; Accent = '#4F5A9F' }
    )) {
        $brandRoot = Join-Path $frontendRoot "$($app.Name)/src/static/brand"
        New-LauncherIcon $app.Role $app.Background $app.Accent 1024 (Join-Path $brandRoot 'app-icon.png')
        foreach ($density in $densitySizes.GetEnumerator()) {
            New-LauncherIcon $app.Role $app.Background $app.Accent $density.Value (Join-Path $brandRoot "launcher/$($density.Key).png")
        }
        New-HarmonyLayers $app.Role $app.Background $app.Accent $brandRoot
        Copy-Item -LiteralPath (Join-Path $brandRoot 'app-icon.png') -Destination (Join-Path $frontendRoot "$($app.Name)/android/app/src/main/res/drawable/app_icon.png") -Force
        Write-Output "Generated launcher icons for $($app.Name)"
    }
}
finally {
    $logo.Dispose()
}
