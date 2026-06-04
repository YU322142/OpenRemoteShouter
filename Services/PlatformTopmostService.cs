using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace RemoteShouter.Services;

public static class PlatformTopmostService
{
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpNoSendChanging = 0x0400;

    public static void Apply(Window window, bool topmost)
    {
        window.Topmost = topmost;

        if (OperatingSystem.IsWindows())
        {
            ApplyWindowsTopmost(window, topmost);
        }
    }

    public static void Reassert(Window window)
    {
        if (!window.Topmost)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            ApplyWindowsTopmost(window, true);
        }
    }

    private static void ApplyWindowsTopmost(Window window, bool topmost)
    {
        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _ = SetWindowPos(
            handle,
            topmost ? HwndTopmost : HwndNoTopmost,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoActivate | SwpNoOwnerZOrder | SwpNoSendChanging);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
