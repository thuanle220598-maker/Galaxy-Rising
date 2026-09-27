param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$Source
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Source)) {
    $Source = (Resolve-Path (Join-Path $ProjectRoot '..\Model\Nova\Nova.pxo')).Path
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

$definitions = @(
    [pscustomobject]@{ Layer = 'Idle';         File = 'FireGodIdle.png';     Count = 8  },
    [pscustomobject]@{ Layer = 'Walk';         File = 'FireGodWalk.png';     Count = 8  },
    [pscustomobject]@{ Layer = 'Run';          File = 'FireGodRun.png';      Count = 13 },
    [pscustomobject]@{ Layer = 'Active Skill'; File = 'FireGodActive.png';   Count = 13 },
    [pscustomobject]@{ Layer = 'Ultimate';     File = 'FireGodUltimate.png'; Count = 13 },
    [pscustomobject]@{ Layer = 'Hit';          File = 'FireGodHit.png';      Count = 9  },
    [pscustomobject]@{ Layer = 'Death';        File = 'FireGodDeath.png';    Count = 9  }
)

$output = Join-Path $ProjectRoot 'Assets\Art\Characters\Nova\Combat'
$sourceOutput = Join-Path $ProjectRoot 'Assets\Art\Characters\Nova\Source'
New-Item -ItemType Directory -Force $output, $sourceOutput | Out-Null
$zip = [IO.Compression.ZipFile]::OpenRead($Source)
try {
    $dataEntry = $zip.GetEntry('data.json')
    if ($null -eq $dataEntry) { throw 'PXO data.json is missing.' }
    $reader = [IO.StreamReader]::new($dataEntry.Open())
    try { $data = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($data.size_x -ne 68 -or $data.size_y -ne 68) { throw 'PXO canvas must be 68x68.' }

    $report = @()
    foreach ($definition in $definitions) {
        $layerIndex = -1
        for ($index = 0; $index -lt $data.layers.Count; $index++) {
            if ($data.layers[$index].name -eq $definition.Layer) { $layerIndex = $index; break }
        }
        if ($layerIndex -lt 0) { throw "Missing PXO layer: $($definition.Layer)" }

        $frames = [Collections.Generic.List[byte[]]]::new()
        for ($frame = 1; $frame -le $data.frames.Count; $frame++) {
            $entry = $zip.GetEntry("image_data/frames/$frame/layer_$($layerIndex + 1)")
            if ($null -eq $entry) { throw "Missing cel for $($definition.Layer), frame $frame" }
            $binary = [IO.BinaryReader]::new($entry.Open())
            try { $bytes = $binary.ReadBytes([int]$entry.Length) } finally { $binary.Dispose() }
            if ($bytes.Length -ne 18496) { throw "Invalid cel length for $($definition.Layer), frame $frame" }

            $nonEmpty = $false
            for ($alpha = 3; $alpha -lt $bytes.Length; $alpha += 4) {
                if ($bytes[$alpha] -ne 0) { $nonEmpty = $true; break }
            }
            if ($nonEmpty) { $frames.Add($bytes) }
        }
        if ($frames.Count -ne $definition.Count) {
            throw "$($definition.Layer) expected $($definition.Count) frames, got $($frames.Count)."
        }

        $bitmap = [Drawing.Bitmap]::new(68 * $frames.Count, 68)
        try {
            for ($frame = 0; $frame -lt $frames.Count; $frame++) {
                $bytes = $frames[$frame]
                for ($y = 0; $y -lt 68; $y++) {
                    for ($x = 0; $x -lt 68; $x++) {
                        $offset = (($y * 68) + $x) * 4
                        $color = [Drawing.Color]::FromArgb(
                            $bytes[$offset + 3], $bytes[$offset], $bytes[$offset + 1], $bytes[$offset + 2])
                        $bitmap.SetPixel(($frame * 68) + $x, $y, $color)
                    }
                }
            }
            $bitmap.Save((Join-Path $output $definition.File), [Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
        $report += "$($definition.Layer)=$($frames.Count)"
    }
} finally { $zip.Dispose() }

Copy-Item -LiteralPath $Source -Destination (Join-Path $sourceOutput 'Nova.pxo') -Force
Write-Output ($report -join ' ')
Write-Output 'Exported Fire God PXO sheets successfully.'
