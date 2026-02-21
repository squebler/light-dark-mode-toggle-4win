////////////////////////////////////////////////////////////////////////////////////////////////////
// ToggleLightTheme
//
// Toggles the light-dark theme, also known as "Choose your color" setting.
// Tested on Windows 10 Build 19042.
// Persists through logout.
//
// It seems to make the change immediately; but ChatGPT says WM_SETTINGCHANGE might not work.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal static class Program
{
    // HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
    private const string PersonalizeKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string AppsUseLightTheme = "AppsUseLightTheme";
    private const string SystemUsesLightTheme = "SystemUsesLightTheme";

    private static int Main()
    {
        try
        {
            var (appsLight, systemLight) = ReadThemeBits();

            bool bothLight = appsLight == 1 && systemLight == 1;
            bool bothDark = appsLight == 0 && systemLight == 0;

            int newValue = bothLight ? 0 : 1; // Light->Dark, else -> Light

            WriteThemeBits(newValue, newValue);

            BroadcastSettingChange();

            Console.WriteLine(newValue == 1 ? "Set theme: Light" : "Set theme: Dark");
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
        if (v is int i) return i;

        return defaultValue;
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
        IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;

        // Common strings people use to trigger theme refresh. Not officially documented,
        // but harmless if ignored.
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet",
            SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 200, out _);

        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "UserPreferencesMask",
            SendMessageTimeoutFlags.SMTO_ABORTIFHUNG, 200, out _);
    }

    [Flags]
    private enum SendMessageTimeoutFlags : uint
    {
        SMTO_NORMAL = 0x0000,
        SMTO_ABORTIFHUNG = 0x0002,
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int Msg,
        IntPtr wParam,
        string lParam,
        SendMessageTimeoutFlags fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);
}