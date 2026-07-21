$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$resourceRoot = Resolve-Path (Join-Path $PSScriptRoot '..\Resources')
$artRoot = Join-Path $resourceRoot 'art'
$cityRoot = Join-Path $artRoot 'city'
$cityStateRoot = Join-Path $cityRoot 'states'
$worldRoot = Join-Path $artRoot 'world'
New-Item -ItemType Directory -Force -Path $cityRoot, $cityStateRoot, $worldRoot | Out-Null

function Color-Hex([string]$hex) {
    return [System.Drawing.ColorTranslator]::FromHtml($hex)
}

function Brush-Hex([string]$hex) {
    return [System.Drawing.SolidBrush]::new((Color-Hex $hex))
}

function Pen-Hex([string]$hex, [float]$width = 1.0) {
    $pen = [System.Drawing.Pen]::new((Color-Hex $hex), $width)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    return $pen
}

function Point-Array([array]$items) {
    return [System.Drawing.Point[]]($items | ForEach-Object { [System.Drawing.Point]::new([int]$_[0], [int]$_[1]) })
}

function New-Canvas([int]$width, [int]$height, [bool]$transparent = $true) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    if ($transparent) {
        $graphics.Clear([System.Drawing.Color]::Transparent)
    }
    return [pscustomobject]@{ Bitmap = $bitmap; Graphics = $graphics }
}

function Save-Canvas($canvas, [string]$path) {
    $canvas.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Graphics.Dispose()
    $canvas.Bitmap.Dispose()
}

function Fill-Polygon($graphics, [string]$color, [array]$points) {
    $brush = Brush-Hex $color
    $graphics.FillPolygon($brush, (Point-Array $points))
    $brush.Dispose()
}

function Stroke-Polygon($graphics, [string]$color, [float]$width, [array]$points) {
    $pen = Pen-Hex $color $width
    $graphics.DrawPolygon($pen, (Point-Array $points))
    $pen.Dispose()
}

function Fill-Rect($graphics, [string]$color, [int]$x, [int]$y, [int]$w, [int]$h) {
    $brush = Brush-Hex $color
    $graphics.FillRectangle($brush, $x, $y, $w, $h)
    $brush.Dispose()
}

function Fill-Ellipse($graphics, [string]$color, [int]$x, [int]$y, [int]$w, [int]$h) {
    $brush = Brush-Hex $color
    $graphics.FillEllipse($brush, $x, $y, $w, $h)
    $brush.Dispose()
}

function Draw-Line($graphics, [string]$color, [float]$width, [int]$x1, [int]$y1, [int]$x2, [int]$y2) {
    $pen = Pen-Hex $color $width
    $graphics.DrawLine($pen, $x1, $y1, $x2, $y2)
    $pen.Dispose()
}

$canvas = New-Canvas 1280 720 $false
$g = $canvas.Graphics
$sky = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.Rectangle]::new(0, 0, 1280, 720),
    (Color-Hex '#6fb6c8'),
    (Color-Hex '#d8bd86'),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillRectangle($sky, 0, 0, 1280, 720)
$sky.Dispose()
Fill-Ellipse $g '#f0d98b' 935 62 118 118
Fill-Polygon $g '#486a66' @((0,270),(135,135),(270,258),(400,100),(560,292),(720,148),(900,275),(1088,125),(1280,270),(1280,720),(0,720))
Fill-Polygon $g '#5f8073' @((0,325),(190,215),(385,315),(565,205),(750,325),(990,225),(1280,330),(1280,720),(0,720))
Fill-Polygon $g '#3f6f4c' @((0,420),(1280,345),(1280,720),(0,720))
Fill-Polygon $g '#547b43' @((0,520),(1280,460),(1280,720),(0,720))
Fill-Polygon $g '#376e80' @((0,620),(295,540),(535,590),(820,510),(1280,575),(1280,720),(0,720))
Fill-Polygon $g '#9db76d' @((0,560),(290,500),(560,548),(830,482),(1280,532),(1280,592),(840,540),(555,605),(290,555),(0,638))
Save-Canvas $canvas (Join-Path $cityRoot 'city_backdrop.png')

$canvas = New-Canvas 360 330
$g = $canvas.Graphics
Fill-Ellipse $g '#00000040' 42 272 280 36
Fill-Rect $g '#7d8487' 95 104 170 178
Fill-Rect $g '#60696e' 118 132 124 150
Fill-Polygon $g '#b75346' @((82,108),(180,36),(278,108))
Fill-Rect $g '#8d9599' 46 148 64 135
Fill-Rect $g '#8d9599' 250 148 64 135
Fill-Polygon $g '#a6463f' @((38,150),(78,88),(118,150))
Fill-Polygon $g '#a6463f' @((242,150),(282,88),(322,150))
Fill-Rect $g '#384247' 156 211 48 71
Fill-Ellipse $g '#e4c465' 162 154 36 36
1..5 | ForEach-Object { Fill-Rect $g '#d0d4ce' (88 + $_ * 32) 132 14 22 }
Stroke-Polygon $g '#475157' 4 @((95,104),(265,104),(265,282),(95,282))
Save-Canvas $canvas (Join-Path $cityRoot 'keep.png')

