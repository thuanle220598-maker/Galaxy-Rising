param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$Source
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Source)) {
    $Source = (Resolve-Path (Join-Path $ProjectRoot '..\Model\Nova\Nova.pxo')).Path
}

& (Join-Path $PSScriptRoot 'Export-FireGodPxo.ps1') -ProjectRoot $ProjectRoot -Source $Source | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

$zip = [IO.Compression.ZipFile]::OpenRead($Source)
$bitmap = $null
try {
    $reader = [IO.StreamReader]::new($zip.GetEntry('data.json').Open())
    try { $data = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    $layerIndex = [Array]::FindIndex([object[]]$data.layers, [Predicate[object]] { param($layer) $layer.name -eq 'Idle' })

    foreach ($frame in 1..$data.frames.Count) {
        $entry = $zip.GetEntry("image_data/frames/$frame/layer_$($layerIndex + 1)")
        $binary = [IO.BinaryReader]::new($entry.Open())
        try { $bytes = $binary.ReadBytes([int]$entry.Length) } finally { $binary.Dispose() }
        if (($bytes | Where-Object { $_ -ne 0 } | Select-Object -First 1) -ne $null) { break }
    }

    $bitmap = [Drawing.Bitmap]::new((Join-Path $ProjectRoot 'Assets\Art\Characters\Nova\Combat\FireGodIdle.png'))
    for ($y = 0; $y -lt 68; $y++) {
        for ($x = 0; $x -lt 68; $x++) {
            $offset = (($y * 68) + $x) * 4
            $expected = [Drawing.Color]::FromArgb(
                $bytes[$offset + 3], $bytes[$offset], $bytes[$offset + 1], $bytes[$offset + 2])
            if ($bitmap.GetPixel($x, $y).ToArgb() -ne $expected.ToArgb()) {
                throw "Fire God frame is vertically inverted at ($x, $y)."
            }
        }
    }
} finally {
    if ($null -ne $bitmap) { $bitmap.Dispose() }
    $zip.Dispose()
}

Write-Output 'Fire God PXO orientation check passed.'
