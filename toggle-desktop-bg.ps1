# toggle-desktop-bg.ps1
# Toggle desktop background between 73% light gray and black (solid color).
# Instant apply + persistent across sessions.
# This was tested to work (immediately) in Windows 10 build 19042. Didn't test
# logout/in persistence.

$ErrorActionPreference = 'Stop'

# --- Presets ---
[int[]]$Light = 187,187,187   # 73% of 255 ≈ 187
[int[]]$Black = 0,0,0

$colorsKey  = 'HKCU:\Control Panel\Colors'
$desktopKey = 'HKCU:\Control Panel\Desktop'

function Apply-Background([int[]]$rgb) {
    $bgString = "$($rgb[0]) $($rgb[1]) $($rgb[2])"

    # Persist the color and force "solid color" mode
    New-Item -Path $colorsKey -Force | Out-Null
    Set-ItemProperty -Path $colorsKey  -Name Background -Value $bgString
    New-Item -Path $desktopKey -Force | Out-Null
    Set-ItemProperty -Path $desktopKey -Name WallPaper  -Value ''

    # Nudge Windows to reload per-user desktop params
    rundll32.exe user32.dll, UpdatePerUserSystemParameters

    # Instant visual update via SetSysColors (0x00BBGGRR format)
    if (-not ("PInvoke" -as [type])) {
        Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class PInvoke {
  [DllImport("user32.dll")] public static extern bool SetSysColors(int cElements, int[] lpaElements, int[] lpaRgbValues);
}
"@
    }

    $r = [int]$rgb[0]; $g = [int]$rgb[1]; $b = [int]$rgb[2]
    [int]$packed = ($b -shl 16) -bor ($g -shl 8) -bor $r
    [void][PInvoke]::SetSysColors(1, @(1), @($packed))

    Start-Sleep -Milliseconds 150
    rundll32.exe user32.dll, UpdatePerUserSystemParameters
}

# --- Read current state ---
$curBgString = (Get-ItemProperty -Path $colorsKey -Name Background -ErrorAction SilentlyContinue).Background
$wallpaper   = (Get-ItemProperty -Path $desktopKey -Name WallPaper  -ErrorAction SilentlyContinue).WallPaper
$lightString = "$($Light[0]) $($Light[1]) $($Light[2])"

# --- Decide next color ---
# If *any* wallpaper path is set, treat as "not light gray" so first run flips to light gray.
$target =
    if ($wallpaper -and $wallpaper.Trim() -ne '') { $Light }
    elseif ($curBgString -eq $lightString)        { $Black }
    else                                          { $Light }

Apply-Background $target

Write-Host ("Set desktop to: {0}" -f (("$($target[0]),$($target[1]),$($target[2])")))
