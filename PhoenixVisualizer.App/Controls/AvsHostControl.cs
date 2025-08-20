using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;

namespace PhoenixVisualizer.App.Controls;

/// <summary>
/// Hosts a Win32 child HWND inside Avalonia using the built-in NativeControlHost.
/// We create a "STATIC" control (predefined class) as the parent for AVS.
/// </summary>
public sealed class AvsHostControl : NativeControlHost
{
#if WINDOWS
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const string HWND_TYPE = "HWND";
    private nint _hwnd;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var parentHwnd = parent.Handle;
        // Create a simple STATIC child window that AVS will draw into
        _hwnd = CreateWindowExW(0, "STATIC", "",
            WS_CHILD | WS_VISIBLE, 0, 0, Math.Max(1, (int)Bounds.Width), Math.Max(1, (int)Bounds.Height),
            parentHwnd, 0, GetModuleHandleW(null), 0);
        return new PlatformHandle(_hwnd, HWND_TYPE);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (_hwnd != 0) { DestroyWindow(_hwnd); _hwnd = 0; }
        base.DestroyNativeControlCore(control);
    }

    public nint Hwnd => _hwnd;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        int exStyle, string className, string windowName,
        int style, int x, int y, int width, int height,
        nint parent, nint menu, nint hInstance, nint lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandleW(string? lpModuleName);
#else
    public nint Hwnd => 0;
#endif
}
