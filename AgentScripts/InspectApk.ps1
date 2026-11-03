param(
    [string]$ApkFile = 'Builds/Android/ColorMovePuzzle-Development.apk',
    [string]$ReportFile = 'Artifacts/Android/ApkInspection.json'
)
$ErrorActionPreference = 'Stop'
$apkPath = if ([System.IO.Path]::IsPathRooted($ApkFile)) { $ApkFile } else { Join-Path (Get-Location).Path $ApkFile }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($apkPath)
try {
    $libraries = foreach ($entry in $archive.Entries) {
        if ($entry.FullName -notmatch '^lib/arm64-v8a/[^/]+\.so$') { continue }
        $stream = $entry.Open()
        try {
            $buffer = New-Object byte[] 65536
            $read = 0
            while ($read -lt $buffer.Length) {
                $chunk = $stream.Read($buffer, $read, $buffer.Length - $read)
                if ($chunk -eq 0) { break }
                $read += $chunk
            }
        } finally { $stream.Dispose() }
        if ($read -lt 64 -or $buffer[0] -ne 127 -or $buffer[1] -ne 69 -or $buffer[2] -ne 76 -or $buffer[3] -ne 70 -or $buffer[4] -ne 2 -or $buffer[5] -ne 1) { throw "Expected little-endian ELF64 in $($entry.FullName)" }
        $machine = [BitConverter]::ToUInt16($buffer,18)
        $programOffset = [BitConverter]::ToUInt64($buffer,32)
        $programSize = [BitConverter]::ToUInt16($buffer,54)
        $programCount = [BitConverter]::ToUInt16($buffer,56)
        if ($programOffset + $programSize * $programCount -gt $read) { throw 'ELF program table exceeds the inspected header.' }
        $segments = for ($i=0; $i -lt $programCount; $i++) {
            $offset = [int]($programOffset + $i*$programSize)
            if ([BitConverter]::ToUInt32($buffer,$offset) -ne 1) { continue }
            $alignment = [BitConverter]::ToUInt64($buffer,$offset+48)
            $fileOffset = [BitConverter]::ToUInt64($buffer,$offset+8)
            $virtualAddress = [BitConverter]::ToUInt64($buffer,$offset+16)
            [pscustomobject]@{alignment=$alignment;page16KCompatible=($alignment -ge 16384 -and $fileOffset % 16384 -eq $virtualAddress % 16384)}
        }
        [pscustomobject]@{path=$entry.FullName;bytes=$entry.Length;elfMachine=$machine;arm64=($machine -eq 183);loadSegments=@($segments);page16KCompatible=(@($segments | Where-Object {-not $_.page16KCompatible}).Count -eq 0)}
    }
} finally { $archive.Dispose() }
$apk = Get-Item -LiteralPath $apkPath
$result = [pscustomobject]@{apk=$apk.FullName;bytes=$apk.Length;sha256=(Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash;libraries=@($libraries);arm64Only=(@($libraries | Where-Object {-not $_.arm64}).Count -eq 0);elf16KCompatible=(@($libraries | Where-Object {-not $_.page16KCompatible}).Count -eq 0)}
$result | ConvertTo-Json -Depth 7 | Set-Content -Encoding UTF8 $ReportFile
$result | Select-Object apk,bytes,sha256,arm64Only,elf16KCompatible
if (-not $result.arm64Only -or -not $result.elf16KCompatible) { throw 'APK native library inspection failed.' }
