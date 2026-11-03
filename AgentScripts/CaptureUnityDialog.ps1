param([long]$WindowHandle,[string]$OutputFile)
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class UnityDialogCapture { [StructLayout(LayoutKind.Sequential)] public struct Rect { public int left,top,right,bottom; } [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Rect rect); [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags); [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value); }
'@
[UnityDialogCapture]::SetThreadDpiAwarenessContext([IntPtr](-4)) | Out-Null
$rect=New-Object UnityDialogCapture+Rect
[UnityDialogCapture]::GetWindowRect([IntPtr]$WindowHandle,[ref]$rect) | Out-Null
$bitmap=[System.Drawing.Bitmap]::new($rect.right-$rect.left,$rect.bottom-$rect.top)
$graphics=[System.Drawing.Graphics]::FromImage($bitmap); $dc=$graphics.GetHdc()
try { [UnityDialogCapture]::PrintWindow([IntPtr]$WindowHandle,$dc,2) | Out-Null } finally { $graphics.ReleaseHdc($dc) }
try { $bitmap.Save((Join-Path (Get-Location) $OutputFile)) } finally { $graphics.Dispose(); $bitmap.Dispose() }
