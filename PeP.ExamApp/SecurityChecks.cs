using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace PeP.ExamApp;

/// <summary>
/// Comprehensive security checks for exam integrity.
/// </summary>
public static class SecurityChecks
{
    #region Admin Check

    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Monitor Check

    public static int GetMonitorCount()
    {
        try
        {
            var monitors = NativeMethods.GetSystemMetrics(NativeMethods.SM_CMONITORS);
            return monitors > 0 ? monitors : 1;
        }
        catch
        {
            return 1;
        }
    }

    #endregion

    #region Remote Session Check

    public static bool IsRemoteSession()
    {
        try
        {
            return NativeMethods.GetSystemMetrics(NativeMethods.SM_REMOTESESSION) != 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Virtual Machine Detection

    private static readonly string[] VmProcesses = {
        // VMware
        "vmtoolsd", "vmwaretray", "vmwareuser",
        // VirtualBox
        "vboxservice", "vboxtray", "vboxclient",
        // QEMU/KVM
        "qemu-ga", "spice-vdagent",
        // Xen
        "xenservice", "xensvc",
        // Parallels
        "prl_tools", "prl_cc"
        // Note: We don't include hyperv/vmcompute/vmms as these run on host Windows too
    };

    // Only check registry keys that ONLY exist inside VMs, not on host machines
    private static readonly string[] VmRegistryKeys = {
        // VMware Guest - only exists inside a VMware VM
        @"SOFTWARE\VMware, Inc.\VMware Tools",
        // VirtualBox Guest - only exists inside VirtualBox
        @"SOFTWARE\Oracle\VirtualBox Guest Additions",
        @"HARDWARE\ACPI\DSDT\VBOX__",
        @"HARDWARE\ACPI\FADT\VBOX__",
        @"HARDWARE\ACPI\RSDT\VBOX__",
        // Hyper-V Guest Parameters - only exists inside Hyper-V VM, not on host
        @"SOFTWARE\Microsoft\Virtual Machine\Guest\Parameters"
        // Note: vmbus, VMBusHID, hypervideo can exist on Windows hosts with Hyper-V enabled
    };

    public static (bool IsVM, string? VmType) DetectVirtualMachine()
    {
        // Check WMI for VM indicators - most reliable method
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                var manufacturer = obj["Manufacturer"]?.ToString()?.ToLower() ?? "";
                var model = obj["Model"]?.ToString()?.ToLower() ?? "";

                if (manufacturer.Contains("vmware") || model.Contains("vmware"))
                    return (true, "VMware");
                // For Hyper-V, the model must specifically say "Virtual Machine"
                if (manufacturer.Contains("microsoft corporation") && model.Contains("virtual machine"))
                    return (true, "Hyper-V");
                if (manufacturer.Contains("innotek") || manufacturer.Contains("oracle") || model.Contains("virtualbox"))
                    return (true, "VirtualBox");
                if (manufacturer.Contains("xen") || model.Contains("xen") || model.Contains("hvm domu"))
                    return (true, "Xen");
                if (manufacturer.Contains("qemu") || model.Contains("qemu") || model.Contains("bochs") || model.Contains("kvm"))
                    return (true, "QEMU/KVM");
                if (manufacturer.Contains("parallels") || model.Contains("parallels"))
                    return (true, "Parallels");
            }
        }
        catch { }

        // Check BIOS information
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (var obj in searcher.Get())
            {
                var serialNumber = obj["SerialNumber"]?.ToString()?.ToLower() ?? "";
                var version = obj["SMBIOSBIOSVersion"]?.ToString()?.ToLower() ?? "";

                if (serialNumber.Contains("vmware") || version.Contains("vmware"))
                    return (true, "VMware");
                if (serialNumber.Contains("parallels") || version.Contains("parallels"))
                    return (true, "Parallels");
                if (version.Contains("vbox") || version.Contains("virtualbox"))
                    return (true, "VirtualBox");
                if (version.Contains("qemu"))
                    return (true, "QEMU");
            }
        }
        catch { }

