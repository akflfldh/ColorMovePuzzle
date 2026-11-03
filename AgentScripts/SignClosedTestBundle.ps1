param([string]$SigningDirectory = [System.IO.Path]::Combine($env:USERPROFILE,'UnitySigning','ColorMovePuzzle'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Security
$workspace=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unsigned=Join-Path $workspace 'Builds/Android/Intermediate/ColorMovePuzzle-ClosedTest-unsigned.aab'
$preflight=Get-Content -LiteralPath (Join-Path $workspace 'Artifacts/ClosedTest/BundlePreflight.json') -Raw | ConvertFrom-Json
if(-not $preflight.passed -or (Get-FileHash -LiteralPath $unsigned -Algorithm SHA256).Hash -ne $preflight.sha256){throw 'Validate the exact unsigned bundle before signing.'}
if($preflight.versionName -notmatch '^[A-Za-z0-9._-]+$' -or [int]$preflight.versionCode -lt 1){throw 'Unsupported version for artifact naming.'}
$signed=Join-Path $workspace ('Builds/Android/ColorMovePuzzle-ClosedTest-v'+$preflight.versionName+'-code'+$preflight.versionCode+'.aab')
$metadata=Get-Content -LiteralPath (Join-Path $SigningDirectory 'upload-key.json') -Raw | ConvertFrom-Json
if(-not(Test-Path -LiteralPath $metadata.keystore)){throw 'User upload keystore missing.'}
if(Test-Path -LiteralPath $signed){throw 'Preserve the existing signed artifact; choose a new version rather than overwrite it.'}
$plain=[System.Security.Cryptography.ProtectedData]::Unprotect([System.IO.File]::ReadAllBytes($metadata.passwordFile),$null,[System.Security.Cryptography.DataProtectionScope]::CurrentUser)
$password=[System.Text.Encoding]::UTF8.GetString($plain)
$start=New-Object System.Diagnostics.ProcessStartInfo
$start.FileName='C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/jarsigner.exe'
$start.Arguments='-keystore "'+$metadata.keystore+'" -storetype '+$metadata.storeType+' -storepass:env CMP_UPLOAD_STORE_PASS -keypass:env CMP_UPLOAD_STORE_PASS -digestalg SHA-256 -sigalg SHA256withRSA -signedjar "'+$signed+'" "'+$unsigned+'" '+$metadata.alias
$start.EnvironmentVariables['CMP_UPLOAD_STORE_PASS']=$password
$start.UseShellExecute=$false; $start.CreateNoWindow=$true
$start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
try{
    $process=[System.Diagnostics.Process]::Start($start)
    $stdout=$process.StandardOutput.ReadToEnd(); $stderr=$process.StandardError.ReadToEnd(); $process.WaitForExit()
    if($process.ExitCode -ne 0){throw 'AAB signing failed; no password is logged.'}
    [System.IO.File]::WriteAllText((Join-Path $workspace 'Artifacts/ClosedTest/Signing.txt'),$stdout+[Environment]::NewLine+$stderr)
    Copy-Item -LiteralPath (Join-Path $SigningDirectory 'upload-certificate.pem') -Destination (Join-Path $workspace 'Artifacts/ClosedTest/UploadCertificate.pem')
    [pscustomobject]@{path=$signed;versionName=$preflight.versionName;versionCode=$preflight.versionCode;bytes=(Get-Item -LiteralPath $signed).Length;sha256=(Get-FileHash -LiteralPath $signed -Algorithm SHA256).Hash} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $workspace 'Artifacts/ClosedTest/SignedArtifact.json') -Encoding UTF8
    Write-Output ('Signed AAB: '+$signed)
} finally {
    $start.EnvironmentVariables.Remove('CMP_UPLOAD_STORE_PASS'); [Array]::Clear($plain,0,$plain.Length); $password=$null
}
