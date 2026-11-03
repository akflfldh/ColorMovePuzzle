param([string]$SigningDirectory = [System.IO.Path]::Combine($env:USERPROFILE,'UnitySigning','ColorMovePuzzle'))
# Run manually on the owning Windows account. Never call this in an automated log/capture job.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Windows.Forms
$metadata=Get-Content -LiteralPath (Join-Path $SigningDirectory 'upload-key.json') -Raw|ConvertFrom-Json
$plain=[System.Security.Cryptography.ProtectedData]::Unprotect([System.IO.File]::ReadAllBytes($metadata.passwordFile),$null,[System.Security.Cryptography.DataProtectionScope]::CurrentUser)
$form=New-Object System.Windows.Forms.Form
$form.Text='ColorMovePuzzle upload password - save in your password manager'; $form.Width=650; $form.Height=160
$box=New-Object System.Windows.Forms.TextBox
$box.Left=15; $box.Top=15; $box.Width=600; $box.ReadOnly=$true; $box.UseSystemPasswordChar=$true
$box.Text=[System.Text.Encoding]::UTF8.GetString($plain)
$reveal=New-Object System.Windows.Forms.CheckBox
$reveal.Left=15; $reveal.Top=50; $reveal.Width=580; $reveal.Text='Reveal locally to record securely (not written to console or clipboard)'
$reveal.Add_CheckedChanged({$box.UseSystemPasswordChar=-not $reveal.Checked})
$form.Controls.Add($box); $form.Controls.Add($reveal)
try{[void]$form.ShowDialog()}finally{$box.Text='';[Array]::Clear($plain,0,$plain.Length);$form.Dispose()}