$canvas = New-Canvas 1000 180
$g = $canvas.Graphics
Fill-Ellipse $g '#00000035' 52 132 895 32
Fill-Rect $g '#747a75' 72 72 856 80
for ($x = 95; $x -lt 900; $x += 70) { Fill-Rect $g '#92988f' $x 44 38 38 }
for ($x = 110; $x -lt 890; $x += 86) { Draw-Line $g '#555f5b' 3 $x 76 ($x + 45) 145 }
Draw-Line $g '#4e5d58' 5 72 72 928 72
Draw-Line $g '#4e5d58' 5 72 152 928 152
Save-Canvas $canvas (Join-Path $cityRoot 'wall.png')

$canvas = New-Canvas 260 210
$g = $canvas.Graphics
Fill-Ellipse $g '#00000035' 42 165 178 24
Fill-Polygon $g '#d8b56b' @((35,150),(130,82),(226,150),(185,176),(78,176))
Fill-Polygon $g '#6a8b49' @((40,150),(130,116),(218,150),(176,168),(85,168))
Fill-Rect $g '#b87645' 88 90 82 80
Fill-Polygon $g '#8a4e38' @((76,95),(130,48),(184,95))
for ($x = 52; $x -lt 210; $x += 26) { Draw-Line $g '#efd68a' 3 $x 151 ($x + 36) 132 }
Save-Canvas $canvas (Join-Path $cityRoot 'farm.png')

$canvas = New-Canvas 260 220
$g = $canvas.Graphics
Fill-Ellipse $g '#00000035' 46 175 170 24
Fill-Rect $g '#8b6846' 72 102 110 74
Fill-Polygon $g '#6c3f31' @((58,106),(128,62),(196,106))
Fill-Rect $g '#4f6f47' 178 82 22 96
Fill-Ellipse $g '#3e713e' 162 44 58 58
Fill-Ellipse $g '#477a42' 184 58 48 52
Fill-Rect $g '#caa76e' 42 156 174 14
Draw-Line $g '#5e412f' 5 45 166 215 134
Draw-Line $g '#5e412f' 5 48 178 210 148
Save-Canvas $canvas (Join-Path $cityRoot 'sawmill.png')

$canvas = New-Canvas 290 230
$g = $canvas.Graphics
Fill-Ellipse $g '#00000035' 48 184 190 24
Fill-Rect $g '#7c6f65' 68 105 150 86
Fill-Polygon $g '#485f68' @((54,110),(144,52),(232,110))
Fill-Rect $g '#5e5149' 96 132 96 59
Fill-Rect $g '#c4c8bd' 116 146 20 28
Fill-Rect $g '#c4c8bd' 156 146 20 28
Draw-Line $g '#d7c27b' 6 225 82 249 40
Draw-Line $g '#d7c27b' 6 238 84 262 42
Save-Canvas $canvas (Join-Path $cityRoot 'barracks.png')

$canvas = New-Canvas 290 230
$g = $canvas.Graphics
Fill-Ellipse $g '#00000035' 54 182 184 24
Fill-Rect $g '#806551' 78 112 132 76
Fill-Polygon $g '#b7894a' @((62,116),(145,62),(228,116))
Fill-Rect $g '#4d5962' 112 78 30 110
Fill-Rect $g '#59666d' 154 92 28 96
Fill-Ellipse $g '#e6c46e' 112 42 28 28
Draw-Line $g '#d2b06c' 5 228 146 258 120
Draw-Line $g '#d2b06c' 5 230 164 263 138
Save-Canvas $canvas (Join-Path $cityRoot 'workshop.png')

$canvas = New-Canvas 1280 720 $false
$g = $canvas.Graphics
$land = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.Rectangle]::new(0, 0, 1280, 720),
    (Color-Hex '#4d8b58'),
    (Color-Hex '#2e6364'),
    [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal
)
$g.FillRectangle($land, 0, 0, 1280, 720)
$land.Dispose()
for ($i = 0; $i -lt 13; $i++) {
    $x = 70 + (($i * 173) % 1150)
    $y = 105 + (($i * 97) % 520)
    Fill-Ellipse $g '#6ba06580' $x $y 170 80
}
Fill-Polygon $g '#346e88cc' @((0,500),(170,458),(360,515),(580,452),(810,518),(1015,470),(1280,525),(1280,720),(0,720))
Save-Canvas $canvas (Join-Path $worldRoot 'world_backdrop.png')

