using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PeP.ExamApp;

/// <summary>
/// Comprehensive exam mode manager with SafeExamBrowser-like lockdown features.
/// Handles taskbar hiding, keyboard hooks, focus protection, and system lockdown.
/// </summary>
public sealed class ExamModeManager : IDisposable
{
    private readonly Window _window;
    private bool _isActive;
    private bool _disposed;
    private bool _hooksSuspended;

    // Previous window state
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;
    private bool _previousTopmost;
    private bool _previousShowInTaskbar;

    // Keyboard hook
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _keyboardProc;
    private GCHandle _keyboardProcHandle;

    // Focus monitoring
    private IntPtr _winEventHook = IntPtr.Zero;
    private NativeMethods.WinEventDelegate? _winEventProc;
    private GCHandle _winEventProcHandle;

    // Monitoring timer
    private DispatcherTimer? _monitorTimer;

    // Shell windows
    private IntPtr _taskbarHandle;
    private IntPtr _startButtonHandle;
    private IntPtr _trayHandle;

    // Clipboard monitoring
    private IntPtr _clipboardListenerHandle;

    public ExamModeManager(Window window)
    {
        _window = window;
    }

    public bool IsActive => _isActive;

    public event EventHandler? SecurityViolationDetected;

    /// <summary>
    /// Temporarily suspends keyboard and focus hooks to allow dialog input.
    /// </summary>
    public void SuspendHooks()
    {
        _hooksSuspended = true;
    }

    /// <summary>
    /// Resumes keyboard and focus hooks after dialog is closed.
    /// </summary>
    public void ResumeHooks()
    {
        _hooksSuspended = false;
    }

    public void Enter()
    {
        if (_isActive) return;
        _isActive = true;

        // Save previous state
        _previousWindowState = _window.WindowState;
        _previousWindowStyle = _window.WindowStyle;
        _previousResizeMode = _window.ResizeMode;
        _previousTopmost = _window.Topmost;
        _previousShowInTaskbar = _window.ShowInTaskbar;

        // Configure window for exam mode
        _window.WindowStyle = WindowStyle.None;
        _window.ResizeMode = ResizeMode.NoResize;
        _window.Topmost = true;
        _window.ShowInTaskbar = false;
        _window.WindowState = WindowState.Maximized;

        // Apply all security measures
        ApplyCaptureProtection();
        HideTaskbar();
        InstallKeyboardHook();
        InstallFocusMonitor();
        ClearClipboard();
        StartMonitoring();
        DisableAccessibilityShortcuts();

        // Force focus to our window
        var handle = new WindowInteropHelper(_window).Handle;
        NativeMethods.SetForegroundWindow(handle);
        NativeMethods.BringWindowToTop(handle);
    }

    public void Exit()
    {
        if (!_isActive) return;
        _isActive = false;

        // Remove all security measures
        StopMonitoring();
        UninstallFocusMonitor();
        UninstallKeyboardHook();
        ShowTaskbar();
        RemoveCaptureProtection();
        EnableAccessibilityShortcuts();

        // Restore window state
        _window.Topmost = _previousTopmost;
        _window.WindowState = _previousWindowState;
        _window.ResizeMode = _previousResizeMode;
        _window.WindowStyle = _previousWindowStyle;
        _window.ShowInTaskbar = _previousShowInTaskbar;
    }

    #region Capture Protection

    private void ApplyCaptureProtection()
    {
        var handle = new WindowInteropHelper(_window).Handle;
        if (handle == IntPtr.Zero) return;

        // Try Windows 10 2004+ exclude from capture first, then fallback to monitor affinity
        if (!NativeMethods.SetWindowDisplayAffinity(handle, NativeMethods.WDA_EXCLUDEFROMCAPTURE))
        {
            NativeMethods.SetWindowDisplayAffinity(handle, NativeMethods.WDA_MONITOR);
        }
    }

    private void RemoveCaptureProtection()
    {
        var handle = new WindowInteropHelper(_window).Handle;
        if (handle == IntPtr.Zero) return;
        NativeMethods.SetWindowDisplayAffinity(handle, NativeMethods.WDA_NONE);
    }

    #endregion

    #region Taskbar Control

