using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.System;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace DMBTimer.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x444D;
    private const uint WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;
    private static readonly UIntPtr SubclassId = new(0x444D4254);

    private IntPtr _windowHandle;
    private DispatcherQueue? _dispatcherQueue;
    private SubclassProcedure? _subclassProcedure;
    private bool _registered;

    public event Action? Invoked;

    public bool Register(Window window)
    {
        if (_registered)
            return true;

        _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _dispatcherQueue = window.DispatcherQueue;
        _subclassProcedure = WindowProcedure;

        if (!SetWindowSubclass(_windowHandle, _subclassProcedure, SubclassId, UIntPtr.Zero))
            return false;

        _registered = RegisterHotKey(
            _windowHandle,
            HotkeyId,
            ModControl | ModNoRepeat,
            (uint)VirtualKey.D);

        if (!_registered)
        {
            RemoveWindowSubclass(_windowHandle, _subclassProcedure, SubclassId);
            _subclassProcedure = null;
        }

        return _registered;
    }

    private IntPtr WindowProcedure(
        IntPtr windowHandle,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr referenceData)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _dispatcherQueue?.TryEnqueue(() => Invoked?.Invoke());
            return IntPtr.Zero;
        }

        return DefSubclassProc(windowHandle, message, wParam, lParam);
    }

    public void Dispose()
    {
        if (_registered)
            UnregisterHotKey(_windowHandle, HotkeyId);

        if (_subclassProcedure is not null && _windowHandle != IntPtr.Zero)
            RemoveWindowSubclass(_windowHandle, _subclassProcedure, SubclassId);

        _registered = false;
        _subclassProcedure = null;
        _windowHandle = IntPtr.Zero;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr SubclassProcedure(
        IntPtr windowHandle,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr referenceData);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(
        IntPtr windowHandle,
        SubclassProcedure procedure,
        UIntPtr subclassId,
        UIntPtr referenceData);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(
        IntPtr windowHandle,
        SubclassProcedure procedure,
        UIntPtr subclassId);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam);
}