$canvas = New-Canvas 260 160
$g = $canvas.Graphics
Fill-Polygon $g '#6e9e5e' @((130,8),(248,80),(130,152),(12,80))
Stroke-Polygon $g '#446946' 4 @((130,8),(248,80),(130,152),(12,80))
Draw-Line $g '#83b573' 3 130 8 130 152
Draw-Line $g '#83b573' 3 12 80 248 80
Save-Canvas $canvas (Join-Path $worldRoot 'tile_grass.png')

$canvas = New-Canvas 180 190
$g = $canvas.Graphics
Fill-Ellipse $g '#00000040' 38 152 104 18
Fill-Rect $g '#8a9091' 56 76 68 82
Fill-Rect $g '#687274' 74 98 32 60
Fill-Polygon $g '#a54d42' @((46,80),(90,38),(134,80))
Fill-Rect $g '#7b8586' 32 102 34 58
Fill-Rect $g '#7b8586' 114 102 34 58
Fill-Polygon $g '#c59a53' @((90,162),(105,184),(90,190),(75,184))
Save-Canvas $canvas (Join-Path $worldRoot 'castle_marker.png')

$canvas = New-Canvas 150 150
$g = $canvas.Graphics
Fill-Ellipse $g '#00000035' 28 118 94 16
Fill-Polygon $g '#8f8b79' @((35,112),(74,40),(118,112))
Fill-Polygon $g '#b7a15b' @((72,56),(95,112),(48,112))
Fill-Rect $g '#54493a' 55 102 42 24
Save-Canvas $canvas (Join-Path $worldRoot 'mine_marker.png')

$canvas = New-Canvas 160 150
$g = $canvas.Graphics
Fill-Ellipse $g '#00000035' 24 118 106 16
Fill-Rect $g '#6b4c35' 72 72 16 52
Fill-Ellipse $g '#3f743e' 36 36 58 58
Fill-Ellipse $g '#4f8848' 70 26 60 62
Fill-Ellipse $g '#376b3a' 60 58 58 54
Save-Canvas $canvas (Join-Path $worldRoot 'forest_marker.png')

$canvas = New-Canvas 320 110
$g = $canvas.Graphics
Draw-Line $g '#e2c45f' 8 32 72 258 38
Fill-Polygon $g '#e2c45f' @((258,38),(230,20),(242,58))
Draw-Line $g '#6f4b2f' 3 32 82 258 48
Save-Canvas $canvas (Join-Path $worldRoot 'march_arrow.png')