    private void HideTaskbar()
    {
        try
        {
            // Find and hide the main taskbar
            _taskbarHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (_taskbarHandle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_taskbarHandle, NativeMethods.SW_HIDE);
                NativeMethods.SetWindowPos(_taskbarHandle, IntPtr.Zero, 0, 0, 0, 0,
                    NativeMethods.SWP_HIDEWINDOW | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }

            // Find and hide the Start button (Windows 10/11)
            _startButtonHandle = NativeMethods.FindWindow("Button", "Start");
            if (_startButtonHandle == IntPtr.Zero)
            {
                _startButtonHandle = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Button", null);
            }
            if (_startButtonHandle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_startButtonHandle, NativeMethods.SW_HIDE);
            }

            // Hide secondary taskbars (multi-monitor)
            IntPtr secondaryTaskbar;
            while ((secondaryTaskbar = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(secondaryTaskbar, NativeMethods.SW_HIDE);
            }

            // Hide system tray overflow window
            _trayHandle = NativeMethods.FindWindow("NotifyIconOverflowWindow", null);
            if (_trayHandle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_trayHandle, NativeMethods.SW_HIDE);
            }

            // Hide Cortana/Search
            var cortana = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", "Cortana");
            if (cortana != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(cortana, NativeMethods.SW_HIDE);
            }

            var search = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", "Search");
            if (search != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(search, NativeMethods.SW_HIDE);
            }

            // Hide Task View button area
            var taskView = NativeMethods.FindWindow("Windows.UI.Core.CoreWindow", "Task View");
            if (taskView != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(taskView, NativeMethods.SW_HIDE);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error hiding taskbar: {ex.Message}");
        }
    }

    private void ShowTaskbar()
    {
        try
        {
            if (_taskbarHandle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_taskbarHandle, NativeMethods.SW_SHOW);
                NativeMethods.SetWindowPos(_taskbarHandle, IntPtr.Zero, 0, 0, 0, 0,
                    NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }

            if (_startButtonHandle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_startButtonHandle, NativeMethods.SW_SHOW);
            }

            if (_trayHandle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(_trayHandle, NativeMethods.SW_SHOW);
            }

            // Show secondary taskbars
            IntPtr secondaryTaskbar = IntPtr.Zero;
            while ((secondaryTaskbar = NativeMethods.FindWindowEx(IntPtr.Zero, secondaryTaskbar, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(secondaryTaskbar, NativeMethods.SW_SHOW);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing taskbar: {ex.Message}");
        }
    }

    #endregion

    #region Keyboard Hook

    private void InstallKeyboardHook()
    {
        _keyboardProc = KeyboardHookCallback;
        _keyboardProcHandle = GCHandle.Alloc(_keyboardProc);

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        if (curModule != null)
        {
            _keyboardHookId = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _keyboardProc,
                NativeMethods.GetModuleHandle(curModule.ModuleName),
                0);
        }
    }

    private void UninstallKeyboardHook()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_keyboardProcHandle.IsAllocated)
        {
            _keyboardProcHandle.Free();
        }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // If hooks are suspended (e.g., for password dialog), allow all input
        if (_hooksSuspended)
        {
            return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        if (nCode >= 0 && _isActive)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var vkCode = hookStruct.vkCode;

            // Block Windows key
            if (vkCode == NativeMethods.VK_LWIN || vkCode == NativeMethods.VK_RWIN)
            {
                return (IntPtr)1;
            }

            // Block Alt+Tab
            if (vkCode == NativeMethods.VK_TAB && (hookStruct.flags & 0x20) != 0) // LLKHF_ALTDOWN
            {
                return (IntPtr)1;
            }

            // Block Alt+Esc
            if (vkCode == NativeMethods.VK_ESCAPE && (hookStruct.flags & 0x20) != 0)
            {
                return (IntPtr)1;
            }

            // Block Alt+F4
            if (vkCode == NativeMethods.VK_F4 && (hookStruct.flags & 0x20) != 0)
            {
                return (IntPtr)1;
            }
            // Block Ctrl+Alt+Delete (can't fully block, but we can detect)
            // Block Ctrl+Shift+Esc (Task Manager)
            bool ctrlPressed = (NativeMethods.GetSystemMetrics(0) & 0x8000) != 0; // Check if Ctrl is pressed

            // Block Print Screen
            if (vkCode == NativeMethods.VK_SNAPSHOT)
            {
                ClearClipboard(); // Clear any potential screenshot
                return (IntPtr)1;
            }

            // Block F1-F12 (except what might be needed)
            if (vkCode >= NativeMethods.VK_F1 && vkCode <= NativeMethods.VK_F12)
            {
                // Allow F5 for refresh within the exam
                if (vkCode != NativeMethods.VK_F5)
                {
                    return (IntPtr)1;
                }
            }

            // Block Escape key
            if (vkCode == NativeMethods.VK_ESCAPE)
            {
                return (IntPtr)1;
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    #endregion

    #region Focus Monitor

    private void InstallFocusMonitor()
    {
        _winEventProc = WinEventCallback;
        _winEventProcHandle = GCHandle.Alloc(_winEventProc);

        _winEventHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _winEventProc,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);
    }

    private void UninstallFocusMonitor()
    {
        if (_winEventHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        }

        if (_winEventProcHandle.IsAllocated)
        {
            _winEventProcHandle.Free();
        }
    }

    private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (!_isActive || _hooksSuspended) return;

        var ourHandle = new WindowInteropHelper(_window).Handle;
        if (hwnd != ourHandle && hwnd != IntPtr.Zero)
        {
            // Check if the focused window is a child dialog of our app
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            using var currentProcess = Process.GetCurrentProcess();
            
            // If it's our own process (e.g., a dialog), don't trigger violation
            if (processId == currentProcess.Id)
            {
                return;
            }

            // Another window got focus, reclaim it
            _window.Dispatcher.BeginInvoke(() =>
            {
                if (_isActive && !_hooksSuspended)
                {
                    NativeMethods.SetForegroundWindow(ourHandle);
                    NativeMethods.BringWindowToTop(ourHandle);
                    _window.Activate();

                    // Re-hide taskbar in case it appeared
                    HideTaskbar();

                    SecurityViolationDetected?.Invoke(this, EventArgs.Empty);
                }
            });
        }
    }