        // Check disk drive model - specific VM disk identifiers only
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (var obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString()?.ToLower() ?? "";
                // Only match specific VM disk names, not generic "virtual" word
                if (model.Contains("vmware virtual") || model.Contains("vbox harddisk") || 
                    model.Contains("qemu harddisk") || model.Contains("msft virtual disk"))
                    return (true, "Virtual Disk Detected");
            }
        }
        catch { }

        // Check running processes
        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    var name = proc.ProcessName.ToLower();
                    foreach (var vmProc in VmProcesses)
                    {
                        if (name.Contains(vmProc))
                            return (true, $"VM Process: {vmProc}");
                    }
                }
                catch { }
            }
        }
        catch { }

        // Check registry keys
        foreach (var key in VmRegistryKeys)
        {
            try
            {
                using var regKey = Registry.LocalMachine.OpenSubKey(key);
                if (regKey != null)
                    return (true, "VM Registry Key Found");
            }
            catch { }
        }

        // Check MAC address prefixes (VM-specific vendors only)
        // Note: We check if ALL physical adapters have VM MACs, not just one
        // because Hyper-V hosts can have virtual NICs with 00:15:5D prefix
        try
        {
            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                             ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .ToList();
            
            if (networkInterfaces.Count > 0)
            {
                int vmMacCount = 0;
                foreach (var ni in networkInterfaces)
                {
                    var mac = ni.GetPhysicalAddress().ToString().ToUpper();
                    if (mac.Length >= 6)
                    {
                        var prefix = mac.Substring(0, 6);
                        // VMware: 00:0C:29, 00:50:56, 00:1C:14
                        // VirtualBox: 08:00:27
                        // Parallels: 00:1C:42
                        // QEMU: 52:54:00
                        // Note: Hyper-V (00:15:5D) excluded - exists on host virtual switches
                        if (prefix == "000C29" || prefix == "005056" || prefix == "001C14" ||
                            prefix == "080027" || prefix == "001C42" || prefix == "525400")
                            vmMacCount++;
                    }
                }
                // Only flag as VM if ALL network adapters have VM MACs
                if (vmMacCount > 0 && vmMacCount == networkInterfaces.Count)
                    return (true, "VM MAC Address Detected");
            }
        }
        catch { }

        return (false, null);
    }

    #endregion

    #region Debugger Detection

    public static bool IsDebuggerAttached()
    {
        // Check .NET debugger
        if (Debugger.IsAttached)
            return true;

        // Check native debugger
        if (NativeMethods.IsDebuggerPresent())
            return true;

        // Check remote debugger
        try
        {
            NativeMethods.CheckRemoteDebuggerPresent(NativeMethods.GetCurrentProcess(), out bool isRemoteDebugger);
            if (isRemoteDebugger)
                return true;
        }
        catch { }

        // Check debug port via NtQueryInformationProcess
        try
        {
            var pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();
            uint returnLength;
            var status = NativeMethods.NtQueryInformationProcess(
                NativeMethods.GetCurrentProcess(),
                NativeMethods.ProcessDebugPort,
                ref pbi,
                (uint)Marshal.SizeOf(pbi),
                out returnLength);

            if (status == 0 && pbi.Reserved1 != IntPtr.Zero)
                return true;
        }
        catch { }

        return false;
    }

    #endregion

    #region Blacklisted Process Detection

    // Critical processes that should block exam launch (recording/cheating tools)
    private static readonly string[] CriticalBlacklistedProcesses = {
        // Screen recording/streaming - CRITICAL
        "obs", "obs64", "obs32", "streamlabs", "streamlabsobs",
        "xsplit", "xsplitbroadcaster", "xsplitgamecaster",
        "bandicam", "bdcam", "fraps",
        "action", "mirillis", "camtasia", "snagit",
        "screencastomatic", "flashback", "debut",
        "camstudio", "ezvid", "icecream",
        "loom", "screencast", "sharex", "lightshot",
        "greenshot", "screenpresso",
        "shadowplay",
        "relive", "radeonreplay", "radeonrelive",
        "gamebar", "gamebarftserver",
        "gamebarpresencewriter", "gamedvr",
        
        // Remote access - CRITICAL
        "anydesk", "teamviewer", "ammyy",
        "logmein", "gotomeeting", "webex",
        "screenconnect", "bomgar", "parsec",
        "chromeremotedesktop",
        "vnc", "ultravnc", "tightvnc", "realvnc",
        "radmin", "dameware", "netop",
        "rustdesk", "nomachine",
        
        // Virtual input/automation - CRITICAL
        "autohotkey", "autoit", "keytweak",
        "autokey", "tinytask",
        
        // Cheat/debugging tools - CRITICAL
        "cheatengine", "artmoney", "tsearch",
        "ollydbg", "x64dbg", "x32dbg", "ida", "ida64",
        "ghidra", "immunity", "windbg",
        "processhacker",
        "wireshark", "fiddler", "charles",
        "burp", "mitmproxy", "proxifier"
    };

    // Apps that should be closed but won't block exam if user refuses
    private static readonly string[] SoftBlacklistedProcesses = {
        // Communication apps - can be closed
        "discord", "discordptb", "discordcanary",
        "zoom", "slack", "teams", "msteams", "skype",
        
        // Clipboard managers
        "ditto", "clipboardfusion", "clipx", "clipmate",
        
        // Other distractions
        "spotify", "itunes"
    };

    public static List<string> GetBlacklistedProcesses()
    {
        var found = new List<string>();
        var allBlacklisted = CriticalBlacklistedProcesses.Concat(SoftBlacklistedProcesses).ToArray();
        
        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    var name = proc.ProcessName.ToLower().Replace(" ", "").Replace("-", "").Replace("_", "");
                    foreach (var blacklisted in allBlacklisted)
                    {
                        var checkName = blacklisted.ToLower().Replace(" ", "").Replace("-", "").Replace("_", "");
                        if (name.Contains(checkName) || checkName.Contains(name))
                        {
                            if (!found.Contains(proc.ProcessName))
                                found.Add(proc.ProcessName);
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
        return found;
    }

    public static List<string> GetCriticalBlacklistedProcesses()
    {
        var found = new List<string>();
        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    var name = proc.ProcessName.ToLower().Replace(" ", "").Replace("-", "").Replace("_", "");
                    foreach (var blacklisted in CriticalBlacklistedProcesses)
                    {
                        var checkName = blacklisted.ToLower().Replace(" ", "").Replace("-", "").Replace("_", "");
                        if (name.Contains(checkName) || checkName.Contains(name))
                        {
                            if (!found.Contains(proc.ProcessName))
                                found.Add(proc.ProcessName);
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
        return found;
    }

    /// <summary>
    /// Attempts to kill all blacklisted processes.
    /// Returns a tuple with (killed processes, failed to kill processes).
    /// </summary>
    public static (List<string> Killed, List<string> Failed) KillBlacklistedProcesses()
    {
        var killed = new List<string>();
        var failed = new List<string>();
        var allBlacklisted = CriticalBlacklistedProcesses.Concat(SoftBlacklistedProcesses).ToArray();

        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    var name = proc.ProcessName.ToLower().Replace(" ", "").Replace("-", "").Replace("_", "");
                    foreach (var blacklisted in allBlacklisted)
                    {
                        var checkName = blacklisted.ToLower().Replace(" ", "").Replace("-", "").Replace("_", "");
                        if (name.Contains(checkName) || checkName.Contains(name))
                        {
                            try
                            {
                                proc.Kill();
                                proc.WaitForExit(3000); // Wait up to 3 seconds
                                if (!killed.Contains(proc.ProcessName))
                                    killed.Add(proc.ProcessName);
                            }
                            catch
                            {
                                // Try forceful termination
                                try
                                {
                                    var psi = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "taskkill",
                                        Arguments = $"/F /IM \"{proc.ProcessName}.exe\"",
                                        CreateNoWindow = true,
                                        UseShellExecute = false,
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    };
                                    using var killProc = Process.Start(psi);
                                    killProc?.WaitForExit(3000);
                                    if (!killed.Contains(proc.ProcessName))
                                        killed.Add(proc.ProcessName);
                                }
                                catch
                                {
                                    if (!failed.Contains(proc.ProcessName))
                                        failed.Add(proc.ProcessName);
                                }
                            }
                            break;
                        }
                    }
                }
                catch { }
            }
        }
        catch { }

        return (killed, failed);
    }

    #endregion

    #region Screen Recording Detection

    public static bool IsScreenBeingRecorded()
    {
        // Check for known recording processes
        var blacklisted = GetBlacklistedProcesses();
        if (blacklisted.Any(p => 
            p.ToLower().Contains("obs") ||
            p.ToLower().Contains("bandicam") ||
            p.ToLower().Contains("camtasia") ||
            p.ToLower().Contains("fraps") ||
            p.ToLower().Contains("shadowplay") ||
            p.ToLower().Contains("gamebar") ||
            p.ToLower().Contains("screencast")))
        {
            return true;
        }

        // Check if Windows Game Bar is active
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR");
            if (key != null)
            {
                var appCaptureEnabled = key.GetValue("AppCaptureEnabled");
                if (appCaptureEnabled != null && Convert.ToInt32(appCaptureEnabled) == 1)
                {
                    // Game DVR is enabled, check if it's recording
                    var processes = Process.GetProcessesByName("GameBar");
                    if (processes.Length > 0)
                        return true;
                }
            }
        }
        catch { }

        return false;
    }

    #endregion

    #region Window Integrity Check

    public static bool HasSuspiciousWindows()
    {
        var suspiciousWindows = new List<string>();
        
        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;

            var length = NativeMethods.GetWindowTextLength(hWnd);
            if (length == 0)
                return true;

            var sb = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString().ToLower();

            // Check for suspicious window titles
            string[] suspiciousTerms = {
                "screen record", "screen capture", "game bar",
                "obs studio", "streamlabs", "xsplit",
                "remote desktop", "teamviewer", "anydesk",
                "cheat engine", "memory scanner",
                "packet sniffer", "wireshark"
            };

            foreach (var term in suspiciousTerms)
            {
                if (title.Contains(term))
                {
                    suspiciousWindows.Add(title);
                    break;
                }
            }

            return true;
        }, IntPtr.Zero);

        return suspiciousWindows.Count > 0;
    }

    #endregion

    #region Network Check

    public static bool HasVpnOrProxy()
    {
        try
        {
            // Check for VPN adapters
            var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                if (adapter.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;

                var description = adapter.Description.ToLower();
                var name = adapter.Name.ToLower();

                string[] vpnIndicators = {
                    "vpn", "tap-windows", "tunnelbear", "nordvpn",
                    "expressvpn", "cyberghost", "surfshark", "pia",
                    "windscribe", "protonvpn", "mullvad", "wireguard",
                    "openvpn", "softether", "cisco anyconnect",
                    "pulse secure", "globalprotect", "forticlient"
                };

                foreach (var indicator in vpnIndicators)
                {
                    if (description.Contains(indicator) || name.Contains(indicator))
                        return true;
                }
            }

            // Check proxy settings
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
            if (key != null)
            {
                var proxyEnabled = key.GetValue("ProxyEnable");
                if (proxyEnabled != null && Convert.ToInt32(proxyEnabled) == 1)
                    return true;
            }
        }
        catch { }

        return false;
    }

    #endregion

    #region Comprehensive Security Report

    public class SecurityReport
    {
        public bool IsAdmin { get; set; }
        public int MonitorCount { get; set; }
        public bool IsRemoteSession { get; set; }
        public bool IsVM { get; set; }
        public string? VmType { get; set; }
        public bool IsDebuggerAttached { get; set; }
        public List<string> BlacklistedProcesses { get; set; } = new();
        public bool IsScreenRecording { get; set; }
        public bool HasSuspiciousWindows { get; set; }
        public bool HasVpnOrProxy { get; set; }

        public bool CanLaunchExam =>
            IsAdmin &&
            MonitorCount == 1 &&
            !IsRemoteSession &&
            !IsVM &&
            !IsDebuggerAttached &&
            BlacklistedProcesses.Count == 0 &&
            !IsScreenRecording &&
            !HasSuspiciousWindows;

        public List<string> GetBlockingReasons()
        {
            var reasons = new List<string>();

            if (!IsAdmin)
                reasons.Add("Run the application as Administrator.");
            if (MonitorCount != 1)
                reasons.Add($"Disconnect extra monitors ({MonitorCount} detected, only 1 allowed).");
            if (IsRemoteSession)
                reasons.Add("Remote desktop sessions are not allowed.");
            if (IsVM)
                reasons.Add($"Virtual machines are not allowed ({VmType ?? "Unknown VM"}).");
            if (IsDebuggerAttached)
                reasons.Add("Debugger detected. Close debugging tools.");
            if (BlacklistedProcesses.Count > 0)
                reasons.Add($"Close these applications: {string.Join(", ", BlacklistedProcesses)}");
            if (IsScreenRecording)
                reasons.Add("Screen recording software detected. Close recording applications.");
            if (HasSuspiciousWindows)
                reasons.Add("Suspicious windows detected. Close unnecessary applications.");

            return reasons;
        }
    }

    public static SecurityReport RunFullSecurityCheck()
    {
        var vmCheck = DetectVirtualMachine();

        return new SecurityReport
        {
            IsAdmin = IsRunningAsAdmin(),
            MonitorCount = GetMonitorCount(),
            IsRemoteSession = IsRemoteSession(),
            IsVM = vmCheck.IsVM,
            VmType = vmCheck.VmType,
            IsDebuggerAttached = IsDebuggerAttached(),
            BlacklistedProcesses = GetBlacklistedProcesses(),
            IsScreenRecording = IsScreenBeingRecorded(),
            HasSuspiciousWindows = HasSuspiciousWindows(),
            HasVpnOrProxy = HasVpnOrProxy()
        };
    }

    #endregion
}