function New-CityBuildingAsset(
    [string]$fileName,
    [string]$bodyColor,
    [string]$roofColor,
    [string]$accentColor,
    [string]$glyph,
    [string]$shape = 'hall'
) {
    $canvas = New-Canvas 300 250
    $g = $canvas.Graphics
    Fill-Ellipse $g '#00000035' 48 202 204 26

    if ($shape -eq 'tower') {
        Fill-Rect $g $bodyColor 110 66 80 140
        Fill-Polygon $g $roofColor @((96,70),(150,28),(204,70))
        Fill-Rect $g $accentColor 132 150 36 56
        Fill-Rect $g '#d8d7c8' 124 92 16 24
        Fill-Rect $g '#d8d7c8' 160 92 16 24
    }
    elseif ($shape -eq 'mine') {
        Fill-Polygon $g $bodyColor @((48,190),(126,58),(220,190))
        Fill-Polygon $g $roofColor @((118,82),(178,190),(72,190))
        Fill-Rect $g $accentColor 102 168 78 34
        Draw-Line $g '#5c4c3a' 5 68 192 214 142
        Draw-Line $g '#5c4c3a' 5 84 206 232 156
    }
    elseif ($shape -eq 'yard') {
        Fill-Rect $g $bodyColor 64 128 160 70
        Fill-Polygon $g $roofColor @((50,132),(144,72),(238,132))
        Fill-Rect $g $accentColor 98 152 38 46
        Fill-Rect $g '#6d4d35' 204 96 18 102
        Fill-Ellipse $g '#467a42' 184 58 62 64
        Fill-Ellipse $g '#5d8f4d' 216 72 48 54
    }
    elseif ($shape -eq 'tent') {
        Fill-Polygon $g $bodyColor @((48,196),(150,68),(252,196))
        Fill-Polygon $g $roofColor @((150,68),(208,196),(92,196))
        Draw-Line $g $accentColor 5 70 196 230 196
        Draw-Line $g '#f0d88a' 4 150 76 150 196
    }
    else {
        Fill-Rect $g $bodyColor 72 112 156 90
        Fill-Polygon $g $roofColor @((54,116),(150,54),(246,116))
        Fill-Rect $g $accentColor 124 150 52 52
        Fill-Rect $g '#dad7c6' 92 136 22 28
        Fill-Rect $g '#dad7c6' 186 136 22 28
    }

    $font = [System.Drawing.Font]::new('Arial', 32, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = Brush-Hex '#fff1b8'
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString($glyph, $font, $brush, [System.Drawing.RectangleF]::new(0, 112, 300, 80), $format)
    $format.Dispose()
    $brush.Dispose()
    $font.Dispose()

    Save-Canvas $canvas (Join-Path $cityRoot $fileName)
}

$canvas = New-Canvas 1600 740
$g = $canvas.Graphics
Fill-Polygon $g '#8da66b' @((70,560),(790,260),(1530,560),(1370,690),(230,690))
Fill-Polygon $g '#71835c' @((230,690),(790,470),(1370,690))
for ($x = 170; $x -lt 1450; $x += 160) {
    Draw-Line $g '#69795a' 4 $x 668 ($x + 210) 514
}
Fill-Polygon $g '#6f756f' @((90,570),(790,335),(1510,570),(1510,630),(790,400),(90,630))
for ($x = 150; $x -lt 1460; $x += 92) {
    Fill-Rect $g '#a0a69d' $x 540 48 48
}
Save-Canvas $canvas (Join-Path $cityRoot 'courtyard.png')

New-CityBuildingAsset 'lumberyard.png' '#8a6546' '#516b5f' '#c6a05a' 'L' 'yard'
New-CityBuildingAsset 'stable.png' '#87674d' '#7b4239' '#4c3430' 'S' 'hall'
New-CityBuildingAsset 'range.png' '#716d56' '#3f5d52' '#5f473b' 'R' 'tent'
New-CityBuildingAsset 'academy.png' '#656d83' '#42536d' '#2e3749' 'A' 'tower'
New-CityBuildingAsset 'hospital.png' '#8a7f75' '#b35650' '#d8ddd2' 'H' 'hall'
New-CityBuildingAsset 'tavern.png' '#8f6549' '#8a4738' '#5b3a2c' 'T' 'hall'
New-CityBuildingAsset 'embassy.png' '#6c7588' '#b9904f' '#3f4d5a' 'E' 'tower'
New-CityBuildingAsset 'watchtower.png' '#727b75' '#4d5f63' '#364144' 'W' 'tower'
New-CityBuildingAsset 'forge.png' '#6e5f54' '#9a4d3e' '#d58445' 'F' 'hall'
New-CityBuildingAsset 'iron_mine.png' '#77766e' '#4e565c' '#b58b52' 'Fe' 'mine'
New-CityBuildingAsset 'silver_mine.png' '#8b9290' '#59696d' '#c6d2d0' 'Ag' 'mine'
New-CityBuildingAsset 'market.png' '#8b6f4b' '#d09b4f' '#4c6b67' 'M' 'hall'
New-CityBuildingAsset 'hall_of_war.png' '#6f6370' '#8f483f' '#c39b53' 'HW' 'tower'
New-CityBuildingAsset 'drill_grounds.png' '#786b55' '#525f5e' '#caa65c' 'DG' 'tent'
New-CityBuildingAsset 'warehouse.png' '#7a715f' '#69503d' '#b69867' 'WH' 'hall'

function Stroke-Rect($graphics, [string]$color, [float]$width, [int]$x, [int]$y, [int]$w, [int]$h) {
    $pen = Pen-Hex $color $width
    $graphics.DrawRectangle($pen, $x, $y, $w, $h)
    $pen.Dispose()
}

function Fill-GradientRect($graphics, [string]$topColor, [string]$bottomColor, [int]$x, [int]$y, [int]$w, [int]$h) {
    $brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Rectangle]::new($x, $y, $w, $h),
        (Color-Hex $topColor),
        (Color-Hex $bottomColor),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
    )
    $graphics.FillRectangle($brush, $x, $y, $w, $h)
    $brush.Dispose()
}

function Draw-RoofTiles($graphics, [int]$left, [int]$top, [int]$right, [int]$bottom, [string]$color) {
    for ($x = $left + 18; $x -lt $right; $x += 28) {
        Draw-Line $graphics $color 2 $x ($top + 14) ($x - 18) ([Math]::Min($top + 68, $bottom - 10))
    }
    Draw-Line $graphics '#fff0bc' 2 ($left + 20) ($top + 18) ($right - 20) ($top + 18)
    Draw-Line $graphics '#5b332c' 3 $left $bottom $right $bottom
}

function Draw-Banner($graphics, [int]$x, [int]$y, [string]$clothColor) {
    Draw-Line $graphics '#3d3327' 3 $x $y $x ($y + 70)
    Fill-Polygon $graphics $clothColor @(
        @($x, ($y + 6)),
        @(($x + 42), ($y + 14)),
        @(($x + 35), ($y + 42)),
        @($x, ($y + 35))
    )
    Draw-Line $graphics '#f4d27c' 2 $x ($y + 8) ($x + 38) ($y + 16)
}

