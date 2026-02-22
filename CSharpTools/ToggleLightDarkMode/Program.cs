////////////////////////////////////////////////////////////////////////////////////////////////////
// ToggleLightDarkMode
//
// Toggles the Windows light/dark theme (the “Choose your color” setting),
// then sets the solid desktop background color to match the new theme.
//
// Tested on Windows 10 Build 19042.
// Persists through logout.
//
// Notes:
// - Theme bits live at:
//   HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
//     AppsUseLightTheme (DWORD 0/1)
//     SystemUsesLightTheme (DWORD 0/1)
// - Desktop background color uses IDesktopWallpaper (Windows 8+).

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal class Program
{
    // ------------------------------------------------------------
    // Define your two colors here
    // ------------------------------------------------------------
    private static readonly (byte R, byte G, byte B) LightBg = (187, 187, 187);
    private static readonly (byte R, byte G, byte B) DarkBg = (0, 0, 0);

    // HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
    private const string PersonalizeKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string AppsUseLightTheme = "AppsUseLightTheme";
    private const string SystemUsesLightTheme = "SystemUsesLightTheme";

    private static int Main(string[] args)
    {
        try
        {
            // ------------------------------------------------------------
            // Theme
            // ------------------------------------------------------------
            var (appsLight, systemLight) = ReadThemeBits();

            bool bothLight = appsLight == 1 && systemLight == 1;
            bool bothDark = appsLight == 0 && systemLight == 0;

            int newValue = bothLight ? 0 : 1; // Light->Dark, else -> Light

            WriteThemeBits(newValue, newValue);

            BroadcastSettingChange();

            // ------------------------------------------------------------
            // Desktop background
            // ------------------------------------------------------------
            bool nowLightTheme = newValue == 1;

            SetSolidDesktopBackground(nowLightTheme ? LightBg : DarkBg);

            Console.WriteLine(nowLightTheme ? "Set: Light theme + light desktop background"
                                           : "Set: Dark theme + dark desktop background");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }


    private static (int apps, int system) ReadThemeBits()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath, writable: false);

        // Default to Light if values are missing.
        int apps = ReadDwordOrDefault(key, AppsUseLightTheme, defaultValue: 1);
        int sys = ReadDwordOrDefault(key, SystemUsesLightTheme, defaultValue: 1);

        // Normalize to 0/1
        apps = apps != 0 ? 1 : 0;
        sys = sys != 0 ? 1 : 0;

        return (apps, sys);
    }

    private static int ReadDwordOrDefault(RegistryKey? key, string name, int defaultValue)
    {
        if (key is null) return defaultValue;

        object? v = key.GetValue(name);
        return v is int i ? i : defaultValue;
    }

    private static void WriteThemeBits(int appsUseLightTheme, int systemUsesLightTheme)
    {
        using var key = Registry.CurrentUser.CreateSubKey(PersonalizeKeyPath, writable: true)
                      ?? throw new InvalidOperationException("Failed to open/create Personalize registry key.");

        key.SetValue(AppsUseLightTheme, appsUseLightTheme, RegistryValueKind.DWord);
        key.SetValue(SystemUsesLightTheme, systemUsesLightTheme, RegistryValueKind.DWord);
    }

    // Best-effort “poke” to get Windows to notice quickly.
    private static void BroadcastSettingChange()
    {
        const int WM_SETTINGCHANGE = 0x001A;
        nint HWND_BROADCAST = 0xFFFF;

        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, nint.Zero, "ImmersiveColorSet",
            SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 200, out _);

        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, nint.Zero, "UserPreferencesMask",
            SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 200, out _);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint SendMessageTimeout(
        nint hWnd,
        int Msg,
        nint wParam,
        string lParam,
        SendMessageTimeoutFlags fuFlags,
        uint uTimeout,
        out nint lpdwResult);

    private static uint ToColorRef((byte R, byte G, byte B) c)
        => (uint)(c.B << 16 | c.G << 8 | c.R); // 0x00BBGGRR

    private static void SetSolidDesktopBackground((byte R, byte G, byte B) chosen)
    {
        var wallpaper = (IDesktopWallpaper)new DesktopWallpaper();

        // Ensure solid-color mode (clear wallpaper image)
        wallpaper.SetWallpaper(null, "");

        // Set background color. This persists through logout.
        wallpaper.SetBackgroundColor(ToColorRef(chosen));
    }
}

[Flags]
enum SendMessageTimeoutFlags : uint
{
    SMTO_NORMAL = 0x0000,
    SMTO_ABORTIFHUNG = 0x0002,
}

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

