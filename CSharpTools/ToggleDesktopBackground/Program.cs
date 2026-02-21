////////////////////////////////////////////////////////////////////////////////////////////////////
// ToggleDesktopBackground
//
// This app uses the IDesktopWallpaper COM interface (Windows 8+). Tested on Windows 10 Build 19042.
// The change takes a moment to appear, and persists through logout.

using System;
using System.Runtime.InteropServices;

// ------------------------------------------------------------
// Define your two colors here
// ------------------------------------------------------------
(byte R, byte G, byte B) Light = (187, 187, 187);
(byte R, byte G, byte B) Dark = (0, 0, 0);

static uint ToColorRef((byte R, byte G, byte B) c)
    => (uint)((c.B << 16) | (c.G << 8) | c.R); // 0x00BBGGRR

static bool SameColor(uint a, uint b) => a == b;

// ------------------------------------------------------------
// Run
// ------------------------------------------------------------
var wallpaper = (IDesktopWallpaper)new DesktopWallpaper();

uint lightRef = ToColorRef(Light);
uint darkRef = ToColorRef(Dark);

uint currentRef = wallpaper.GetBackgroundColor();

(byte R, byte G, byte B) chosen =
    SameColor(currentRef, lightRef) ? Dark :
    SameColor(currentRef, darkRef) ? Light :
    Light;

// Ensure solid-color mode (clear wallpaper image)
wallpaper.SetWallpaper(null, "");

// Set background color. This persists through logout.
wallpaper.SetBackgroundColor(ToColorRef(chosen));

Console.WriteLine($"Set desktop to {chosen.R},{chosen.G},{chosen.B}");


// ============================================================
// COM Interop (IMPORTANT: method order must match the native vtable)
// ============================================================

[ComImport]
[Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")]
class DesktopWallpaper
{
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
interface IDesktopWallpaper
{
    void SetWallpaper(
        [MarshalAs(UnmanagedType.LPWStr)] string? monitorID,
        [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetWallpaper(
        [MarshalAs(UnmanagedType.LPWStr)] string monitorID);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMonitorDevicePathAt(uint monitorIndex);

    uint GetMonitorDevicePathCount();

    void GetMonitorRECT(
        [MarshalAs(UnmanagedType.LPWStr)] string monitorID,
        out RECT displayRect);

    void SetBackgroundColor(uint color);

    uint GetBackgroundColor();

    void SetPosition(uint position);

    uint GetPosition();

    void SetSlideshow(IntPtr items);

    IntPtr GetSlideshow();

    void SetSlideshowOptions(uint options, uint slideshowTick);

    void GetSlideshowOptions(out uint options, out uint slideshowTick);

    void AdvanceSlideshow(
        [MarshalAs(UnmanagedType.LPWStr)] string monitorID,
        uint direction);

    uint GetStatus();

    void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
}

[StructLayout(LayoutKind.Sequential)]
struct RECT
{
    public int left, top, right, bottom;
}