function Draw-Windows($graphics, [int]$x, [int]$y, [int]$count) {
    for ($i = 0; $i -lt $count; $i++) {
        Fill-Rect $graphics '#d7d8c5' ($x + ($i * 42)) $y 22 34
        Fill-Rect $graphics '#7d8b8c' ($x + 4 + ($i * 42)) ($y + 4) 14 26
    }
}

function Draw-StoneLines($graphics, [int]$x, [int]$y, [int]$w, [int]$h) {
    for ($row = $y + 22; $row -lt ($y + $h); $row += 34) {
        Draw-Line $graphics '#5d6868' 2 $x $row ($x + $w) $row
    }
    for ($col = $x + 30; $col -lt ($x + $w); $col += 48) {
        Draw-Line $graphics '#6f7b7a' 2 $col $y $col ($y + $h)
    }
}

function New-PolishedBuilding(
    [string]$fileName,
    [string]$profile,
    [string]$bodyColor,
    [string]$roofColor,
    [string]$trimColor,
    [string]$accentColor
) {
    $canvas = New-Canvas 512 512
    $g = $canvas.Graphics
    Fill-Ellipse $g '#33392f' 78 408 356 44
    Fill-Polygon $g '#526247' @((86,402),(256,326),(426,402),(256,472))
    Fill-Polygon $g '#3f4d3d' @((86,402),(256,472),(256,438),(120,390))
    Fill-Polygon $g '#677655' @((256,326),(426,402),(392,416),(256,360))

    if ($profile -eq 'tower') {
        Fill-GradientRect $g $bodyColor '#4e5d61' 196 170 120 226
        Draw-StoneLines $g 196 170 120 226
        Fill-Polygon $g $roofColor @((174,174),(256,90),(338,174),(316,194),(256,128),(196,194))
        Draw-RoofTiles $g 182 112 330 194 '#743c34'
        Fill-GradientRect $g '#7f8988' '#596665' 132 246 72 150
        Fill-GradientRect $g '#7f8988' '#596665' 308 246 72 150
        Fill-Polygon $g '#8f4f42' @((122,250),(168,198),(214,250))
        Fill-Polygon $g '#8f4f42' @((298,250),(344,198),(390,250))
        Fill-Rect $g $accentColor 238 314 36 82
        Draw-Windows $g 224 210 2
        Draw-Banner $g 328 202 $trimColor
    }
    elseif ($profile -eq 'mine') {
        Fill-Polygon $g '#5f6868' @((86,392),(198,164),(414,394))
        Fill-Polygon $g $bodyColor @((116,386),(232,204),(386,386))
        Fill-Polygon $g $roofColor @((214,236),(306,388),(160,388))
        Fill-Rect $g '#342c27' 206 334 104 66
        Stroke-Rect $g '#a17b47' 5 206 334 104 66
        Fill-Polygon $g $trimColor @((344,322),(372,276),(400,326))
        Fill-Polygon $g $accentColor @((142,334),(162,300),(184,336))
        Draw-Line $g '#5a4431' 7 112 412 388 310
        Draw-Line $g '#7b5b38' 5 124 426 402 324
    }
    elseif ($profile -eq 'tent') {
        Fill-Polygon $g '#7b875d' @((96,400),(256,258),(416,400))
        Fill-Polygon $g $bodyColor @((118,392),(256,158),(394,392))
        Fill-Polygon $g $roofColor @((256,158),(320,392),(194,392))
        Draw-Line $g $trimColor 5 256 164 256 396
        Draw-Line $g '#f1d78a' 4 138 392 374 392
        Draw-Banner $g 346 190 $accentColor
        Fill-Ellipse $g '#af7b45' 212 350 86 28
        Fill-Rect $g '#5a4634' 232 342 46 48
    }
    elseif ($profile -eq 'yard') {
        Fill-GradientRect $g $bodyColor '#665039' 124 258 172 112
        Fill-Polygon $g $roofColor @((100,264),(210,178),(324,264),(294,286),(210,220),(126,286))
        Draw-RoofTiles $g 116 208 306 286 '#593e33'
        Fill-Rect $g $accentColor 174 306 58 64
        Fill-Rect $g '#6e5238' 324 196 26 176
        Fill-Ellipse $g '#326d3c' 288 96 96 100
        Fill-Ellipse $g '#4f894a' 340 130 78 86
        Fill-Ellipse $g '#3f783d' 316 162 94 86
        Draw-Line $g '#956b3d' 10 100 386 334 348
        Draw-Line $g '#c29a57' 6 110 404 344 366
    }
    else {
        Fill-GradientRect $g $bodyColor '#5b5f5b' 124 242 226 134
        Fill-Polygon $g '#455052' @((350,242),(402,270),(402,376),(350,376))
        Fill-Polygon $g $roofColor @((92,250),(256,138),(422,250),(350,276),(256,202),(124,276))
        Draw-RoofTiles $g 110 170 402 276 '#623a31'
        Fill-Rect $g $accentColor 226 316 58 60
        Draw-Windows $g 150 286 2
        Draw-Windows $g 294 286 1
        Draw-Banner $g 370 250 $trimColor
    }

    Fill-Ellipse $g '#f3d876' 238 382 36 18
    Save-Canvas $canvas (Join-Path $cityRoot $fileName)
}