    #endregion

    #region Monitoring

    private void StartMonitoring()
    {
        _monitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _monitorTimer.Tick += OnMonitorTick;
        _monitorTimer.Start();
    }

    private void StopMonitoring()
    {
        if (_monitorTimer != null)
        {
            _monitorTimer.Stop();
            _monitorTimer.Tick -= OnMonitorTick;
            _monitorTimer = null;
        }
    }

    private void OnMonitorTick(object? sender, EventArgs e)
    {
        if (!_isActive || _hooksSuspended) return;

        // Ensure window stays maximized and on top
        if (_window.WindowState != WindowState.Maximized)
        {
            _window.WindowState = WindowState.Maximized;
        }

        // Re-ensure taskbar is hidden
        if (_taskbarHandle != IntPtr.Zero && NativeMethods.IsWindowVisible(_taskbarHandle))
        {
            HideTaskbar();
        }

        // Ensure we have focus (but check if dialog is showing)
        var ourHandle = new WindowInteropHelper(_window).Handle;
        var foreground = NativeMethods.GetForegroundWindow();
        
        // Check if foreground window belongs to our process
        NativeMethods.GetWindowThreadProcessId(foreground, out uint processId);
        using var currentProcess = Process.GetCurrentProcess();
        
        if (foreground != ourHandle && processId != currentProcess.Id)
        {
            NativeMethods.SetForegroundWindow(ourHandle);
            NativeMethods.BringWindowToTop(ourHandle);
        }

        // Check for new suspicious processes
        var report = SecurityChecks.RunFullSecurityCheck();
        if (report.BlacklistedProcesses.Count > 0 || report.IsDebuggerAttached || report.IsScreenRecording)
        {
            SecurityViolationDetected?.Invoke(this, EventArgs.Empty);
        }

        // Clear clipboard periodically
        ClearClipboard();
    }

    #endregion

    #region Clipboard Control

    private void ClearClipboard()
    {
        try
        {
            _clipboardListenerHandle = new WindowInteropHelper(_window).Handle;
            if (NativeMethods.OpenClipboard(_clipboardListenerHandle))
            {
                NativeMethods.EmptyClipboard();
                NativeMethods.CloseClipboard();
            }
        }
        catch { }
    }

    #endregion

    #region Accessibility Shortcuts

    private void DisableAccessibilityShortcuts()
    {
        try
        {
            // Disable Sticky Keys, Filter Keys, Toggle Keys via registry
            // Note: These require elevated privileges
            using var stickyKeys = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\StickyKeys", true);
            stickyKeys?.SetValue("Flags", "506"); // Disable shortcut

            using var filterKeys = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\Keyboard Response", true);
            filterKeys?.SetValue("Flags", "122"); // Disable shortcut

            using var toggleKeys = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\ToggleKeys", true);
            toggleKeys?.SetValue("Flags", "58"); // Disable shortcut
        }
        catch { }
    }

    private void EnableAccessibilityShortcuts()
    {
        try
        {
            using var stickyKeys = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\StickyKeys", true);
            stickyKeys?.SetValue("Flags", "510"); // Re-enable shortcut

            using var filterKeys = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\Keyboard Response", true);
            filterKeys?.SetValue("Flags", "126"); // Re-enable shortcut

            using var toggleKeys = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\ToggleKeys", true);
            toggleKeys?.SetValue("Flags", "62"); // Re-enable shortcut
        }
        catch { }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Exit();
        GC.SuppressFinalize(this);
    }

    ~ExamModeManager()
    {
        Dispose();
    }

    #endregion
}

