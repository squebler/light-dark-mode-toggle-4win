# set-desktop-solid-color.ps1
# Sets your desktop background to a solid color and applies it immediately.
# This was tested to work (immediately) in Windows 10 build 19042. Didn't test
# logout/in persistence.
# Edit R/G/B below if you want a different color.

#--------------- CONFIG: choose your color 0..255 ----------------#
[int]$R = 187
[int]$G = 187
[int]$B = 187
#-----------------------------------------------------------------#

$ErrorActionPreference = 'Stop'

# 1) Persist the color so it survives sign-out/reboot
$bg = "$R $G $B"
Set-ItemProperty -Path 'HKCU:\Control Panel\Colors' -Name 'Background' -Value $bg

# 2) Ensure Windows is in "solid color" mode (no wallpaper)
Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'WallPaper' -Value ''

# 3) Apply the change immediately (no Explorer restart)
#    This refreshes per-user desktop parameters.
rundll32.exe user32.dll, UpdatePerUserSystemParameters

# 4) Instant visual update using SetSysColors (doesn't persist by itself,
#    but combined with steps above you get both instant + persistent)
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class PInvoke {
  [DllImport("user32.dll")] public static extern bool SetSysColors(int cElements, int[] lpaElements, int[] lpaRgbValues);
}
"@

# COLOR_BACKGROUND index = 1; SetSysColors expects 0x00BBGGRR format
[int]$rgb = ($B -shl 16) -bor ($G -shl 8) -bor $R
[void][PInvoke]::SetSysColors(1, @(1), @($rgb))

# Optional: tiny delay + re-run parameter update to catch any stragglers
Start-Sleep -Milliseconds 150
rundll32.exe user32.dll, UpdatePerUserSystemParameters