function New-PalaceArt {
    $canvas = New-Canvas 640 640
    $g = $canvas.Graphics
    Fill-Ellipse $g '#343a31' 108 532 424 52
    Fill-Polygon $g '#607047' @((96,520),(320,416),(548,520),(320,602))
    Fill-GradientRect $g '#8d9898' '#636f70' 218 234 204 270
    Fill-GradientRect $g '#7f898a' '#596567' 118 298 92 206
    Fill-GradientRect $g '#7f898a' '#596567' 430 298 92 206
    Draw-StoneLines $g 218 234 204 270
    Draw-StoneLines $g 118 298 92 206
    Draw-StoneLines $g 430 298 92 206
    Fill-Polygon $g '#b74e43' @((190,240),(320,122),(450,240),(420,266),(320,176),(220,266))
    Fill-Polygon $g '#9e473d' @((94,304),(164,210),(234,304))
    Fill-Polygon $g '#9e473d' @((406,304),(476,210),(546,304))
    Draw-RoofTiles $g 210 152 430 266 '#793a33'
    for ($x = 244; $x -le 360; $x += 40) { Fill-Rect $g '#d8d8c5' $x 272 24 38 }
    Fill-Rect $g '#323a3c' 282 406 76 98
    Fill-Ellipse $g '#f1ca60' 290 330 60 60
    Draw-Banner $g 446 274 '#d1a23f'
    Draw-Banner $g 166 274 '#d1a23f'
    Stroke-Polygon $g '#495254' 5 @((218,234),(422,234),(422,504),(218,504))
    Save-Canvas $canvas (Join-Path $cityRoot 'keep.png')
}

function New-WallArt {
    $canvas = New-Canvas 1024 320
    $g = $canvas.Graphics
    Fill-Ellipse $g '#30372f' 68 244 884 42
    Fill-Polygon $g '#74806f' @((68,116),(512,70),(956,116),(956,232),(512,278),(68,232))
    Fill-Polygon $g '#5b665f' @((68,232),(512,278),(512,232),(68,186))
    Fill-Polygon $g '#8d958a' @((68,116),(512,70),(956,116),(512,162))
    for ($x = 96; $x -lt 920; $x += 70) {
        Fill-Rect $g '#a7aca1' $x 84 44 56
        Stroke-Rect $g '#6b736b' 2 $x 84 44 56
    }
    for ($x = 116; $x -lt 900; $x += 84) {
        Draw-Line $g '#58625c' 4 $x 142 ($x + 54) 244
    }
    Draw-Line $g '#dde2c5' 3 86 126 938 126
    Draw-Line $g '#464e49' 5 70 232 956 232
    Save-Canvas $canvas (Join-Path $cityRoot 'wall.png')
}

function New-FarmArt {
    $canvas = New-Canvas 512 512
    $g = $canvas.Graphics
    Fill-Ellipse $g '#343a31' 92 402 326 42
    Fill-Polygon $g '#b5c46f' @((78,394),(256,314),(434,394),(256,468))
    for ($x = 118; $x -lt 390; $x += 38) {
        Draw-Line $g '#f1d98a' 5 $x 404 ($x + 132) 348
    }
    Fill-GradientRect $g '#a96f42' '#835438' 178 260 142 120
    Fill-Polygon $g '#a64d39' @((150,266),(250,174),(352,266),(320,286),(250,220),(180,286))
    Draw-RoofTiles $g 166 202 336 286 '#744032'
    Fill-Rect $g '#5b4231' 226 320 48 60
    Fill-Ellipse $g '#e5c56e' 318 224 34 34
    Draw-Banner $g 120 264 '#d4a849'
    Save-Canvas $canvas (Join-Path $cityRoot 'farm.png')
}

