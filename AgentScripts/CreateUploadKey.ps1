param([string]$SigningDirectory = [System.IO.Path]::Combine($env:USERPROFILE, 'UnitySigning', 'ColorMovePuzzle'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
$workspace = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$signingPath = [System.IO.Path]::GetFullPath($SigningDirectory)
if ($signingPath.Equals($workspace,[System.StringComparison]::OrdinalIgnoreCase) -or $signingPath.StartsWith($workspace.TrimEnd('\')+'\',[System.StringComparison]::OrdinalIgnoreCase)) { throw 'The signing directory must be outside the project.' }
$keyFile = Join-Path $signingPath 'colormove-upload.keystore'
$secretFile = Join-Path $signingPath 'upload-password.dpapi'
$metadataFile = Join-Path $signingPath 'upload-key.json'
$certificateFile = Join-Path $signingPath 'upload-certificate.pem'
foreach($path in $keyFile,$secretFile,$metadataFile,$certificateFile) { if(Test-Path -LiteralPath $path) {throw 'Existing signing material detected; reuse it rather than overwriting it.'} }
$jdk = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin'
$keytool = Join-Path $jdk 'keytool.exe'
if(-not(Test-Path -LiteralPath $keytool)){throw 'Unity JDK keytool is missing.'}
New-Item -ItemType Directory -Force -Path $signingPath | Out-Null
$sid=[System.Security.Principal.WindowsIdentity]::GetCurrent().User
$acl=New-Object System.Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true,$false)
$acl.SetOwner($sid)
foreach($identity in @($sid.Value,'S-1-5-18','S-1-5-32-544')){
    $rule=New-Object System.Security.AccessControl.FileSystemAccessRule([System.Security.Principal.SecurityIdentifier]$identity,'FullControl','ContainerInherit,ObjectInherit','None','Allow')
    $acl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $signingPath -AclObject $acl
$random=New-Object byte[] 32
$rng=[System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($random); $rng.Dispose()
$password=[Convert]::ToBase64String($random)
$plain=[System.Text.Encoding]::UTF8.GetBytes($password)
$protected=[System.Security.Cryptography.ProtectedData]::Protect($plain,$null,[System.Security.Cryptography.DataProtectionScope]::CurrentUser)
[System.IO.File]::WriteAllBytes($secretFile,$protected)
function Invoke-Keytool([string]$arguments){
    $start=New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName=$keytool; $start.Arguments=$arguments
    $start.UseShellExecute=$false; $start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
    $start.EnvironmentVariables['CMP_UPLOAD_STORE_PASS']=$password
    try {
        $process=[System.Diagnostics.Process]::Start($start)
        $stdout=$process.StandardOutput.ReadToEnd(); $stderr=$process.StandardError.ReadToEnd(); $process.WaitForExit()
        if($process.ExitCode -ne 0){throw 'Keytool failed. Signing material was preserved for investigation; no password is logged.'}
        return $stdout
    } finally {
        $start.EnvironmentVariables.Remove('CMP_UPLOAD_STORE_PASS')
        if($process){$process.Dispose()}
    }
}
try {
    Invoke-Keytool ('-genkeypair -alias colormove-upload -keyalg RSA -keysize 3072 -sigalg SHA256withRSA -validity 10000 -dname "CN=ColorMovePuzzle Upload" -storetype PKCS12 -keystore "'+$keyFile+'" -storepass:env CMP_UPLOAD_STORE_PASS -keypass:env CMP_UPLOAD_STORE_PASS') | Out-Null
    Invoke-Keytool ('-exportcert -rfc -alias colormove-upload -keystore "'+$keyFile+'" -storepass:env CMP_UPLOAD_STORE_PASS -file "'+$certificateFile+'"') | Out-Null
    $metadata=[ordered]@{keystore=$keyFile;alias='colormove-upload';storeType='PKCS12';algorithm='RSA';bits=3072;validityDays=10000;passwordProtection='Windows DPAPI CurrentUser';passwordFile=$secretFile;createdUtc=[DateTime]::UtcNow.ToString('o')}
    [System.IO.File]::WriteAllText($metadataFile,($metadata|ConvertTo-Json),[System.Text.Encoding]::UTF8)
    Write-Output ('Upload key created outside the project: '+$keyFile)
    Write-Output 'Password generated randomly and protected with Windows DPAPI; it was not printed or stored in plaintext.'
} finally {
    [Array]::Clear($random,0,$random.Length); [Array]::Clear($plain,0,$plain.Length)
    $password=$null
}
