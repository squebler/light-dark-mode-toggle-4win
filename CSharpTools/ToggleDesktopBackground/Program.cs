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

// ------------------------------------------------------------
// Prompt
// ------------------------------------------------------------
Console.WriteLine("Choose desktop color:");
Console.WriteLine("0 = Light (187,187,187)");
Console.WriteLine("1 = Dark  (0,0,0)");
Console.Write("> ");

var input = Console.ReadLine();

(byte R, byte G, byte B) chosen = input switch
{
    "0" => Light,
    "1" => Dark,
    _ => default
};

if (chosen == default && input is not ("0" or "1"))
{
    Console.WriteLine("Invalid input.");
    return;
}

// COLORREF format = 0x00BBGGRR
uint colorRef = (uint)(chosen.B << 16 | chosen.G << 8 | chosen.R);

var wallpaper = (IDesktopWallpaper)new DesktopWallpaper();

// Ensure solid-color mode (clear wallpaper image)
wallpaper.SetWallpaper(null, "");

// Set background color. This persists through logout.
wallpaper.SetBackgroundColor(colorRef);

Console.WriteLine($"Set desktop to {chosen.R},{chosen.G},{chosen.B}");
Console.WriteLine("Press Enter to exit.");
Console.ReadLine();


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