function New-CourtyardArt {
    $canvas = New-Canvas 2048 1024
    $g = $canvas.Graphics
    Fill-Polygon $g '#90a76f' @((90,740),(1024,320),(1958,740),(1700,940),(348,940))
    Fill-Polygon $g '#6f805b' @((348,940),(1024,628),(1700,940),(1024,1010))
    Fill-Polygon $g '#737c72' @((132,732),(1024,446),(1916,732),(1916,810),(1024,524),(132,810))
    Fill-Polygon $g '#5f6c62' @((132,810),(1024,524),(1024,604),(132,890))
    Fill-Polygon $g '#879382' @((132,732),(1024,446),(1916,732),(1024,604))
    for ($x = 210; $x -lt 1850; $x += 132) {
        Fill-Rect $g '#a9afa4' $x 700 58 58
        Stroke-Rect $g '#687369' 2 $x 700 58 58
    }
    for ($x = 170; $x -lt 1860; $x += 160) {
        Draw-Line $g '#556257' 5 $x 910 ($x + 240) 690
    }
    Draw-Line $g '#d6d9bc' 4 150 750 1900 750
    Save-Canvas $canvas (Join-Path $cityRoot 'courtyard.png')
}

function New-CityBackdropArt {
    $canvas = New-Canvas 2048 1024 $false
    $g = $canvas.Graphics
    $sky = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.Rectangle]::new(0, 0, 2048, 1024),
        (Color-Hex '#7eb8c9'),
        (Color-Hex '#d7c08d'),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
    )
    $g.FillRectangle($sky, 0, 0, 2048, 1024)
    $sky.Dispose()
    Fill-Ellipse $g '#f2d978' 1660 64 190 190
    Fill-Polygon $g '#405f60' @((0,410),(200,190),(450,408),(645,120),(870,430),(1160,210),(1455,430),(1765,165),(2048,420),(2048,1024),(0,1024))
    Fill-Polygon $g '#668272' @((0,510),(300,340),(610,520),(900,330),(1190,535),(1590,350),(2048,530),(2048,1024),(0,1024))
    Fill-Polygon $g '#386f4a' @((0,650),(2048,575),(2048,1024),(0,1024))
    Fill-Polygon $g '#547f45' @((0,800),(2048,710),(2048,1024),(0,1024))
    Fill-Polygon $g '#376f84' @((0,870),(465,780),(900,840),(1330,770),(2048,850),(2048,1024),(0,1024))
    Fill-Polygon $g '#a5ba73' @((0,812),(465,735),(900,802),(1330,722),(2048,786),(2048,860),(1330,790),(900,880),(465,812),(0,905))
    Save-Canvas $canvas (Join-Path $cityRoot 'city_backdrop.png')
}

New-CityBackdropArt
New-CourtyardArt
New-PalaceArt
New-WallArt
New-FarmArt
New-PolishedBuilding 'barracks.png' 'hall' '#7d7061' '#4f6572' '#c49d48' '#4b3b34'
New-PolishedBuilding 'workshop.png' 'hall' '#81654d' '#b98745' '#d7b25b' '#4f5960'
New-PolishedBuilding 'lumberyard.png' 'yard' '#8a6546' '#516b5f' '#c6a05a' '#5d3f2c'
New-PolishedBuilding 'stable.png' 'hall' '#87674d' '#7b4239' '#c89c55' '#4c3430'
New-PolishedBuilding 'range.png' 'tent' '#6f7954' '#3f5d52' '#c9ad5b' '#93633f'
New-PolishedBuilding 'academy.png' 'tower' '#6f7890' '#42536d' '#d3b45f' '#2e3749'
New-PolishedBuilding 'hospital.png' 'hall' '#92877d' '#b35650' '#e0d6bd' '#d8ddd2'
New-PolishedBuilding 'tavern.png' 'hall' '#8f6549' '#8a4738' '#d1a34c' '#5b3a2c'
New-PolishedBuilding 'embassy.png' 'tower' '#6c7588' '#b9904f' '#cfad56' '#3f4d5a'
New-PolishedBuilding 'watchtower.png' 'tower' '#727b75' '#4d5f63' '#c6a553' '#364144'
New-PolishedBuilding 'forge.png' 'hall' '#6e5f54' '#9a4d3e' '#d58445' '#2f3334'
New-PolishedBuilding 'iron_mine.png' 'mine' '#77766e' '#4e565c' '#b58b52' '#c0713d'
New-PolishedBuilding 'silver_mine.png' 'mine' '#8b9290' '#59696d' '#c6d2d0' '#d8e7e5'
New-PolishedBuilding 'market.png' 'hall' '#8b6f4b' '#d09b4f' '#4c6b67' '#4e7f7b'
New-PolishedBuilding 'hall_of_war.png' 'tower' '#6f6370' '#8f483f' '#c39b53' '#5c3b42'
New-PolishedBuilding 'drill_grounds.png' 'tent' '#786b55' '#525f5e' '#caa65c' '#7a4836'
New-PolishedBuilding 'warehouse.png' 'hall' '#7a715f' '#69503d' '#b69867' '#4f4335'

