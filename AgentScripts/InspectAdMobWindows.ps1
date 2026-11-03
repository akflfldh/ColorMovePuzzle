param([int]$EditorProcessId = 38340)
Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class AdMobWindowInspection {
 public delegate bool EnumProc(IntPtr hwnd, IntPtr param);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr param);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hwnd, EnumProc callback, IntPtr param);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd, StringBuilder text, int count);
}
'@
$items=[System.Collections.Generic.List[object]]::new()
$callback=[AdMobWindowInspection+EnumProc]{param($handle,$parameter)
 [uint32]$owner=0; [AdMobWindowInspection]::GetWindowThreadProcessId($handle,[ref]$owner) | Out-Null
 if($owner -eq $script:EditorProcessId){ $title=[System.Text.StringBuilder]::new(2048); $class=[System.Text.StringBuilder]::new(256)
 [AdMobWindowInspection]::GetWindowText($handle,$title,2048) | Out-Null; [AdMobWindowInspection]::GetClassName($handle,$class,256) | Out-Null
 if($title.Length -gt 0){ $script:items.Add([pscustomobject]@{handle=$handle.ToInt64();title=$title.ToString();class=$class.ToString()}) }
 }; return $true
}
[AdMobWindowInspection]::EnumWindows($callback,[IntPtr]::Zero) | Out-Null
$items | ConvertTo-Json