$canvas = New-Canvas 256 256
$g = $canvas.Graphics
for ($radius = 88; $radius -ge 52; $radius -= 12) {
    $alpha = [int](28 + ((88 - $radius) * 2))
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb($alpha, 105, 255, 116))
    $g.FillEllipse($brush, 128 - $radius, 128 - [int]($radius * 0.45), $radius * 2, [int]($radius * 0.9))
    $brush.Dispose()
}
Save-Canvas $canvas (Join-Path $cityStateRoot 'ready_glow.png')

$canvas = New-Canvas 256 256
$g = $canvas.Graphics
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(120, 24, 28, 30))
$g.FillEllipse($brush, 42, 74, 172, 108)
$brush.Dispose()
Draw-Line $g '#c9ced0' 9 82 92 174 164
Draw-Line $g '#c9ced0' 9 174 92 82 164
Save-Canvas $canvas (Join-Path $cityStateRoot 'locked_overlay.png')

$canvas = New-Canvas 256 256
$g = $canvas.Graphics
$pen = Pen-Hex '#f0c650' 12
$g.DrawArc($pen, 46, 46, 164, 164, -25, 290)
$pen.Dispose()
$pen = Pen-Hex '#fff0a8' 6
$g.DrawArc($pen, 66, 66, 124, 124, 20, 145)
$pen.Dispose()
Fill-Polygon $g '#f0c650' @((198,74),(230,92),(194,111))
Save-Canvas $canvas (Join-Path $cityStateRoot 'upgrading_ring.png')

$canvas = New-Canvas 512 512
$g = $canvas.Graphics
Draw-Line $g '#7b5436' 8 128 372 390 190
Draw-Line $g '#7b5436' 8 150 192 404 374
Draw-Line $g '#d3a65a' 6 112 292 396 292
Draw-Line $g '#d3a65a' 6 134 244 374 244
Draw-Line $g '#d3a65a' 6 160 198 342 198
Draw-Banner $g 372 172 '#d6a23e'
Save-Canvas $canvas (Join-Path $cityStateRoot 'upgrading_scaffold.png')

function New-LevelTierOrnament(
    [string]$fileName,
    [int]$tier,
    [string]$flagColor,
    [string]$trimColor
) {
    $canvas = New-Canvas 320 190
    $g = $canvas.Graphics

    # These overlays sit above each building. They deliberately use a shared
    # silhouette so artists can replace one tier at a time without changing UI code.
    Fill-Polygon $g '#59635f' @((56,142),(160,100),(264,142),(232,166),(160,142),(88,166))
    Fill-Polygon $g '#8f9890' @((56,142),(160,100),(264,142),(160,154))
    Draw-Line $g '#43504d' 4 66 144 160 112
    Draw-Line $g '#43504d' 4 160 112 254 144
    Draw-Line $g $trimColor 4 80 145 240 145
    Fill-Rect $g '#6d4930' 112 60 7 88
    Fill-Polygon $g $flagColor @((119,66),(164,78),(119,94))

    if ($tier -ge 2) {
        Fill-Rect $g '#6d4930' 201 68 7 80
        Fill-Polygon $g $flagColor @((208,74),(250,86),(208,102))
        Fill-Ellipse $g $trimColor 151 111 18 18
    }
    if ($tier -ge 3) {
        Fill-Polygon $g '#707975' @((133,108),(160,62),(187,108))
        Fill-Polygon $g $trimColor @((151,82),(160,66),(169,82))
        Fill-Rect $g '#565f5c' 150 86 20 28
    }
    if ($tier -ge 4) {
        Fill-Polygon $g '#707975' @((83,126),(106,82),(129,126))
        Fill-Polygon $g '#707975' @((191,126),(214,82),(237,126))
        Draw-Line $g $trimColor 3 106 88 106 120
        Draw-Line $g $trimColor 3 214 88 214 120
    }
    if ($tier -ge 5) {
        Fill-Ellipse $g $trimColor 144 72 32 32
        Fill-Polygon $g '#fff0b6' @((160,78),(168,91),(160,104),(152,91))
        Draw-Line $g '#d2a65a' 3 98 54 222 54
    }

    Save-Canvas $canvas (Join-Path $cityStateRoot $fileName)
}

New-LevelTierOrnament 'level_tier_1.png' 1 '#b95445' '#d6af58'
New-LevelTierOrnament 'level_tier_2.png' 2 '#587d9b' '#e1bc63'
New-LevelTierOrnament 'level_tier_3.png' 3 '#5c8754' '#d9c26f'
New-LevelTierOrnament 'level_tier_4.png' 4 '#8b5c96' '#e2bc6e'
New-LevelTierOrnament 'level_tier_5.png' 5 '#b76b3d' '#f0cf74'

Write-Host "Generated placeholder Cocos art under $artRoot"
