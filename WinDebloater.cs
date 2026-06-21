/*
 * =====================================================================================
 *        Sultan's Ultimate Windows Optimizer - v1.1 Stable Release
 * =====================================================================================
 * 
 * Developer:     0xSultan
 * GitHub:        https://github.com/0xSultan-dev
 * Discord:       5e_d
 * 
 * Description:   A comprehensive, premium Windows optimization tool designed to 
 *                improve system responsiveness, network performance, and reduce 
 *                input/audio latency for power users and gamers.
 * 
 * Disclaimer:    This tool modifies Windows registry keys and services. 
 *                Use with caution. Backups are automatically created before modifications.
 * =====================================================================================
 */

using System;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinDebloatTools
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Administrator Privilege Check
            if (!IsAdministrator())
            {
                MessageBox.Show("Error: You must run this tool as Administrator to configure system settings, services, and memory standby lists.", 
                    "Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RunAsAdmin();
                return;
            }

            // Request 0.5ms Timer Resolution on startup for low latency
            try
            {
                uint currentRes;
                NtSetTimerResolution(5000, true, out currentRes);
            }
            catch {}

            Application.Run(new MainWindow());
        }

        static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RunAsAdmin()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Application.ExecutablePath;
            proc.Verb = "runas";

            try
            {
                Process.Start(proc);
            }
            catch {}
            Environment.Exit(0);
        }

        // Native APIs for RAM standby list flushing and Timer Resolution
        [DllImport("ntdll.dll")]
        public static extern int NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);
    }

    // Registry tweak definition structure
    public struct RegistryTweak
    {
        public string Hive; // HKLM or HKCU
        public string SubKey;
        public string ValueName;
        public object DebloatValue;
        public RegistryValueKind ValueKind;
        public string Description;
        public string Category;

        public RegistryTweak(string hive, string subKey, string valueName, object debloatValue, RegistryValueKind valueKind, string description, string category)
        {
            Hive = hive;
            SubKey = subKey;
            ValueName = valueName;
            DebloatValue = debloatValue;
            ValueKind = valueKind;
            Description = description;
            Category = category;
        }
    }

    public class MainWindow : Form
    {
        // P/Invoke for Standby List Purging privilege
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privilege;
        }

        const uint TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        const uint TOKEN_QUERY = 0x00000008;
        const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        const uint SHERB_NOCONFIRMATION = 0x00000001;
        const uint SHERB_NOPROGRESSUI = 0x00000002;
        const uint SHERB_NOSOUND = 0x00000004;

        // Tweaks List
        static RegistryTweak[] tweaks = new RegistryTweak[]
        {
            // === 1. Privacy & Diagnostics ===
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, "Disable Windows Diagnostic Telemetry", "Privacy & Diagnostics"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, RegistryValueKind.DWord, "Disable Cortana Voice Assistant", "Privacy & Diagnostics"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1, RegistryValueKind.DWord, "Disable AI Windows Recall Data Snapshots", "Privacy & Diagnostics"),
            new RegistryTweak("HKCU", @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1, RegistryValueKind.DWord, "Disable Windows Copilot Assistant", "Privacy & Diagnostics"),

            // === 2. UI & Responsiveness ===
            new RegistryTweak("HKCU", @"Control Panel\Desktop", "MenuShowDelay", "20", RegistryValueKind.String, "Accelerate Menu Show Response Delay", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, RegistryValueKind.DWord, "Disable Transparency Effects (Mica & Acrylic)", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord, "Set Visual Effects to Best Performance", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0, RegistryValueKind.DWord, "Disable Taskbar Icon Animations", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout", 0, RegistryValueKind.DWord, "Disable Hover Snap Assist Layout Popups", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0, RegistryValueKind.DWord, "Disable and Hide Widgets Icon from Taskbar", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "", RegistryValueKind.String, "Restore Classic Windows 10 Right-Click Context Menu", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0, RegistryValueKind.DWord, "Disable Explorer Startup Apps Delay (Speeds up desktop loading)", "UI & Responsiveness"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, RegistryValueKind.DWord, "Launch File Explorer to 'This PC' instead of 'Quick Access' (Loads faster)", "UI & Responsiveness"),

            // === 3. Search Bar Web Results ===
            new RegistryTweak("HKCU", @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, "Disable Bing Web Search Suggestions in Start Menu", "Search Bar Web Results"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1, RegistryValueKind.DWord, "Disable Web Search Globally in Taskbar Search Box", "Search Bar Web Results"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0, RegistryValueKind.DWord, "Prevent Search Indexer from Accessing Web Connections", "Search Bar Web Results"),

            // === 4. CPU & Latency Optimizations ===
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3, RegistryValueKind.DWord, "Disable CPU Speculative Mitigations (Spectre/Meltdown)", "CPU & Latency"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord, "Disable CPU Mitigation Mask (Spectre/Meltdown Mask)", "CPU & Latency"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1, RegistryValueKind.DWord, "Disable Power Throttling to Ensure Full Background CPU Power", "CPU & Latency"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord, "Allocate 100% CPU Priority to Active Applications", "CPU & Latency"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord, "Disable Network Bandwidth Throttling during gameplay", "CPU & Latency"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1, RegistryValueKind.DWord, "Keep Core System Files & Drivers in RAM", "CPU & Latency"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\FTH", "Enabled", 0, RegistryValueKind.DWord, "Disable Windows Fault Tolerant Heap (Improves app launching & CPU overhead)", "CPU & Latency"),

            // === 5. GPU & Scheduling ===
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High", RegistryValueKind.String, "Set Game Thread Scheduling Category to High", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "High", RegistryValueKind.String, "Set Game Disk Input/Output Priority to High", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Background Only", "False", RegistryValueKind.String, "Prioritize Foreground Game Resources over System Services", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 1, RegistryValueKind.DWord, "Set Game Priority Level to 1", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8, RegistryValueKind.DWord, "Increase GPU Scheduling Priority for Gaming to 8 (High)", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Clock Rate", 10, RegistryValueKind.DWord, "Increase Graphics Clock Rate for Stabler Frametimes", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord, "Enable Hardware-Accelerated GPU Scheduling (HAGS)", "GPU & Scheduling"),

            // === 6. Storage & NTFS ===
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 1, RegistryValueKind.DWord, "Disable NTFS Last Access Time Updates to Reduce SSD Writes", "Storage & NTFS"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisable8dot3NameCreation", 1, RegistryValueKind.DWord, "Disable NTFS 8.3 Short Filename Creation to Optimize Disk I/O", "Storage & NTFS"),

            // === 7. Windows Theme & Personalization ===
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0, RegistryValueKind.DWord, "Switch Applications Theme to Windows Dark Mode", "Theme & Personalization"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 0, RegistryValueKind.DWord, "Switch Windows Shell and Taskbar Theme to Dark Mode", "Theme & Personalization"),

            // === 8. DNS Caching ===
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "MaxCacheTtl", 86400, RegistryValueKind.DWord, "Optimize DNS Cache Duration (Cache resolved hosts for 24 hours)", "DNS Caching"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "MaxNegativeCacheTtl", 5, RegistryValueKind.DWord, "Optimize DNS Negative Cache Duration (Cache errors for 5 seconds)", "DNS Caching"),

            // === 9. Xbox Game DVR ===
            new RegistryTweak("HKCU", @"System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord, "Disable Xbox Game DVR Background Recording", "Xbox Game DVR"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, RegistryValueKind.DWord, "Disable Xbox Game DVR Policy", "Xbox Game DVR"),
            new RegistryTweak("HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord, "Disable Xbox Game DVR App Capture", "Xbox Game DVR"),

            // === 10. Processor Scheduling ===
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 26, RegistryValueKind.DWord, "Optimize CPU Scheduling for Foreground Apps & Games", "Processor Scheduling"),

            // === 11. Windows Game Mode ===
            new RegistryTweak("HKCU", @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord, "Enable Windows Auto Game Mode", "Windows Game Mode"),
            new RegistryTweak("HKCU", @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord, "Enable Windows Game Mode Dashboard", "Windows Game Mode"),

            // === 12. Taskbar Icons Cleanup ===
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, RegistryValueKind.DWord, "Hide Search Box from Taskbar", "Taskbar Icons"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0, RegistryValueKind.DWord, "Hide Task View Button from Taskbar", "Taskbar Icons"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0, RegistryValueKind.DWord, "Hide Chat (Meet Now) Icon from Taskbar", "Taskbar Icons"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0, RegistryValueKind.DWord, "Hide Copilot Button from Taskbar", "Taskbar Icons"),

            // === 13. Fullscreen Optimizations ===
            new RegistryTweak("HKCU", @"System\GameConfigStore", "GameDVR_FSEBehavior", 2, RegistryValueKind.DWord, "Disable Fullscreen Optimizations Globally for Stabler FPS", "Fullscreen Optimizations"),
            new RegistryTweak("HKCU", @"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord, "Enforce Game DVR FSE Behavior Mode", "Fullscreen Optimizations"),

            // === 14. Virtualization-Based Security (VBS) ===
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord, "Disable Virtualization-Based Security (VBS)", "Virtualization-Based Security (VBS)"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0, RegistryValueKind.DWord, "Disable Hypervisor-Protected Code Integrity (HVCI)", "Virtualization-Based Security (VBS)"),

            // === 15. Power & System Stability ===
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0, RegistryValueKind.DWord, "Disable Fast Startup to Ensure Clean Driver Loading", "Power & System Stability"),
            new RegistryTweak("HKCU", @"Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String, "Enable Auto-Ending of Hung Tasks on Shutdown/Reboot", "Power & System Stability"),
            new RegistryTweak("HKCU", @"Control Panel\Desktop", "HungAppTimeout", "1000", RegistryValueKind.String, "Reduce Hung Application Timeout to 1 Second", "Power & System Stability"),
            new RegistryTweak("HKCU", @"Control Panel\Desktop", "WaitToKillAppTimeout", "2000", RegistryValueKind.String, "Reduce Wait to Kill App Timeout to 2 Seconds", "Power & System Stability"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "2000", RegistryValueKind.String, "Reduce Wait to Kill Service Timeout to 2 Seconds", "Power & System Stability"),

            // === 16. Advanced Network ===
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 100, RegistryValueKind.DWord, "Disable Delivery Optimization P2P Sharing (Simple HTTP Mode)", "Advanced Network"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", 255, RegistryValueKind.DWord, "Disable IPv6 Stack to Reduce Network Overhead", "Advanced Network"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient", "EnableMulticast", 0, RegistryValueKind.DWord, "Disable LLMNR Multi-cast DNS Resolution", "Advanced Network"),

            // === 17. GPU Latency & Preemption ===
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrDelay", 10, RegistryValueKind.DWord, "Extend GPU TDR Delay to 10 Seconds to Prevent Crash Spikes", "GPU Latency & Preemption"),
            new RegistryTweak("HKLM", @"SOFTWARE\NVIDIA Corporation\Global\OpenGL", "ShaderCacheSize", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord, "Configure Unlimited NVIDIA Shader Cache Size", "GPU Latency & Preemption"),
            new RegistryTweak("HKLM", @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler", "EnablePreemption", 0, RegistryValueKind.DWord, "Configure WDDM Scheduler Preemption Level", "GPU Latency & Preemption"),

            // === 18. Automatic Maintenance ===
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", 1, RegistryValueKind.DWord, "Disable Windows Automatic Maintenance Background Activity", "Automatic Maintenance"),

            // === 19. Defender Customizations ===
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "SubmitSamplesConsent", 2, RegistryValueKind.DWord, "Disable Windows Defender Sample Submission", "Defender Customizations"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "SpynetReporting", 0, RegistryValueKind.DWord, "Disable Windows Defender Cloud Protection Reporting", "Defender Customizations"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0, RegistryValueKind.DWord, "Disable Windows SmartScreen Globally", "Defender Customizations"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0, RegistryValueKind.DWord, "Disable Web Content Evaluation in UWP AppHost", "Defender Customizations"),

            // === 20. UI/Shell Background reduction ===
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1, RegistryValueKind.DWord, "Disable UWP Background Apps Running in Background", "UI/Shell & Backgrounds"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppDiagnosticStatus", 2, RegistryValueKind.DWord, "Disable Background Diagnostics for UWP apps", "UI/Shell & Backgrounds"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0, RegistryValueKind.DWord, "Disable Windows Soft Landing Tips", "UI/Shell & Backgrounds"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord, "Disable Start Menu Suggestions", "UI/Shell & Backgrounds"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord, "Disable Lock Screen Suggestions", "UI/Shell & Backgrounds"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0, RegistryValueKind.DWord, "Disable Search Box Suggestions", "UI/Shell & Backgrounds"),
            new RegistryTweak("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", 1, RegistryValueKind.DWord, "Disable Lock Screen to Direct Boot to Login", "UI/Shell & Backgrounds"),
            new RegistryTweak("HKCU", @"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy", "StoragePolicy", 0, RegistryValueKind.DWord, "Disable Automatic Windows Storage Sense File Deletions", "UI/Shell & Backgrounds"),

            // === 21. Advanced Network Interfaces (SPECIAL) ===
            new RegistryTweak("SPECIAL", "NetworkAdapter", "*LsoV2IPv4", 1, RegistryValueKind.DWord, "Disable Large Send Offload (LSO) on network adapters", "Advanced Network Interfaces"),
            new RegistryTweak("SPECIAL", "NetworkAdapter", "ChecksumOffload", 1, RegistryValueKind.DWord, "Disable Checksum Offload on network adapters", "Advanced Network Interfaces"),
            new RegistryTweak("SPECIAL", "NetworkAdapter", "*FlowControl", 1, RegistryValueKind.DWord, "Disable Flow Control on network adapters", "Advanced Network Interfaces"),
            new RegistryTweak("SPECIAL", "NetworkAdapter", "*NumRssQueues", 2, RegistryValueKind.String, "Optimize RSS Queues (*NumRssQueues = 2) on network adapters", "Advanced Network Interfaces"),
            new RegistryTweak("SPECIAL", "DNS", "CloudflareDNS", 1, RegistryValueKind.DWord, "Configure Cloudflare DNS (1.1.1.1) on network adapters", "Advanced Network Interfaces"),
            new RegistryTweak("SPECIAL", "NetBIOS", "DisableNetBIOS", 1, RegistryValueKind.DWord, "Disable NetBIOS over TCP/IP", "Advanced Network Interfaces"),
            new RegistryTweak("SPECIAL", "TcpGlobal", "OptimizeTcpGlobal", 1, RegistryValueKind.DWord, "Optimize TCP Global Settings (RSS, Autotuning disabled)", "Advanced Network Interfaces"),

            // === 22. System Services (SPECIAL) ===
            new RegistryTweak("SPECIAL", "Service", "DiagTrack", 1, RegistryValueKind.DWord, "Disable Telemetry (DiagTrack) Service", "System Services"),
            new RegistryTweak("SPECIAL", "Service", "SysMain", 1, RegistryValueKind.DWord, "Disable SysMain (Superfetch) Service", "System Services"),
            new RegistryTweak("SPECIAL", "Service", "WSearch", 1, RegistryValueKind.DWord, "Disable Windows Search Indexing Service", "System Services"),
            new RegistryTweak("SPECIAL", "Service", "WerSvc", 1, RegistryValueKind.DWord, "Disable Windows Error Reporting Service", "System Services"),
            new RegistryTweak("SPECIAL", "Service", "Spooler", 1, RegistryValueKind.DWord, "Disable Print Spooler Service", "System Services"),
            new RegistryTweak("SPECIAL", "Service", "DoSvc", 1, RegistryValueKind.DWord, "Disable Delivery Optimization P2P Service", "System Services"),

            // === 23. CPU & Power Management (SPECIAL) ===
            new RegistryTweak("SPECIAL", "PowerScheme", "UltimatePerformance", 1, RegistryValueKind.DWord, "Enable Ultimate Performance Power Scheme", "CPU & Power Management"),
            new RegistryTweak("SPECIAL", "PowerSetting", "CPMINCORES", 1, RegistryValueKind.DWord, "Disable CPU Core Parking (Unpark Cores)", "CPU & Power Management"),
            new RegistryTweak("SPECIAL", "PowerSetting", "USBSUSPEND", 1, RegistryValueKind.DWord, "Disable USB Selective Suspend (Reduces latency)", "CPU & Power Management"),
            new RegistryTweak("SPECIAL", "PowerSetting", "PCIEASPM", 1, RegistryValueKind.DWord, "Disable PCI Express Link State Management (ASPM)", "CPU & Power Management"),
            new RegistryTweak("SPECIAL", "PowerSetting", "CSTATES", 1, RegistryValueKind.DWord, "Disable CPU Idle States (C-States) to prevent stutters", "CPU & Power Management"),
            new RegistryTweak("SPECIAL", "LatencyBCDTweak", "HPET_Disable", 1, RegistryValueKind.DWord, "Disable High Precision Event Timer (HPET) & Dynamic Tick", "CPU & Power Management"),
            new RegistryTweak("SPECIAL", "MemoryCompression", "DisableMemComp", 1, RegistryValueKind.DWord, "Disable Memory Compression to reduce CPU overhead", "CPU & Power Management"),

            // === 24. Storage & OS Components (SPECIAL) ===
            new RegistryTweak("SPECIAL", "FixedPagefile", "PagefileConfig", 1, RegistryValueKind.DWord, "Configure Fixed Pagefile Size (Optimizes RAM paging)", "Storage & OS Components"),
            new RegistryTweak("SPECIAL", "OptionalFeatures", "DisableFeatures", 1, RegistryValueKind.DWord, "Disable Optional Features (Hyper-V, SMB1, IE, XPS)", "Storage & OS Components"),
            new RegistryTweak("SPECIAL", "DefenderExclusions", "GameExclusions", 1, RegistryValueKind.DWord, "Add Defender Game Directory Exclusions (Steam/Epic)", "Storage & OS Components"),

            // === 25. Multimedia Class Scheduler (MMCSS) Audio ===
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Scheduling Category", "High", RegistryValueKind.String, "Set Audio Thread Scheduling Category to High", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "SFIO Priority", "High", RegistryValueKind.String, "Set Audio Disk Input/Output Priority to High", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Priority", 6, RegistryValueKind.DWord, "Set Audio Thread Priority Level to 6", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Clock Rate", 10, RegistryValueKind.DWord, "Increase Audio Clock Rate for stable sound latency", "GPU & Scheduling"),
            new RegistryTweak("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio", "Background Only", "False", RegistryValueKind.String, "Prioritize Foreground Audio Resources over System Services", "GPU & Scheduling"),

            // === 26. AMD/Intel Shader Cache (SPECIAL) ===
            new RegistryTweak("SPECIAL", "ShaderCache", "AmdIntelShaderCache", 1, RegistryValueKind.DWord, "Configure Always-On AMD & Intel Shader Cache Size", "GPU Latency & Preemption"),

            // === 27. GPU MSI Mode & Interrupt Priority (SPECIAL) ===
            new RegistryTweak("SPECIAL", "GpuMsi", "EnableMsiMode", 1, RegistryValueKind.DWord, "Enable GPU MSI Mode & High Interrupt Priority (lower DPC latency)", "GPU Latency & Preemption")
        };

        static string[] telemetryTasks = new string[]
        {
            @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
            @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp",
            @"\Microsoft\Windows\Application Experience\StartupAppTask",
            @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
            @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
            @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
            @"\Microsoft\Windows\Windows Error Reporting\QueueReporting",
            @"\Microsoft\Windows\Windows Defender\Windows Defender Scheduled Scan"
        };

        static string[] optionalFeatures = new string[] {
            "Microsoft-Hyper-V-All",
            "SMB1Protocol",
            "Internet-Explorer-Optional-amd64",
            "Printing-XPSServices-Features"
        };

        // === Shared constants (single source of truth to prevent backup/apply/restore drift) ===
        const string UltimatePerfGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
        const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";

        // Services toggled by the optimizer; backup and apply MUST use the same list to stay reversible.
        static readonly string[] managedServices = new string[] {
            "DiagTrack", "dmwappushservice", "SysMain", "TrkWks", "WSearch",
            "WerSvc", "Spooler", "DoSvc", "RemoteRegistry"
        };

        // Network-adapter offload values toggled by the optimizer (RSS queues handled separately).
        static readonly string[] adapterOffloadValues = new string[] {
            "*InterruptModeration", "*EEELinkAdvertisement", "*GreenEthernet",
            "*LsoV2IPv4", "*LsoV2IPv6",
            "*IPChecksumOffloadIPv4", "*TCPChecksumOffloadIPv4", "*TCPChecksumOffloadIPv6",
            "*UDPChecksumOffloadIPv4", "*UDPChecksumOffloadIPv6",
            "*FlowControl"
        };

        // Cleaning statistics counters
        static long totalBytesCleaned = 0;
        static int filesDeletedCount = 0;
        static int foldersDeletedCount = 0;
        static int filesSkippedCount = 0;

        // UI Controls
        Panel pnlSidebar;
        Panel pnlContent;
        Label lblTitle;
        Label lblVersion;

        // Tab Panels
        Panel pnlTweaks;
        Panel pnlCleaner;
        Panel pnlStartup;
        Panel pnlGameProfiles;
        Panel pnlBackupRestore;
        Panel pnlHealth;

        // Sidebar Buttons
        Button btnTabTweaks;
        Button btnTabCleaner;
        Button btnTabStartup;
        Button btnTabGameProfiles;
        Button btnTabBackupRestore;
        Button btnTabHealth;

        // Tweaks Controls
        Panel pnlTweaksScroll;
        List<CheckBox> chkTweaks = new List<CheckBox>();
        ProgressBar prgTweaks;
        Label lblTweaksStatus;

        // System Tray Control
        NotifyIcon sysTrayIcon;
        ContextMenu trayMenu;
        Icon extractedTrayIcon;
        bool reallyClose = false;

        // Cleaner Controls
        CheckBox chkCleanUserTemp;
        CheckBox chkCleanSysTemp;
        CheckBox chkCleanPrefetch;
        CheckBox chkCleanUpdateCache;
        CheckBox chkCleanRecycleBin;
        CheckBox chkCleanRamStandby;
        CheckBox chkCleanDriveTrim;
        RichTextBox rchCleanerLog;

        // Startup Controls
        ListView lstStartup;
        Button btnToggleStartup;

        // Game Profile Controls
        TextBox txtGameExe;
        ComboBox cmbGameCpuPriority;
        ComboBox cmbGameIoPriority;
        CheckBox chkExcludeCore0;
        ListView lstGameProfiles;

        // Backup Controls
        ListView lstBackups;

        // System Health Controls
        RichTextBox rchHealthLog;
        CheckBox chkRepairDism;
        CheckBox chkRepairSfc;
        CheckBox chkRepairChkdsk;

        public MainWindow()
        {
            // Set Form Properties
            this.Text = "Sultan's Ultimate Windows Optimizer - v1.1";
            this.Size = new Size(950, 680);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            // Initialize Main Panels
            InitializeLayout();

            // Load Content Panels
            CreateTweaksPanel();
            CreateCleanerPanel();
            CreateStartupPanel();
            CreateGameProfilesPanel();
            CreateBackupRestorePanel();
            CreateHealthPanel();

            // Default Tab: Tweaks
            SwitchTab(pnlTweaks, btnTabTweaks);

            // Initialize System Tray Icon
            sysTrayIcon = new NotifyIcon();
            try
            {
                extractedTrayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                sysTrayIcon.Icon = extractedTrayIcon;
            }
            catch
            {
                sysTrayIcon.Icon = SystemIcons.Application;
            }
            sysTrayIcon.Text = "Ultimate Windows 11 Optimizer";
            sysTrayIcon.Visible = false;
            sysTrayIcon.DoubleClick += (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                sysTrayIcon.Visible = false;
            };

            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Restore Optimizer", (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                sysTrayIcon.Visible = false;
            });
            trayMenu.MenuItems.Add("Exit Completely", (s, e) => {
                reallyClose = true;
                Application.Exit();
            });
            sysTrayIcon.ContextMenu = trayMenu;

            this.FormClosing += MainWindow_FormClosing;
        }

        private void InitializeLayout()
        {
            // Sidebar
            pnlSidebar = new Panel();
            pnlSidebar.Size = new Size(200, 650);
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.BackColor = Color.FromArgb(17, 17, 17);
            this.Controls.Add(pnlSidebar);

            // Title
            lblTitle = new Label();
            lblTitle.Text = "Sultan Optimizer";
            lblTitle.Location = new Point(12, 20);
            lblTitle.Size = new Size(180, 25);
            lblTitle.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 215, 215);
            pnlSidebar.Controls.Add(lblTitle);

            // Version
            lblVersion = new Label();
            lblVersion.Text = "v1.1 by 0xSultan";
            lblVersion.Location = new Point(12, 45);
            lblVersion.Size = new Size(180, 18);
            lblVersion.Font = new Font("Segoe UI", 8f, FontStyle.Italic);
            lblVersion.ForeColor = Color.Gray;
            pnlSidebar.Controls.Add(lblVersion);

            // Sidebar navigation buttons
            btnTabTweaks = CreateSidebarButton("Tweaks", 80);
            btnTabTweaks.Click += (s, e) => SwitchTab(pnlTweaks, btnTabTweaks);

            btnTabCleaner = CreateSidebarButton("Deep Cleaner", 130);
            btnTabCleaner.Click += (s, e) => SwitchTab(pnlCleaner, btnTabCleaner);

            btnTabStartup = CreateSidebarButton("Startup Manager", 180);
            btnTabStartup.Click += (s, e) => {
                SwitchTab(pnlStartup, btnTabStartup);
                RefreshStartupList();
            };

            btnTabGameProfiles = CreateSidebarButton("Game Profiles", 230);
            btnTabGameProfiles.Click += (s, e) => {
                SwitchTab(pnlGameProfiles, btnTabGameProfiles);
                RefreshGameProfilesList();
            };

            btnTabBackupRestore = CreateSidebarButton("Backup & Restore", 280);
            btnTabBackupRestore.Click += (s, e) => {
                SwitchTab(pnlBackupRestore, btnTabBackupRestore);
                RefreshBackupsList();
            };

            btnTabHealth = CreateSidebarButton("System Health", 330);
            btnTabHealth.Click += (s, e) => SwitchTab(pnlHealth, btnTabHealth);

            // Developer Credits Link / Info in Sidebar
            Label lblCredits = new Label();
            lblCredits.Text = "Developer: 0xSultan\nDiscord: 5e_d\nGitHub: github.com/0xSultan-dev";
            lblCredits.Location = new Point(12, 580);
            lblCredits.Size = new Size(180, 55);
            lblCredits.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblCredits.ForeColor = Color.Gray;
            pnlSidebar.Controls.Add(lblCredits);

            // Main Content Area
            pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = Color.FromArgb(30, 30, 30);
            this.Controls.Add(pnlContent);
            pnlContent.BringToFront(); // Fix overlapping layout issue
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!reallyClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                sysTrayIcon.Visible = true;
                sysTrayIcon.ShowBalloonTip(3000, "Sultan's Optimizer Running in Background", 
                    "The 0.5ms Latency Timer Resolution remains active in the system tray.", ToolTipIcon.Info);
            }
            else
            {
                sysTrayIcon.Dispose();
                if (trayMenu != null) trayMenu.Dispose();
                if (extractedTrayIcon != null) extractedTrayIcon.Dispose();
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetControlsEnabled), enabled);
                return;
            }
            btnTabTweaks.Enabled = enabled;
            btnTabCleaner.Enabled = enabled;
            btnTabStartup.Enabled = enabled;
            btnTabGameProfiles.Enabled = enabled;
            btnTabBackupRestore.Enabled = enabled;
            btnTabHealth.Enabled = enabled;

            DisableButtonsInControl(pnlContent, enabled);
        }

        private void DisableButtonsInControl(Control parent, bool enabled)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button) c.Enabled = enabled;
                if (c.HasChildren) DisableButtonsInControl(c, enabled);
            }
        }

        private Button CreateSidebarButton(string text, int y)
        {
            Button btn = new Button();
            btn.Text = "  " + text;
            btn.Location = new Point(0, y);
            btn.Size = new Size(200, 45);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            btn.BackColor = Color.FromArgb(17, 17, 17);
            btn.ForeColor = Color.White;
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => { if (btn.BackColor != Color.FromArgb(40, 40, 40)) btn.BackColor = Color.FromArgb(28, 28, 28); };
            btn.MouseLeave += (s, e) => { if (btn.BackColor != Color.FromArgb(40, 40, 40)) btn.BackColor = Color.FromArgb(17, 17, 17); };
            pnlSidebar.Controls.Add(btn);
            return btn;
        }

        private void SwitchTab(Panel activePanel, Button activeBtn)
        {
            // Hide all panels
            pnlTweaks.Visible = false;
            pnlCleaner.Visible = false;
            pnlStartup.Visible = false;
            pnlGameProfiles.Visible = false;
            pnlBackupRestore.Visible = false;
            pnlHealth.Visible = false;

            // Show active panel
            activePanel.Visible = true;

            // Reset sidebar buttons backgrounds
            btnTabTweaks.BackColor = Color.FromArgb(17, 17, 17);
            btnTabCleaner.BackColor = Color.FromArgb(17, 17, 17);
            btnTabStartup.BackColor = Color.FromArgb(17, 17, 17);
            btnTabGameProfiles.BackColor = Color.FromArgb(17, 17, 17);
            btnTabBackupRestore.BackColor = Color.FromArgb(17, 17, 17);
            btnTabHealth.BackColor = Color.FromArgb(17, 17, 17);

            // Active button style
            activeBtn.BackColor = Color.FromArgb(40, 40, 40);
        }

        private Button CreateFlatButton(string text, int x, int y, int width, int height, Color backColor, Color foreColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(width, height);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(Math.Min(backColor.R + 25, 255), Math.Min(backColor.G + 25, 255), Math.Min(backColor.B + 25, 255));
            btn.MouseLeave += (s, e) => btn.BackColor = backColor;
            return btn;
        }

        // === 1. Tweaks Panel ===
        private void CreateTweaksPanel()
        {
            pnlTweaks = new Panel();
            pnlTweaks.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pnlTweaks);

            // Title
            Label lblHeader = new Label();
            lblHeader.Text = "Ultimate Performance & Latency Tweaks";
            lblHeader.Location = new Point(20, 20);
            lblHeader.Size = new Size(360, 25);
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(0, 215, 215);
            pnlTweaks.Controls.Add(lblHeader);

            // Quick-select presets
            Button btnPresetThermal = CreateFlatButton("Thermal-Safe Preset", 20, 50, 200, 30, Color.FromArgb(0, 150, 100), Color.White);
            btnPresetThermal.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnPresetThermal.Click += (s, e) => SetTweakPreset("thermal");
            pnlTweaks.Controls.Add(btnPresetThermal);

            Button btnPresetAll = CreateFlatButton("Select All", 230, 50, 110, 30, Color.FromArgb(50, 50, 50), Color.White);
            btnPresetAll.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnPresetAll.Click += (s, e) => SetTweakPreset("all");
            pnlTweaks.Controls.Add(btnPresetAll);

            Button btnPresetNone = CreateFlatButton("Clear All", 350, 50, 100, 30, Color.FromArgb(50, 50, 50), Color.White);
            btnPresetNone.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnPresetNone.Click += (s, e) => SetTweakPreset("none");
            pnlTweaks.Controls.Add(btnPresetNone);

            Label lblPresetHint = new Label();
            lblPresetHint.Text = "Thermal-Safe keeps the performance tweaks but leaves CPU idle/clock power-saving on (runs cooler, less throttling).";
            lblPresetHint.Location = new Point(462, 52);
            lblPresetHint.Size = new Size(248, 34);
            lblPresetHint.Font = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            lblPresetHint.ForeColor = Color.Gray;
            pnlTweaks.Controls.Add(lblPresetHint);

            // Scrollable Panel for Checkboxes
            pnlTweaksScroll = new Panel();
            pnlTweaksScroll.Location = new Point(20, 88);
            pnlTweaksScroll.Size = new Size(690, 452);
            pnlTweaksScroll.AutoScroll = true;
            pnlTweaksScroll.BackColor = Color.FromArgb(25, 25, 25);
            pnlTweaks.Controls.Add(pnlTweaksScroll);

            // Populate Tweaks grouped by categories
            Dictionary<string, List<RegistryTweak>> categories = new Dictionary<string, List<RegistryTweak>>();
            foreach (var tweak in tweaks)
            {
                if (!categories.ContainsKey(tweak.Category))
                {
                    categories[tweak.Category] = new List<RegistryTweak>();
                }
                categories[tweak.Category].Add(tweak);
            }

            int y = 10;
            foreach (var category in categories)
            {
                // Category header label
                Label lblCat = new Label();
                lblCat.Text = category.Key;
                lblCat.Location = new Point(15, y);
                lblCat.Size = new Size(500, 20);
                lblCat.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblCat.ForeColor = Color.FromArgb(0, 190, 190);
                pnlTweaksScroll.Controls.Add(lblCat);
                y += 25;

                // Add checkboxes under category
                foreach (var tweak in category.Value)
                {
                    CheckBox chk = new CheckBox();
                    chk.Text = tweak.Description;
                    chk.Location = new Point(30, y);
                    chk.Size = new Size(620, 22);
                    chk.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
                    chk.Checked = true; // Enabled by default
                    chk.Tag = tweak;
                    pnlTweaksScroll.Controls.Add(chk);
                    chkTweaks.Add(chk);
                    y += 25;
                }
                y += 15; // Gap between categories
            }

            // Bottom Buttons Panel
            Button btnApply = CreateFlatButton("Apply Selected Tweaks", 20, 560, 200, 45, Color.FromArgb(0, 120, 215), Color.White);
            btnApply.Click += (s, e) => ApplySelectedTweaks();
            pnlTweaks.Controls.Add(btnApply);

            Button btnRestore = CreateFlatButton("Restore Default Settings", 240, 560, 200, 45, Color.FromArgb(50, 50, 50), Color.White);
            btnRestore.Click += (s, e) => {
                SwitchTab(pnlBackupRestore, btnTabBackupRestore);
                RefreshBackupsList();
            };
            pnlTweaks.Controls.Add(btnRestore);

            // Progress Bar
            prgTweaks = new ProgressBar();
            prgTweaks.Location = new Point(460, 560);
            prgTweaks.Size = new Size(250, 20);
            prgTweaks.Visible = false;
            pnlTweaks.Controls.Add(prgTweaks);

            // Status Label
            lblTweaksStatus = new Label();
            lblTweaksStatus.Location = new Point(460, 585);
            lblTweaksStatus.Size = new Size(250, 20);
            lblTweaksStatus.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            lblTweaksStatus.ForeColor = Color.Gray;
            lblTweaksStatus.Text = "";
            lblTweaksStatus.Visible = false;
            pnlTweaks.Controls.Add(lblTweaksStatus);
        }

        // Quick-select presets for the tweak checkboxes.
        private void SetTweakPreset(string mode)
        {
            // ValueNames of the power tweaks that raise heat: they stop the CPU from idling /
            // down-clocking, which on a thermally limited rig causes throttling and stutter.
            string[] heatInducing = new string[] {
                "UltimatePerformance", "CPMINCORES", "USBSUSPEND", "PCIEASPM", "CSTATES", "PowerThrottlingOff"
            };
            foreach (var chk in chkTweaks)
            {
                RegistryTweak t = (RegistryTweak)chk.Tag;
                if (mode == "all") chk.Checked = true;
                else if (mode == "none") chk.Checked = false;
                else if (mode == "thermal") chk.Checked = (Array.IndexOf(heatInducing, t.ValueName) < 0);
            }
        }

        private void ApplySelectedTweaks()
        {
            DialogResult dialog = MessageBox.Show("Are you sure you want to apply all selected tweaks? An automatic backup will be created for safety.", 
                "Apply Tweaks Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog != DialogResult.Yes) return;

            SetControlsEnabled(false);

            System.Threading.Tasks.Task.Factory.StartNew(() => {
                // Auto-create backup
                this.Invoke(new MethodInvoker(() => {
                    prgTweaks.Minimum = 0;
                    prgTweaks.Value = 0;
                    prgTweaks.Visible = true;
                    lblTweaksStatus.Text = "Creating automatic system backup...";
                    lblTweaksStatus.Visible = true;
                }));
                
                // SAFETY GATE: never modify the system unless a verified backup exists first.
                // This protects the tool's 100% reversibility guarantee.
                bool backupOk = CreateBackupSilently();
                if (!backupOk)
                {
                    this.Invoke(new MethodInvoker(() => {
                        prgTweaks.Visible = false;
                        lblTweaksStatus.Visible = false;
                        SetControlsEnabled(true);
                        MessageBox.Show(
                            "A safety backup could not be created, so NO tweaks were applied.\n\n" +
                            "This protects the tool's 100% reversibility guarantee. Please make sure the app is running " +
                            "as Administrator and that its folder is writable, then try again.",
                            "Aborted: Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }

                int successCount = 0;
                int failCount = 0;
                List<RegistryTweak> selectedTweaks = new List<RegistryTweak>();
                this.Invoke(new MethodInvoker(() => {
                    foreach (var chk in chkTweaks)
                    {
                        if (chk.Checked) selectedTweaks.Add((RegistryTweak)chk.Tag);
                    }
                    prgTweaks.Maximum = selectedTweaks.Count;
                }));

                for (int i = 0; i < selectedTweaks.Count; i++)
                {
                    var tweak = selectedTweaks[i];
                    int currentIndex = i;
                    this.Invoke(new MethodInvoker(() => {
                        prgTweaks.Value = currentIndex;
                        lblTweaksStatus.Text = string.Format("Applying: {0}...", tweak.Description);
                    }));

                    try
                    {
                        if (tweak.Hive == "SPECIAL")
                        {
                            ApplySpecialTweak(tweak.SubKey, tweak.ValueName, true);
                        }
                        else
                        {
                            SetRegistryValue(tweak.Hive, tweak.SubKey, tweak.ValueName, tweak.DebloatValue, tweak.ValueKind);
                        }
                        successCount++;
                    }
                    catch { failCount++; }
                }

                this.Invoke(new MethodInvoker(() => {
                    prgTweaks.Value = prgTweaks.Maximum;
                    lblTweaksStatus.Text = "Restarting Explorer shell...";
                }));

                RestartExplorer();

                this.Invoke(new MethodInvoker(() => {
                    prgTweaks.Visible = false;
                    lblTweaksStatus.Visible = false;
                    SetControlsEnabled(true);
                    string applyMsg = string.Format("Successfully applied {0} optimizations to the system! A restart is highly recommended.", successCount);
                    if (failCount > 0) applyMsg += string.Format("\n\nNote: {0} tweak(s) could not be applied and were skipped.", failCount);
                    MessageBox.Show(applyMsg, "Tweaks Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            });
        }

        // === 2. Cleaner Panel ===
        private void CreateCleanerPanel()
        {
            pnlCleaner = new Panel();
            pnlCleaner.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pnlCleaner);

            // Title
            Label lblHeader = new Label();
            lblHeader.Text = "Deep Temporary Files Cleaner & Memory Purifier";
            lblHeader.Location = new Point(20, 20);
            lblHeader.Size = new Size(400, 25);
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(0, 215, 215);
            pnlCleaner.Controls.Add(lblHeader);

            // Cleaner Options Panel
            Panel pnlOptions = new Panel();
            pnlOptions.Location = new Point(20, 60);
            pnlOptions.Size = new Size(250, 480);
            pnlOptions.BackColor = Color.FromArgb(25, 25, 25);
            pnlCleaner.Controls.Add(pnlOptions);

            Label lblOptsHeader = new Label();
            lblOptsHeader.Text = "Cleanup Modules";
            lblOptsHeader.Location = new Point(15, 15);
            lblOptsHeader.Size = new Size(220, 20);
            lblOptsHeader.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblOptsHeader.ForeColor = Color.FromArgb(0, 190, 190);
            pnlOptions.Controls.Add(lblOptsHeader);

            chkCleanUserTemp = CreateCleanupCheckbox("Clean User Temp Directory", 50, pnlOptions);
            chkCleanSysTemp = CreateCleanupCheckbox("Clean System Temp Directory", 80, pnlOptions);
            chkCleanPrefetch = CreateCleanupCheckbox("Clean Windows Prefetch Logs", 110, pnlOptions);
            chkCleanUpdateCache = CreateCleanupCheckbox("Clean Windows Update Cache", 140, pnlOptions);
            chkCleanRecycleBin = CreateCleanupCheckbox("Empty Recycle Bin", 170, pnlOptions);
            chkCleanRamStandby = CreateCleanupCheckbox("Purge RAM Standby List", 200, pnlOptions);
            chkCleanDriveTrim = CreateCleanupCheckbox("Run Drive TRIM & Defrag", 230, pnlOptions);

            // Log Textbox
            rchCleanerLog = new RichTextBox();
            rchCleanerLog.Location = new Point(290, 60);
            rchCleanerLog.Size = new Size(420, 480);
            rchCleanerLog.BackColor = Color.FromArgb(17, 17, 17);
            rchCleanerLog.ForeColor = Color.LightGreen;
            rchCleanerLog.Font = new Font("Consolas", 9f, FontStyle.Regular);
            rchCleanerLog.ReadOnly = true;
            pnlCleaner.Controls.Add(rchCleanerLog);

            // Buttons
            Button btnClean = CreateFlatButton("Run System Cleanup", 20, 560, 250, 45, Color.FromArgb(0, 120, 215), Color.White);
            btnClean.Click += (s, e) => RunDeepCleanerUI();
            pnlCleaner.Controls.Add(btnClean);
        }

        private CheckBox CreateCleanupCheckbox(string text, int y, Panel parent)
        {
            CheckBox chk = new CheckBox();
            chk.Text = text;
            chk.Location = new Point(15, y);
            chk.Size = new Size(220, 20);
            chk.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            chk.Checked = true;
            parent.Controls.Add(chk);
            return chk;
        }

        private void RunDeepCleanerUI()
        {
            rchCleanerLog.Clear();
            totalBytesCleaned = 0;
            filesDeletedCount = 0;
            foldersDeletedCount = 0;
            filesSkippedCount = 0;

            AppendLog("Starting deep system cleanup...\n");
            AppendLog("======================================\n");

            SetControlsEnabled(false);

            bool cleanUserTemp = chkCleanUserTemp.Checked;
            bool cleanSysTemp = chkCleanSysTemp.Checked;
            bool cleanPrefetch = chkCleanPrefetch.Checked;
            bool cleanUpdateCache = chkCleanUpdateCache.Checked;
            bool cleanRecycleBin = chkCleanRecycleBin.Checked;
            bool cleanRamStandby = chkCleanRamStandby.Checked;
            bool cleanDriveTrim = chkCleanDriveTrim.Checked;

            System.Threading.Tasks.Task.Factory.StartNew(() => {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clean_backups", "clean_backup_" + timestamp);
                try { Directory.CreateDirectory(backupDir); } catch {}

                if (cleanUserTemp)
                {
                    string userTemp = Path.GetTempPath();
                    AppendLog(" -> Cleaning User Temp Folder...\n");
                    CleanDirectoryWithBackup(userTemp, backupDir);
                }
                if (cleanSysTemp)
                {
                    string sysTemp = @"C:\Windows\Temp";
                    AppendLog(" -> Cleaning System Temp Folder...\n");
                    CleanDirectoryWithBackup(sysTemp, backupDir);
                }
                if (cleanPrefetch)
                {
                    string prefetch = @"C:\Windows\Prefetch";
                    AppendLog(" -> Cleaning Prefetch Directory...\n");
                    CleanDirectoryWithBackup(prefetch, backupDir);
                }
                if (cleanUpdateCache)
                {
                    AppendLog(" -> Cleaning Windows Update Cache...\n");
                    CleanSoftwareDistribution();
                }
                if (cleanRamStandby)
                {
                    AppendLog(" -> Flushing System Memory Cache & Standby Lists...\n");
                    PurgeStandbyList();
                }
                if (cleanRecycleBin)
                {
                    AppendLog(" -> Emptying Recycle Bin...\n");
                    EmptyRecycleBin();
                }
                if (cleanDriveTrim)
                {
                    AppendLog(" -> Running Storage Drive Optimizations (TRIM)...\n");
                    OptimizeDrives();
                }

                // Final check of files backup folder
                bool hasBackup = true;
                try
                {
                    if (Directory.Exists(backupDir) && Directory.GetFileSystemEntries(backupDir).Length == 0)
                    {
                        Directory.Delete(backupDir, true);
                        hasBackup = false;
                    }
                }
                catch { hasBackup = false; }

                double megabytesCleaned = (double)totalBytesCleaned / (1024 * 1024);

                this.Invoke(new MethodInvoker(() => {
                    rchCleanerLog.AppendText("\n======================================\n");
                    rchCleanerLog.AppendText("Cleanup Completed Successfully!\n");
                    rchCleanerLog.AppendText(string.Format(" - Total Disk Space Recovered: {0:F2} MB\n", megabytesCleaned));
                    rchCleanerLog.AppendText(string.Format(" - Files Deleted: {0}\n", filesDeletedCount));
                    rchCleanerLog.AppendText(string.Format(" - Folders Deleted: {0}\n", foldersDeletedCount));
                    rchCleanerLog.AppendText(string.Format(" - Files Locked (Skipped): {0}\n", filesSkippedCount));
                    if (hasBackup)
                    {
                        rchCleanerLog.AppendText(string.Format(" - Backup of deleted files saved at: \n   {0}\n", backupDir));
                    }
                    SetControlsEnabled(true);
                }));
            });
        }

        private void AppendLog(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendLog), text);
            }
            else
            {
                rchCleanerLog.AppendText(text);
                rchCleanerLog.SelectionStart = rchCleanerLog.Text.Length;
                rchCleanerLog.ScrollToCaret();
            }
        }

        // === 3. Startup Manager Panel ===
        private void CreateStartupPanel()
        {
            pnlStartup = new Panel();
            pnlStartup.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pnlStartup);

            // Title
            Label lblHeader = new Label();
            lblHeader.Text = "Windows Startup Programs Manager";
            lblHeader.Location = new Point(20, 20);
            lblHeader.Size = new Size(400, 25);
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(0, 215, 215);
            pnlStartup.Controls.Add(lblHeader);

            // Startup List View
            lstStartup = new ListView();
            lstStartup.Location = new Point(20, 60);
            lstStartup.Size = new Size(690, 480);
            lstStartup.View = View.Details;
            lstStartup.FullRowSelect = true;
            lstStartup.BackColor = Color.FromArgb(25, 25, 25);
            lstStartup.ForeColor = Color.White;
            lstStartup.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lstStartup.Columns.Add("Program Name", 200);
            lstStartup.Columns.Add("Status", 100);
            lstStartup.Columns.Add("Location Type", 120);
            lstStartup.Columns.Add("Command Line", 250);
            pnlStartup.Controls.Add(lstStartup);

            // Buttons
            btnToggleStartup = CreateFlatButton("Toggle Status (Enable/Disable)", 20, 560, 250, 45, Color.FromArgb(0, 120, 215), Color.White);
            btnToggleStartup.Click += (s, e) => ToggleSelectedStartup();
            pnlStartup.Controls.Add(btnToggleStartup);

            Button btnRefresh = CreateFlatButton("Refresh List", 290, 560, 150, 45, Color.FromArgb(50, 50, 50), Color.White);
            btnRefresh.Click += (s, e) => RefreshStartupList();
            pnlStartup.Controls.Add(btnRefresh);
        }

        private void RefreshStartupList()
        {
            lstStartup.Items.Clear();
            var entries = GetStartupEntries();
            foreach (var entry in entries)
            {
                ListViewItem item = new ListViewItem(entry.Name);
                item.SubItems.Add(entry.IsEnabled ? "Enabled" : "Disabled");
                item.SubItems.Add(entry.LocationType);
                item.SubItems.Add(entry.Command);
                item.ForeColor = entry.IsEnabled ? Color.LightGreen : Color.Tomato;
                item.Tag = entry;
                lstStartup.Items.Add(item);
            }
        }

        private void ToggleSelectedStartup()
        {
            if (lstStartup.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a startup item from the list first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem item = lstStartup.SelectedItems[0];
            StartupEntry entry = (StartupEntry)item.Tag;
            ToggleStartupEntry(entry);
            RefreshStartupList();
        }

        // === 4. Game Profiles Panel ===
        private void CreateGameProfilesPanel()
        {
            pnlGameProfiles = new Panel();
            pnlGameProfiles.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pnlGameProfiles);

            // Title
            Label lblHeader = new Label();
            lblHeader.Text = "Game Priority Optimization Profiles";
            lblHeader.Location = new Point(20, 20);
            lblHeader.Size = new Size(400, 25);
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(0, 215, 215);
            pnlGameProfiles.Controls.Add(lblHeader);

            // Setup inputs Panel
            Panel pnlInputs = new Panel();
            pnlInputs.Location = new Point(20, 60);
            pnlInputs.Size = new Size(280, 480);
            pnlInputs.BackColor = Color.FromArgb(25, 25, 25);
            pnlGameProfiles.Controls.Add(pnlInputs);

            Label lblInputs = new Label();
            lblInputs.Text = "Add New Game Priority";
            lblInputs.Location = new Point(15, 15);
            lblInputs.Size = new Size(250, 20);
            lblInputs.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblInputs.ForeColor = Color.FromArgb(0, 190, 190);
            pnlInputs.Controls.Add(lblInputs);

            // Game Exe Input
            Label lblGameExe = new Label();
            lblGameExe.Text = "Game Exe Name (e.g. cs2.exe):";
            lblGameExe.Location = new Point(15, 55);
            lblGameExe.Size = new Size(250, 18);
            lblGameExe.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            pnlInputs.Controls.Add(lblGameExe);

            txtGameExe = new TextBox();
            txtGameExe.Location = new Point(15, 75);
            txtGameExe.Size = new Size(250, 25);
            txtGameExe.BackColor = Color.FromArgb(40, 40, 40);
            txtGameExe.ForeColor = Color.White;
            txtGameExe.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            pnlInputs.Controls.Add(txtGameExe);

            // CPU Priority Dropdown
            Label lblCpu = new Label();
            lblCpu.Text = "Select CPU Priority Class:";
            lblCpu.Location = new Point(15, 115);
            lblCpu.Size = new Size(250, 18);
            lblCpu.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            pnlInputs.Controls.Add(lblCpu);

            cmbGameCpuPriority = new ComboBox();
            cmbGameCpuPriority.Location = new Point(15, 135);
            cmbGameCpuPriority.Size = new Size(250, 25);
            cmbGameCpuPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGameCpuPriority.Items.Add("High Priority (Recommended)");
            cmbGameCpuPriority.Items.Add("Realtime Priority");
            cmbGameCpuPriority.Items.Add("Normal Priority");
            cmbGameCpuPriority.SelectedIndex = 0;
            pnlInputs.Controls.Add(cmbGameCpuPriority);

            // I/O Priority Dropdown
            Label lblIo = new Label();
            lblIo.Text = "Select I/O Disk Priority:";
            lblIo.Location = new Point(15, 175);
            lblIo.Size = new Size(250, 18);
            lblIo.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            pnlInputs.Controls.Add(lblIo);

            cmbGameIoPriority = new ComboBox();
            cmbGameIoPriority.Location = new Point(15, 195);
            cmbGameIoPriority.Size = new Size(250, 25);
            cmbGameIoPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGameIoPriority.Items.Add("High I/O Priority (Recommended)");
            cmbGameIoPriority.Items.Add("Normal I/O Priority");
            cmbGameIoPriority.SelectedIndex = 0;
            pnlInputs.Controls.Add(cmbGameIoPriority);

            // Exclude Core 0 Checkbox
            chkExcludeCore0 = new CheckBox();
            chkExcludeCore0.Text = "Exclude CPU Core 0 (Lower Jitter)";
            chkExcludeCore0.Location = new Point(15, 235);
            chkExcludeCore0.Size = new Size(250, 20);
            chkExcludeCore0.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            chkExcludeCore0.Checked = false;
            pnlInputs.Controls.Add(chkExcludeCore0);

            // Add button
            Button btnAddProfile = CreateFlatButton("Save Game Profile", 15, 270, 250, 40, Color.FromArgb(0, 120, 215), Color.White);
            btnAddProfile.Click += (s, e) => SaveGamePriority();
            pnlInputs.Controls.Add(btnAddProfile);

            // Active Profiles List
            lstGameProfiles = new ListView();
            lstGameProfiles.Location = new Point(320, 60);
            lstGameProfiles.Size = new Size(390, 480);
            lstGameProfiles.View = View.Details;
            lstGameProfiles.FullRowSelect = true;
            lstGameProfiles.BackColor = Color.FromArgb(25, 25, 25);
            lstGameProfiles.ForeColor = Color.White;
            lstGameProfiles.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lstGameProfiles.Columns.Add("Game Executable", 140);
            lstGameProfiles.Columns.Add("CPU Priority", 80);
            lstGameProfiles.Columns.Add("I/O Priority", 70);
            lstGameProfiles.Columns.Add("CPU Affinity", 90);
            pnlGameProfiles.Controls.Add(lstGameProfiles);

            // Buttons
            Button btnRemoveProfile = CreateFlatButton("Delete Game Profile", 320, 560, 200, 45, Color.FromArgb(180, 40, 40), Color.White);
            btnRemoveProfile.Click += (s, e) => RemoveSelectedGamePriority();
            pnlGameProfiles.Controls.Add(btnRemoveProfile);
        }

        private void RefreshGameProfilesList()
        {
            lstGameProfiles.Items.Clear();
            try
            {
                using (RegistryKey root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", false))
                {
                    if (root != null)
                    {
                        foreach (string subKeyName in root.GetSubKeyNames())
                        {
                            using (RegistryKey perfKey = root.OpenSubKey(subKeyName + @"\PerfOptions", false))
                            {
                                if (perfKey != null)
                                {
                                    object cpuVal = perfKey.GetValue("CpuPriorityClass");
                                    object ioVal = perfKey.GetValue("IoPriority");
                                    object affinityVal = perfKey.GetValue("CpuAffinityMask");
 
                                    string cpuStr = "Normal";
                                    if (cpuVal != null)
                                    {
                                        int cpuValInt = Convert.ToInt32(cpuVal);
                                        if (cpuValInt == 3) cpuStr = "High";
                                        else if (cpuValInt == 4) cpuStr = "Realtime";
                                    }
 
                                    string ioStr = "Normal";
                                    if (ioVal != null)
                                    {
                                        int ioValInt = Convert.ToInt32(ioVal);
                                        if (ioValInt == 2) ioStr = "High";
                                    }

                                    string affinityStr = "All Cores";
                                    if (affinityVal != null)
                                    {
                                        long mask = Convert.ToInt64(affinityVal);
                                        long expectedMask = (1L << Environment.ProcessorCount) - 2;
                                        if (mask == expectedMask) affinityStr = "Exclude Core 0";
                                        else affinityStr = "Custom (" + mask.ToString("X") + ")";
                                    }
 
                                    ListViewItem item = new ListViewItem(subKeyName);
                                    item.SubItems.Add(cpuStr);
                                    item.SubItems.Add(ioStr);
                                    item.SubItems.Add(affinityStr);
                                    lstGameProfiles.Items.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            catch {}
        }

        private void SaveGamePriority()
        {
            string exe = txtGameExe.Text.Trim();
            if (string.IsNullOrEmpty(exe))
            {
                MessageBox.Show("Please enter a valid game executable name (e.g. cs2.exe).", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                exe += ".exe";
            }
 
            int cpuPriority = cmbGameCpuPriority.SelectedIndex == 0 ? 3 : (cmbGameCpuPriority.SelectedIndex == 1 ? 4 : 2);
            int ioPriority = cmbGameIoPriority.SelectedIndex == 0 ? 2 : 1;
            bool excludeCore0 = chkExcludeCore0.Checked;
 
            SetGamePriorityProfile(exe, cpuPriority, ioPriority, excludeCore0);
            txtGameExe.Clear();
            chkExcludeCore0.Checked = false;
            RefreshGameProfilesList();
            MessageBox.Show("Game priority profile saved successfully!", "Profile Configured", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RemoveSelectedGamePriority()
        {
            if (lstGameProfiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a game profile to delete from the list.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string exe = lstGameProfiles.SelectedItems[0].Text;
            RemoveGamePriorityProfile(exe);
            RefreshGameProfilesList();
            MessageBox.Show("Game priority profile removed successfully.", "Profile Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // === 5. Backup & Restore Panel ===
        private void CreateBackupRestorePanel()
        {
            pnlBackupRestore = new Panel();
            pnlBackupRestore.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pnlBackupRestore);

            // Title
            Label lblHeader = new Label();
            lblHeader.Text = "System Configuration Backups Manager";
            lblHeader.Location = new Point(20, 20);
            lblHeader.Size = new Size(400, 25);
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(0, 215, 215);
            pnlBackupRestore.Controls.Add(lblHeader);

            // List of backups
            lstBackups = new ListView();
            lstBackups.Location = new Point(20, 60);
            lstBackups.Size = new Size(690, 480);
            lstBackups.View = View.Details;
            lstBackups.FullRowSelect = true;
            lstBackups.BackColor = Color.FromArgb(25, 25, 25);
            lstBackups.ForeColor = Color.White;
            lstBackups.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lstBackups.Columns.Add("Backup Display Name", 350);
            lstBackups.Columns.Add("Type", 120);
            lstBackups.Columns.Add("Creation Date", 180);
            pnlBackupRestore.Controls.Add(lstBackups);

            // Buttons
            Button btnCreate = CreateFlatButton("Create New Backup", 20, 560, 200, 45, Color.FromArgb(0, 120, 215), Color.White);
            btnCreate.Click += (s, e) => {
                CreateBackupUI();
                RefreshBackupsList();
            };
            pnlBackupRestore.Controls.Add(btnCreate);

            Button btnRestore = CreateFlatButton("Restore Selected Backup", 240, 560, 220, 45, Color.FromArgb(0, 150, 100), Color.White);
            btnRestore.Click += (s, e) => RestoreSelectedBackupUI();
            pnlBackupRestore.Controls.Add(btnRestore);

            Button btnDelete = CreateFlatButton("Delete Backup", 480, 560, 150, 45, Color.FromArgb(180, 40, 40), Color.White);
            btnDelete.Click += (s, e) => DeleteSelectedBackupUI();
            pnlBackupRestore.Controls.Add(btnDelete);
        }

        struct BackupItem
        {
            public string Path;
            public string DisplayName;
            public bool IsRegistry; // true = registry, false = files backup
            public DateTime Date;
        }

        private List<BackupItem> GetBackups()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            List<BackupItem> backups = new List<BackupItem>();

            // 1. Registry backups (new dedicated "Backups" folder + legacy root for older backups)
            try
            {
                List<string> regFiles = new List<string>();
                string backupsDir = Path.Combine(baseDir, "Backups");
                if (Directory.Exists(backupsDir)) regFiles.AddRange(Directory.GetFiles(backupsDir, "debloat_backup_*.txt"));
                regFiles.AddRange(Directory.GetFiles(baseDir, "debloat_backup_*.txt"));
                foreach (string file in regFiles)
                {
                    DateTime dt = File.GetLastWriteTime(file);
                    backups.Add(new BackupItem {
                        Path = file,
                        DisplayName = "Registry Settings & Service Configs",
                        IsRegistry = true,
                        Date = dt
                    });
                }
            }
            catch {}

            // 2. Clean Backups (Files)
            try
            {
                string cleanDir = Path.Combine(baseDir, "clean_backups");
                if (Directory.Exists(cleanDir))
                {
                    foreach (string dir in Directory.GetDirectories(cleanDir, "clean_backup_*"))
                    {
                        DateTime dt = Directory.GetLastWriteTime(dir);
                        backups.Add(new BackupItem {
                            Path = dir,
                            DisplayName = "Deleted System/User Temp Files",
                            IsRegistry = false,
                            Date = dt
                        });
                    }
                }
            }
            catch {}

            backups.Sort((a, b) => b.Date.CompareTo(a.Date)); // Newest first
            return backups;
        }

        private void RefreshBackupsList()
        {
            lstBackups.Items.Clear();
            var backups = GetBackups();
            foreach (var b in backups)
            {
                ListViewItem item = new ListViewItem(b.DisplayName);
                item.SubItems.Add(b.IsRegistry ? "Registry/Settings" : "Cleaned Files");
                item.SubItems.Add(b.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                item.ForeColor = b.IsRegistry ? Color.FromArgb(235, 235, 120) : Color.LightGreen;
                item.Tag = b;
                lstBackups.Items.Add(item);
            }
        }

        private void CreateBackupUI()
        {
            string path = GetNewBackupFilePath();
            try
            {
                CreateBackupInternal(path, false);
                MessageBox.Show("A complete settings and boot store configuration backup was created successfully!", "Backup Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to create backup: " + ex.Message, "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreSelectedBackupUI()
        {
            if (lstBackups.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a backup from the list first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BackupItem b = (BackupItem)lstBackups.SelectedItems[0].Tag;
            DialogResult dialog = MessageBox.Show(string.Format("Are you sure you want to restore this configuration?\n({0})", b.Date.ToString("yyyy-MM-dd HH:mm:ss")), 
                "Confirm Restoration", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes) return;

            if (b.IsRegistry)
            {
                // Registry Restore
                RestoreRegistryBackup(b.Path);
            }
            else
            {
                // Files Restore
                RestoreFilesBackupUI(b.Path);
            }
        }

        private void DeleteSelectedBackupUI()
        {
            if (lstBackups.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a backup to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BackupItem b = (BackupItem)lstBackups.SelectedItems[0].Tag;
            DialogResult dialog = MessageBox.Show("Are you sure you want to delete this backup? This cannot be undone.", 
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog != DialogResult.Yes) return;

            try
            {
                if (b.IsRegistry)
                {
                    File.Delete(b.Path);
                    string bcdBackup = Path.Combine(Path.GetDirectoryName(b.Path), "bcd_backup_" + Path.GetFileNameWithoutExtension(b.Path));
                    if (File.Exists(bcdBackup)) File.Delete(bcdBackup);
                }
                else
                {
                    Directory.Delete(b.Path, true);
                }
                RefreshBackupsList();
                MessageBox.Show("Backup deleted successfully.", "Backup Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete backup: " + ex.Message, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // === 6. System Health Panel (verify & restore Windows system files) ===
        private void CreateHealthPanel()
        {
            pnlHealth = new Panel();
            pnlHealth.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(pnlHealth);

            Label lblHeader = new Label();
            lblHeader.Text = "System File Health - Scan & Repair";
            lblHeader.Location = new Point(20, 20);
            lblHeader.Size = new Size(500, 25);
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = Color.FromArgb(0, 215, 215);
            pnlHealth.Controls.Add(lblHeader);

            Label lblDesc = new Label();
            lblDesc.Text = "Checks Windows for missing or corrupted system files and restores them from Windows' own trusted store. Uses the built-in, safe Windows tools (DISM, SFC, CHKDSK).";
            lblDesc.Location = new Point(20, 50);
            lblDesc.Size = new Size(700, 40);
            lblDesc.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblDesc.ForeColor = Color.Gray;
            pnlHealth.Controls.Add(lblDesc);

            chkRepairDism = new CheckBox();
            chkRepairDism.Text = "Repair Windows component store  (DISM /RestoreHealth)";
            chkRepairDism.Location = new Point(22, 98);
            chkRepairDism.Size = new Size(680, 20);
            chkRepairDism.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            chkRepairDism.Checked = true;
            pnlHealth.Controls.Add(chkRepairDism);

            chkRepairSfc = new CheckBox();
            chkRepairSfc.Text = "Verify & restore protected system files  (SFC /scannow)";
            chkRepairSfc.Location = new Point(22, 123);
            chkRepairSfc.Size = new Size(680, 20);
            chkRepairSfc.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            chkRepairSfc.Checked = true;
            pnlHealth.Controls.Add(chkRepairSfc);

            chkRepairChkdsk = new CheckBox();
            chkRepairChkdsk.Text = "Scan system drive for errors  (CHKDSK C: /scan - online, safe)";
            chkRepairChkdsk.Location = new Point(22, 148);
            chkRepairChkdsk.Size = new Size(680, 20);
            chkRepairChkdsk.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            chkRepairChkdsk.Checked = false;
            pnlHealth.Controls.Add(chkRepairChkdsk);

            rchHealthLog = new RichTextBox();
            rchHealthLog.Location = new Point(20, 180);
            rchHealthLog.Size = new Size(690, 360);
            rchHealthLog.BackColor = Color.FromArgb(17, 17, 17);
            rchHealthLog.ForeColor = Color.LightGreen;
            rchHealthLog.Font = new Font("Consolas", 9f, FontStyle.Regular);
            rchHealthLog.ReadOnly = true;
            pnlHealth.Controls.Add(rchHealthLog);

            Button btnHealthRun = CreateFlatButton("Scan & Repair Now", 20, 555, 260, 45, Color.FromArgb(0, 120, 215), Color.White);
            btnHealthRun.Click += (s, e) => RunHealthCheckUI();
            pnlHealth.Controls.Add(btnHealthRun);

            Label lblNote = new Label();
            lblNote.Text = "May take 20-40 minutes. The window stays responsive; please let it finish.";
            lblNote.Location = new Point(300, 568);
            lblNote.Size = new Size(410, 20);
            lblNote.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblNote.ForeColor = Color.Gray;
            pnlHealth.Controls.Add(lblNote);
        }

        private void AppendHealthLog(string text)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(AppendHealthLog), text);
            }
            else
            {
                rchHealthLog.AppendText(text);
                rchHealthLog.SelectionStart = rchHealthLog.Text.Length;
                rchHealthLog.ScrollToCaret();
            }
        }

        private void RunHealthCheckUI()
        {
            bool doDism = chkRepairDism.Checked;
            bool doSfc = chkRepairSfc.Checked;
            bool doChkdsk = chkRepairChkdsk.Checked;

            if (!doDism && !doSfc && !doChkdsk)
            {
                MessageBox.Show("Please select at least one repair task first.", "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult dr = MessageBox.Show(
                "This scans Windows for missing or corrupted system files and repairs them from Windows' own trusted store.\n\nIt can take 20-40 minutes and the window may look busy. Continue?",
                "Scan & Repair System Files", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            rchHealthLog.Clear();
            SetControlsEnabled(false);

            System.Threading.Tasks.Task.Factory.StartNew(() => {
                AppendHealthLog("Starting system file health scan...\n");
                AppendHealthLog("======================================\n");
                bool anyProblem = false;
                bool anyRepaired = false;
                int ec = 0;

                if (doDism)
                {
                    AppendHealthLog("\n[DISM] Checking component store health...\n");
                    RunHealthCommand("DISM.exe", "/Online /Cleanup-Image /ScanHealth", out ec);

                    AppendHealthLog("\n[DISM] Repairing component store (this can take 10-20 minutes)...\n");
                    string restore = RunHealthCommand("DISM.exe", "/Online /Cleanup-Image /RestoreHealth", out ec).ToLower();
                    if (restore.Contains("no component store corruption") || restore.Contains("operation completed successfully") || ec == 0)
                        AppendHealthLog(" -> Component store is healthy / repaired.\n");
                    else
                    {
                        AppendHealthLog(" -> DISM reported an issue (exit code " + ec + ").\n");
                        anyProblem = true;
                    }
                }

                if (doSfc)
                {
                    AppendHealthLog("\n[SFC] Verifying and restoring protected system files (this can take 10-30 minutes)...\n");
                    string sfc = RunHealthCommand("sfc.exe", "/scannow", out ec).ToLower();
                    if (sfc.Contains("did not find any integrity violations"))
                        AppendHealthLog(" -> No integrity violations. All system files are intact.\n");
                    else if (sfc.Contains("successfully repaired"))
                    {
                        AppendHealthLog(" -> Found corrupted files and successfully repaired them.\n");
                        anyRepaired = true;
                    }
                    else if (sfc.Contains("unable to fix") || sfc.Contains("could not perform"))
                    {
                        AppendHealthLog(" -> Found problems it could not fix automatically (reboot then re-run, or run DISM first).\n");
                        anyProblem = true;
                    }
                    else
                        AppendHealthLog(" -> SFC finished (exit code " + ec + ").\n");
                }

                if (doChkdsk)
                {
                    AppendHealthLog("\n[CHKDSK] Scanning the system drive for errors (online, safe)...\n");
                    string chk = RunHealthCommand("chkdsk.exe", "C: /scan", out ec).ToLower();
                    if (ec == 0 || chk.Contains("no problems") || chk.Contains("found no problems"))
                        AppendHealthLog(" -> No drive errors found.\n");
                    else
                    {
                        AppendHealthLog(" -> Drive scan reported issues (exit code " + ec + "). Consider 'chkdsk C: /f' on next reboot.\n");
                        anyProblem = true;
                    }
                }

                this.Invoke(new MethodInvoker(() => {
                    rchHealthLog.AppendText("\n======================================\n");
                    rchHealthLog.AppendText("System health scan completed.\n");
                    string summary;
                    if (anyProblem)
                        summary = "Some items need attention - see the log above. A restart and a second run usually clear the rest.";
                    else if (anyRepaired)
                        summary = "Corrupted system files were found and repaired successfully. A restart is recommended.";
                    else
                        summary = "Your Windows system files are healthy - nothing missing or corrupted.";
                    rchHealthLog.AppendText(summary + "\n");
                    SetControlsEnabled(true);
                    MessageBox.Show(summary, "Health Scan Completed", MessageBoxButtons.OK, anyProblem ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }));
            });
        }

        // Runs a long maintenance command, streaming its output to the health log.
        // Uses async reads + an unbounded WaitForExit so SFC/DISM are never killed mid-repair.
        private string RunHealthCommand(string fileName, string arguments, out int exitCode)
        {
            exitCode = -1;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = fileName;
                start.Arguments = arguments;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.CreateNoWindow = true;

                using (Process process = new Process())
                {
                    process.StartInfo = start;
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data == null) return;
                        sb.AppendLine(e.Data);
                        string line = e.Data.Replace("\0", "").Trim();
                        if (line.Length > 0) AppendHealthLog("   " + line + "\n");
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data == null) return;
                        string line = e.Data.Replace("\0", "").Trim();
                        if (line.Length > 0) AppendHealthLog("   " + line + "\n");
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                AppendHealthLog("   [error] " + ex.Message + "\n");
            }
            return sb.ToString().Replace("\0", "");
        }

        // === System Execution Methods ===

        static string GetNewBackupFilePath()
        {
            // All registry/system backups are gathered in a single dedicated "Backups" folder.
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            try { Directory.CreateDirectory(dir); } catch {}
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(dir, string.Format("debloat_backup_{0}.txt", timestamp));
        }

        static bool CreateBackupSilently()
        {
            string path = GetNewBackupFilePath();
            try
            {
                CreateBackupInternal(path, false);
                // Verify the backup was actually written and is non-trivial before trusting it.
                return File.Exists(path) && new FileInfo(path).Length > 0;
            }
            catch { return false; }
        }

        static void CreateBackupInternal(string path, bool verbose)
        {
            // Write to a temp file first and promote it only after a fully successful write, so a
            // failure can never leave a half-written backup that would silently break restore.
            string tempPath = path + ".tmp";
            string bcdTmp = tempPath + ".bcd";
            try
            {
                // Export the BCD boot store to a temp file so it can be embedded inside the backup,
                // keeping each backup a single self-contained file (no separate bcd_backup_ file).
                try { if (File.Exists(bcdTmp)) File.Delete(bcdTmp); } catch {}
                RunCommand("bcdedit", string.Format("/export \"{0}\"", bcdTmp));

                using (StreamWriter writer = new StreamWriter(tempPath, false, System.Text.Encoding.UTF8))
                {
                    // Embed the BCD store as a single base64 line (if the export produced a file).
                    try
                    {
                        if (File.Exists(bcdTmp))
                        {
                            byte[] bcdBytes = File.ReadAllBytes(bcdTmp);
                            writer.WriteLine("BCDSTORE|" + Convert.ToBase64String(bcdBytes));
                        }
                    }
                    catch {}

                    // A. Backup standard array tweaks
                    foreach (var tweak in tweaks)
                    {
                        object currentValue = GetRegistryValue(tweak.Hive, tweak.SubKey, tweak.ValueName);
                        string status = (currentValue != null) ? "Present" : "NotPresent";
                        string valueData = (currentValue != null) ? currentValue.ToString() : "";
                        string kindStr = tweak.ValueKind.ToString();

                        writer.WriteLine(string.Format("{0}|{1}|{2}|{3}|{4}|{5}", 
                            tweak.Hive, tweak.SubKey, tweak.ValueName, kindStr, status, valueData));
                    }

                    // B. Backup Tcpip Network Interfaces (Nagle settings)
                    string interfacesPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
                    using (RegistryKey root = Registry.LocalMachine.OpenSubKey(interfacesPath))
                    {
                        if (root != null)
                        {
                            foreach (string subKeyName in root.GetSubKeyNames())
                            {
                                string fullSubKeyPath = Path.Combine(interfacesPath, subKeyName);
                                using (RegistryKey interfaceKey = root.OpenSubKey(subKeyName))
                                {
                                    if (interfaceKey != null)
                                    {
                                        object ackVal = interfaceKey.GetValue("TCPAckFrequency", null);
                                        object delayVal = interfaceKey.GetValue("TCPNoDelay", null);

                                        string ackStatus = (ackVal != null) ? "Present" : "NotPresent";
                                        string ackData = (ackVal != null) ? ackVal.ToString() : "";

                                        string delayStatus = (delayVal != null) ? "Present" : "NotPresent";
                                        string delayData = (delayVal != null) ? delayVal.ToString() : "";

                                        writer.WriteLine(string.Format("HKLM|{0}|TCPAckFrequency|DWord|{1}|{2}", fullSubKeyPath, ackStatus, ackData));
                                        writer.WriteLine(string.Format("HKLM|{0}|TCPNoDelay|DWord|{1}|{2}", fullSubKeyPath, delayStatus, delayData));
                                    }
                                }
                            }
                        }
                    }

                    // C. Backup Memory Compression (PowerShell MMAgent)
                    string memCompEnabled = GetMemoryCompressionStatus();
                    writer.WriteLine(string.Format("SPECIAL|MemoryAgent|MemoryCompression|Special|Present|{0}", memCompEnabled));

                    // D. Backup Hibernation state (Hiberfil)
                    bool hiberExists = false;
                    try
                    {
                        FileInfo fi = new FileInfo(@"C:\hiberfil.sys");
                        if (fi.Exists) hiberExists = true;
                    }
                    catch {}
                    writer.WriteLine(string.Format("SPECIAL|PowerAgent|Hibernation|Special|Present|{0}", hiberExists ? "True" : "False"));

                    // E. Backup Active Power Scheme
                    string activeScheme = GetActivePowerScheme();
                    writer.WriteLine(string.Format("SPECIAL|PowerAgent|ActiveScheme|Special|Present|{0}", activeScheme));

                    // E2. Backup Pagefile configuration (auto-managed flag + raw PagingFiles list)
                    string autoManaged = GetAutomaticManagedPagefile();
                    string pagingFilesJoined = "";
                    try
                    {
                        object pf = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "PagingFiles", null);
                        string[] pfArr = pf as string[];
                        if (pfArr != null) pagingFilesJoined = string.Join("~~", pfArr);
                    }
                    catch {}
                    writer.WriteLine(string.Format("SPECIAL|PageAgent|PagefileState|Special|Present|{0}::{1}", autoManaged, pagingFilesJoined));

                    // F. Backup Services Startup Types
                    foreach (string service in managedServices)
                    {
                        int startupType = GetServiceStartupType(service);
                        writer.WriteLine(string.Format("SPECIAL|ServiceAgent|{0}|Special|Present|{1}", service, startupType));
                    }

                    // G. Backup Network Adapter Class Configurations
                    BackupNetworkAdapterClassConfigs(writer);

                    // H. Backup GPU MSI Mode & Priority Settings
                    OptimizeGpuMsi(false, writer);

                    // I. Backup Scheduled Tasks State
                    foreach (string task in telemetryTasks)
                    {
                        string status = GetTaskStatus(task);
                        if (status != "NotFound")
                        {
                            writer.WriteLine(string.Format("SPECIAL|TaskAgent|{0}|Special|Present|{1}", task, status));
                        }
                    }

                    // J. Backup DNS and NetBIOS settings
                    string ipInterfacesPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
                    using (RegistryKey root = Registry.LocalMachine.OpenSubKey(ipInterfacesPath))
                    {
                        if (root != null)
                        {
                            foreach (string subKeyName in root.GetSubKeyNames())
                            {
                                string fullPath = Path.Combine(ipInterfacesPath, subKeyName);
                                using (RegistryKey key = root.OpenSubKey(subKeyName))
                                {
                                    if (key != null)
                                    {
                                        object nsVal = key.GetValue("NameServer", null);
                                        string nsStatus = (nsVal != null) ? "Present" : "NotPresent";
                                        string nsData = (nsVal != null) ? nsVal.ToString() : "";
                                        writer.WriteLine(string.Format("HKLM|{0}|NameServer|String|{1}|{2}", fullPath, nsStatus, nsData));
                                    }
                                }
                            }
                        }
                    }

                    string netbtPath = @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces";
                    using (RegistryKey root = Registry.LocalMachine.OpenSubKey(netbtPath))
                    {
                        if (root != null)
                        {
                            foreach (string subKeyName in root.GetSubKeyNames())
                            {
                                string fullPath = Path.Combine(netbtPath, subKeyName);
                                using (RegistryKey key = root.OpenSubKey(subKeyName))
                                {
                                    if (key != null)
                                    {
                                        object nbVal = key.GetValue("NetbiosOptions", null);
                                        string nbStatus = (nbVal != null) ? "Present" : "NotPresent";
                                        string nbData = (nbVal != null) ? nbVal.ToString() : "";
                                        writer.WriteLine(string.Format("HKLM|{0}|NetbiosOptions|DWord|{1}|{2}", fullPath, nbStatus, nbData));
                                    }
                                }
                            }
                        }
                    }

                    // K. Backup Windows Optional Features
                    foreach (string feature in optionalFeatures)
                    {
                        string featState = GetFeatureState(feature);
                        writer.WriteLine(string.Format("SPECIAL|FeatureAgent|{0}|Special|Present|{1}", feature, featState));
                    }
                }

                // Clean up the temp BCD export now that it is embedded in the backup file.
                try { if (File.Exists(bcdTmp)) File.Delete(bcdTmp); } catch {}

                // Atomically promote the completed temp file to the real backup path.
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
            }
            catch
            {
                // Discard the partial temp files and rethrow so callers know the backup failed.
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch {}
                try { if (File.Exists(bcdTmp)) File.Delete(bcdTmp); } catch {}
                throw;
            }
        }

        private void RestoreRegistryBackup(string path)
        {
            SetControlsEnabled(false);

            System.Threading.Tasks.Task.Factory.StartNew(() => {
                // Import BCD Boot Config
                try
                {
                    string bcdBackupPath = Path.Combine(Path.GetDirectoryName(path), "bcd_backup_" + Path.GetFileNameWithoutExtension(path));
                    if (File.Exists(bcdBackupPath))
                    {
                        RunCommand("bcdedit", string.Format("/import \"{0}\"", bcdBackupPath));
                    }
                }
                catch {}

                // Reinstall uninstalled UWP Apps
                try
                {
                    string[] UwpApps = new string[]
                    {
                        "3DBuilder", "GetHelp", "Getstarted", "Messaging", "Microsoft3DViewer", "MicrosoftOfficeHub",
                        "MicrosoftSolitaireCollection", "MixedReality.Portal", "NetworkSpeedTest", "OneNote", "People",
                        "SkypeApp", "FeedbackHub", "YourPhone", "ZuneMusic", "ZuneVideo", "BingNews", "BingSports", "BingWeather"
                    };
                    foreach (string appName in UwpApps)
                    {
                        RunCommand("powershell", string.Format("-Command \"Get-AppxPackage -AllUsers *{0}* | Foreach {{Add-AppxPackage -DisableDevelopmentMode -Register \\\"$($_.InstallLocation)\\AppxManifest.xml\\\" -ErrorAction SilentlyContinue}}\"", appName));
                    }
                }
                catch {}

                // Revert system commands first
                try { ApplySystemCommands(false); } catch {}

                // Revert network interface optimizations
                try { OptimizeNetworkAdapters(false); } catch {}

                int restoreCount = 0;
                int skippedCount = 0;
                try
                {
                    string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;

                        // Embedded BCD store (new single-file backups): decode base64 and import it.
                        if (line.StartsWith("BCDSTORE|"))
                        {
                            try
                            {
                                byte[] bcdBytes = Convert.FromBase64String(line.Substring(9));
                                string bcdRestoreTmp = Path.Combine(Path.GetDirectoryName(path), "bcd_restore_" + Path.GetFileNameWithoutExtension(path) + ".tmp");
                                File.WriteAllBytes(bcdRestoreTmp, bcdBytes);
                                RunCommand("bcdedit", string.Format("/import \"{0}\"", bcdRestoreTmp));
                                try { File.Delete(bcdRestoreTmp); } catch {}
                            }
                            catch {}
                            continue;
                        }

                        string[] parts = line.Split('|');
                        if (parts.Length < 6) continue;

                        string hive = parts[0];
                        string subKey = parts[1];
                        string valueName = parts[2];
                        string kindStr = parts[3];
                        string status = parts[4];
                        string valueData = parts[5];

                        try
                        {
                            if (hive == "SPECIAL")
                            {
                                if (subKey == "MemoryAgent" && valueName == "MemoryCompression")
                                {
                                    // Only act on a definite captured state. "Unknown" means the
                                    // backup-time query failed; skip to avoid wrongly toggling it.
                                    if (valueData == "True" || valueData == "False")
                                    {
                                        SetMemoryCompression(valueData == "True");
                                    }
                                }
                                else if (subKey == "PowerAgent" && valueName == "Hibernation")
                                {
                                    bool originalEnabled = (valueData == "True");
                                    RunCommand("powercfg", originalEnabled ? "-h on" : "-h off");
                                }
                                else if (subKey == "PowerAgent" && valueName == "ActiveScheme")
                                {
                                    RunCommand("powercfg", "-setactive " + valueData);
                                }
                                else if (subKey == "ServiceAgent")
                                {
                                    string serviceName = valueName;
                                    int startType = int.Parse(valueData);
                                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\" + serviceName, "Start", startType, RegistryValueKind.DWord);
                                    if (startType == 4) RunCommand("sc", "stop " + serviceName);
                                    else RunCommand("sc", "start " + serviceName);
                                }
                                else if (subKey == "NetworkAgent")
                                {
                                    if (parts.Length >= 7)
                                    {
                                        string adapterSubKey = parts[2];
                                        string valName = parts[3];
                                        string netStatus = parts[5];
                                        string netData = parts[6];
                                        string adapterPath = string.Format(@"SYSTEM\CurrentControlSet\Control\Class\{{4d36e972-e325-11ce-bfc1-08002be10318}}\{0}", adapterSubKey);
                                        
                                        if (netStatus == "NotPresent") DeleteRegistryValueOrKey("HKLM", adapterPath, valName);
                                        else Registry.SetValue(@"HKEY_LOCAL_MACHINE\" + adapterPath, valName, netData, RegistryValueKind.String);
                                    }
                                }
                                else if (subKey == "TaskAgent")
                                {
                                    string taskPath = valueName;
                                    string originalStatus = valueData;
                                    if (originalStatus == "Disabled") SetTaskState(taskPath, false);
                                    else if (originalStatus == "Ready" || originalStatus == "Running") SetTaskState(taskPath, true);
                                }
                                else if (subKey == "FeatureAgent")
                                {
                                    string featureName = valueName;
                                    // Only act on a definite captured state; "Unknown" means the dism
                                    // query failed at backup time, so skip to avoid wrongly toggling it.
                                    if (valueData == "True") SetFeatureState(featureName, true);
                                    else if (valueData == "False") SetFeatureState(featureName, false);
                                }
                                else if (subKey == "PageAgent" && valueName == "PagefileState")
                                {
                                    string[] seg = valueData.Split(new string[] { "::" }, StringSplitOptions.None);
                                    string auto = seg.Length > 0 ? seg[0] : "Unknown";
                                    string paging = seg.Length > 1 ? seg[1] : "";
                                    if (auto == "True")
                                    {
                                        RunCommand("wmic", string.Format("computersystem where name=\"{0}\" set AutomaticManagedPagefile=True", Environment.MachineName));
                                    }
                                    else if (auto == "False")
                                    {
                                        RunCommand("wmic", string.Format("computersystem where name=\"{0}\" set AutomaticManagedPagefile=False", Environment.MachineName));
                                        if (!string.IsNullOrEmpty(paging))
                                        {
                                            string[] entries = paging.Split(new string[] { "~~" }, StringSplitOptions.RemoveEmptyEntries);
                                            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "PagingFiles", entries, RegistryValueKind.MultiString);
                                        }
                                    }
                                    // "Unknown" -> leave the pagefile configuration untouched
                                }
                                restoreCount++;
                                continue;
                            }

                            if (hive == "GPU_MSI")
                            {
                                if (status == "NotPresent") DeleteRegistryValueOrKey("HKLM", subKey, valueName);
                                else
                                {
                                    RegistryValueKind kind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), kindStr);
                                    object parsedValue = ParseRegistryData(valueData, kind);
                                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\" + subKey, valueName, parsedValue, kind);
                                }
                                restoreCount++;
                                continue;
                            }

                            if (status == "NotPresent") DeleteRegistryValueOrKey(hive, subKey, valueName);
                            else
                            {
                                RegistryValueKind kind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), kindStr);
                                object parsedValue = ParseRegistryData(valueData, kind);
                                SetRegistryValue(hive, subKey, valueName, parsedValue, kind);
                            }
                            restoreCount++;
                        }
                        catch { skippedCount++; }
                    }

                    // If the system is currently on the custom Ultimate scheme, switch to Balanced
                    // BEFORE deleting it, so restore never strands the system on a deleted scheme.
                    if (GetActivePowerScheme().Equals(UltimatePerfGuid, StringComparison.OrdinalIgnoreCase))
                    {
                        RunCommand("powercfg", "-setactive " + BalancedGuid);
                    }
                    // Revert custom power scheme and flush DNS
                    RunCommand("powercfg", "-delete " + UltimatePerfGuid);
                    RunCommand("ipconfig", "/flushdns");

                    this.Invoke(new MethodInvoker(() => {
                        RestartExplorer();
                        SetControlsEnabled(true);
                        string restoreMsg = string.Format("Restored {0} registry/service parameters successfully! Explorer was restarted.", restoreCount);
                        if (skippedCount > 0) restoreMsg += string.Format("\n\nNote: {0} entr(ies) could not be restored and were skipped.", skippedCount);
                        MessageBox.Show(restoreMsg, "Restoration Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshBackupsList();
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new MethodInvoker(() => {
                        SetControlsEnabled(true);
                        MessageBox.Show("Error during restore: " + ex.Message, "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        private void RestoreFilesBackupUI(string backupDir)
        {
            SetControlsEnabled(false);

            System.Threading.Tasks.Task.Factory.StartNew(() => {
                int filesRestored = 0;
                int foldersCreated = 0;
                int failedCount = 0;

                try
                {
                    string[] files = Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        try
                        {
                            string relPath = file.Substring(backupDir.Length + 1);
                            string drive = relPath.Substring(0, 1);
                            string rest = relPath.Substring(2);
                            string originalPath = drive + @":\" + rest;

                            string destDir = Path.GetDirectoryName(originalPath);
                            if (!Directory.Exists(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                                foldersCreated++;
                            }

                            File.Copy(file, originalPath, true);
                            filesRestored++;
                        }
                        catch
                        {
                            failedCount++;
                        }
                    }

                    this.Invoke(new MethodInvoker(() => {
                        SetControlsEnabled(true);
                        string msg = string.Format("Files restore completed!\n- Restored: {0} files\n- Created: {1} directories", filesRestored, foldersCreated);
                        if (failedCount > 0) msg += string.Format("\n- Failed to restore {0} files (in use or access denied)", failedCount);

                        MessageBox.Show(msg, "Files Restore Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshBackupsList();
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new MethodInvoker(() => {
                        SetControlsEnabled(true);
                        MessageBox.Show("Error restoring files: " + ex.Message, "Files Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        // Registry Helpers
        static object GetRegistryValue(string hive, string subKey, string valueName)
        {
            string rootPath = hive == "HKLM" ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
            return Registry.GetValue(string.Format(@"{0}\{1}", rootPath, subKey), valueName, null);
        }

        static void SetRegistryValue(string hive, string subKey, string valueName, object value, RegistryValueKind kind)
        {
            string rootPath = hive == "HKLM" ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
            Registry.SetValue(string.Format(@"{0}\{1}", rootPath, subKey), valueName, value, kind);
        }

        static void DeleteRegistryValueOrKey(string hive, string subKey, string valueName)
        {
            RegistryKey baseKey = hive == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;

            if (subKey.EndsWith("InprocServer32") && string.IsNullOrEmpty(valueName))
            {
                string parentKeyPath = subKey.Replace(@"\InprocServer32", "");
                using (RegistryKey key = baseKey.OpenSubKey(parentKeyPath, true))
                {
                    if (key != null) key.DeleteSubKeyTree("InprocServer32", false);
                }
                
                string clsidRoot = @"Software\Classes\CLSID";
                string targetClsid = parentKeyPath.Substring(parentKeyPath.LastIndexOf('\\') + 1);
                using (RegistryKey key = baseKey.OpenSubKey(clsidRoot, true))
                {
                    if (key != null) key.DeleteSubKeyTree(targetClsid, false);
                }
                return;
            }

            using (RegistryKey key = baseKey.OpenSubKey(subKey, true))
            {
                if (key != null) key.DeleteValue(valueName, false);
            }
        }

        static object ParseRegistryData(string data, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    return int.Parse(data);
                case RegistryValueKind.QWord:
                    return long.Parse(data);
                default:
                    return data;
            }
        }

        // Advanced Network helper
        static void OptimizeNetworkAdapters(bool enable)
        {
            string classPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
            using (RegistryKey classKey = Registry.LocalMachine.OpenSubKey(classPath, true))
            {
                if (classKey == null) return;
                foreach (string subKeyName in classKey.GetSubKeyNames())
                {
                    if (subKeyName.Length != 4) continue;
                    using (RegistryKey adapterKey = classKey.OpenSubKey(subKeyName, true))
                    {
                        if (adapterKey == null) continue;
                        object driverDesc = adapterKey.GetValue("DriverDesc", null);
                        if (driverDesc == null) continue;

                        if (enable)
                        {
                            foreach (string val in adapterOffloadValues)
                            {
                                adapterKey.SetValue(val, "0", RegistryValueKind.String);
                            }
                            adapterKey.SetValue("*NumRssQueues", "2", RegistryValueKind.String);
                        }
                        else
                        {
                            foreach (string val in adapterOffloadValues)
                            {
                                adapterKey.DeleteValue(val, false);
                            }
                            adapterKey.DeleteValue("*NumRssQueues", false);
                        }
                    }
                }
            }
        }

        static void BackupNetworkAdapterClassConfigs(StreamWriter writer)
        {
            string classPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
            using (RegistryKey classKey = Registry.LocalMachine.OpenSubKey(classPath, false))
            {
                if (classKey == null) return;
                foreach (string subKeyName in classKey.GetSubKeyNames())
                {
                    if (subKeyName.Length != 4) continue;
                    using (RegistryKey adapterKey = classKey.OpenSubKey(subKeyName, false))
                    {
                        if (adapterKey == null) continue;
                        object driverDesc = adapterKey.GetValue("DriverDesc", null);
                        if (driverDesc == null) continue;

                        List<string> values = new List<string>(adapterOffloadValues);
                        values.Add("*NumRssQueues");
                        foreach (string valName in values)
                        {
                            object val = adapterKey.GetValue(valName, null);
                            string status = (val != null) ? "Present" : "NotPresent";
                            string data = (val != null) ? val.ToString() : "";
                            writer.WriteLine(string.Format("SPECIAL|NetworkAgent|{0}|{1}|String|{2}|{3}", subKeyName, valName, status, data));
                        }
                    }
                }
            }
        }

        // Memory Compression helper
        static string GetMemoryCompressionStatus()
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "powershell.exe";
                start.Arguments = "-Command \"(Get-MMAgent).MemoryCompression\"";
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        return reader.ReadToEnd().Trim();
                    }
                }
            }
            catch { return "Unknown"; }
        }

        static void SetMemoryCompression(bool enable)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "powershell.exe";
                start.Arguments = string.Format("-Command \"{0}-MMAgent -mc\"", enable ? "Enable" : "Disable");
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    if (process != null && !process.WaitForExit(5000)) { try { process.Kill(); } catch {} }
                }
            }
            catch {}
        }

        static void OptimizeNetworkAdapterSetting(string valName, bool enable)
        {
            string classPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
            using (RegistryKey classKey = Registry.LocalMachine.OpenSubKey(classPath, true))
            {
                if (classKey == null) return;
                foreach (string subKeyName in classKey.GetSubKeyNames())
                {
                    if (subKeyName.Length != 4) continue;
                    using (RegistryKey adapterKey = classKey.OpenSubKey(subKeyName, true))
                    {
                        if (adapterKey == null) continue;
                        object driverDesc = adapterKey.GetValue("DriverDesc", null);
                        if (driverDesc == null) continue;
 
                        List<string> targetValues = new List<string>();
                        if (valName == "*LsoV2IPv4")
                        {
                            targetValues.Add("*LsoV2IPv4");
                            targetValues.Add("*LsoV2IPv6");
                        }
                        else if (valName == "ChecksumOffload")
                        {
                            targetValues.Add("*IPChecksumOffloadIPv4");
                            targetValues.Add("*TCPChecksumOffloadIPv4");
                            targetValues.Add("*TCPChecksumOffloadIPv6");
                            targetValues.Add("*UDPChecksumOffloadIPv4");
                            targetValues.Add("*UDPChecksumOffloadIPv6");
                        }
                        else if (valName == "*FlowControl")
                        {
                            targetValues.Add("*FlowControl");
                        }
                        else if (valName == "*NumRssQueues")
                        {
                            targetValues.Add("*NumRssQueues");
                        }
 
                        foreach (string val in targetValues)
                        {
                            if (enable)
                            {
                                if (val == "*NumRssQueues")
                                {
                                    adapterKey.SetValue(val, "2", RegistryValueKind.String);
                                }
                                else
                                {
                                    adapterKey.SetValue(val, "0", RegistryValueKind.String);
                                }
                            }
                            else
                            {
                                adapterKey.DeleteValue(val, false);
                            }
                        }
                    }
                }
            }
        }

        static void SetAmdIntelShaderCache(bool enable)
        {
            try
            {
                // AMD Class Settings
                string classPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
                using (RegistryKey classKey = Registry.LocalMachine.OpenSubKey(classPath, true))
                {
                    if (classKey != null)
                    {
                        foreach (string subKeyName in classKey.GetSubKeyNames())
                        {
                            if (subKeyName.Length != 4) continue;
                            using (RegistryKey driverKey = classKey.OpenSubKey(subKeyName, true))
                            {
                                if (driverKey != null)
                                {
                                    object driverDesc = driverKey.GetValue("DriverDesc");
                                    if (driverDesc != null && (driverDesc.ToString().ToLower().Contains("amd") || driverDesc.ToString().ToLower().Contains("radeon")))
                                    {
                                        if (enable)
                                        {
                                            driverKey.SetValue("ShaderCache", "2", RegistryValueKind.String);
                                            driverKey.SetValue("ShaderCache_USE", "2", RegistryValueKind.String);
                                        }
                                        else
                                        {
                                            driverKey.DeleteValue("ShaderCache", false);
                                            driverKey.DeleteValue("ShaderCache_USE", false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // AMD HK3D Settings
                if (enable)
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\AMD\HK3D\SpecialSettings", "ShaderCache", 2, RegistryValueKind.DWord);
                }
                else
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\AMD\HK3D\SpecialSettings", true))
                    {
                        if (key != null) key.DeleteValue("ShaderCache", false);
                    }
                }

                // Intel GMM Settings
                if (enable)
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\GMM", "EnableShaderCache", 1, RegistryValueKind.DWord);
                }
                else
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Intel\GMM", true))
                    {
                        if (key != null) key.DeleteValue("EnableShaderCache", false);
                    }
                }
            }
            catch {}
        }

        static void ApplySpecialTweak(string subKey, string valueName, bool enable)
        {
            if (subKey == "NetworkAdapter")
            {
                OptimizeNetworkAdapterSetting(valueName, enable);
            }
            else if (subKey == "DNS")
            {
                if (valueName == "CloudflareDNS")
                {
                    string ipInterfacesPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
                    using (RegistryKey root = Registry.LocalMachine.OpenSubKey(ipInterfacesPath, true))
                    {
                        if (root != null)
                        {
                            foreach (string subKeyName in root.GetSubKeyNames())
                            {
                                using (RegistryKey key = root.OpenSubKey(subKeyName, true))
                                {
                                    if (key != null)
                                    {
                                        if (enable) key.SetValue("NameServer", "1.1.1.1,1.0.0.1", RegistryValueKind.String);
                                        else key.DeleteValue("NameServer", false);
                                    }
                                }
                            }
                        }
                    }
                    RunCommand("ipconfig", "/flushdns");
                }
            }
            else if (subKey == "NetBIOS")
            {
                if (valueName == "DisableNetBIOS")
                {
                    string netbtPath = @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces";
                    using (RegistryKey root = Registry.LocalMachine.OpenSubKey(netbtPath, true))
                    {
                        if (root != null)
                        {
                            foreach (string subKeyName in root.GetSubKeyNames())
                            {
                                using (RegistryKey key = root.OpenSubKey(subKeyName, true))
                                {
                                    if (key != null)
                                    {
                                        if (enable) key.SetValue("NetbiosOptions", 2, RegistryValueKind.DWord);
                                        else key.SetValue("NetbiosOptions", 0, RegistryValueKind.DWord);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (subKey == "TcpGlobal")
            {
                if (valueName == "OptimizeTcpGlobal")
                {
                    if (enable)
                    {
                        RunCommand("netsh", "int tcp set heuristics disabled");
                        RunCommand("netsh", "int tcp set global rss=enabled");
                        RunCommand("netsh", "int tcp set global autotuninglevel=disabled");
                        RunCommand("netsh", "int tcp set global chimney=disabled");
                    }
                    else
                    {
                        RunCommand("netsh", "int tcp set heuristics enabled");
                        RunCommand("netsh", "int tcp set global rss=enabled");
                        RunCommand("netsh", "int tcp set global autotuninglevel=normal");
                        RunCommand("netsh", "int tcp set global chimney=default");
                    }
                }
            }
            else if (subKey == "Service")
            {
                if (enable)
                {
                    RunCommand("sc", "config " + valueName + " start= disabled");
                    RunCommand("sc", "stop " + valueName);
                }
                else
                {
                    string startType = "demand";
                    if (valueName == "DiagTrack" || valueName == "SysMain" || valueName == "Spooler") startType = "auto";
                    else if (valueName == "WSearch") startType = "delayed-auto";
                    
                    RunCommand("sc", "config " + valueName + " start= " + startType);
                    if (startType == "auto" || startType == "delayed-auto") RunCommand("sc", "start " + valueName);
                }
            }
            else if (subKey == "PowerScheme")
            {
                if (valueName == "UltimatePerformance")
                {
                    if (enable)
                    {
                        RunCommand("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 e9a42b02-d5df-448d-aa00-03f14749eb61");
                        RunCommand("powercfg", "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
                    }
                    else
                    {
                        RunCommand("powercfg", "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                        RunCommand("powercfg", "-delete e9a42b02-d5df-448d-aa00-03f14749eb61");
                    }
                }
            }
            else if (subKey == "PowerSetting")
            {
                if (valueName == "CPMINCORES")
                {
                    if (enable)
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 SUB_PROCESSOR CPMINCORES 100");
                    }
                    else
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 5");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 SUB_PROCESSOR CPMINCORES 5");
                    }
                }
                else if (valueName == "USBSUSPEND")
                {
                    if (enable)
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ee3-487e-a10f-1b3f3a902462 0");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ee3-487e-a10f-1b3f3a902462 0");
                    }
                    else
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ee3-487e-a10f-1b3f3a902462 1");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ee3-487e-a10f-1b3f3a902462 1");
                    }
                }
                else if (valueName == "PCIEASPM")
                {
                    if (enable)
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT 501a4d13-42af-4429-9fd1-a821a10cd04b ee12f27e-b9a3-4b6a-9dfc-ea3a450011c5 0");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 501a4d13-42af-4429-9fd1-a821a10cd04b ee12f27e-b9a3-4b6a-9dfc-ea3a450011c5 0");
                    }
                    else
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT 501a4d13-42af-4429-9fd1-a821a10cd04b ee12f27e-b9a3-4b6a-9dfc-ea3a450011c5 1");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 501a4d13-42af-4429-9fd1-a821a10cd04b ee12f27e-b9a3-4b6a-9dfc-ea3a450011c5 1");
                    }
                }
                else if (valueName == "CSTATES")
                {
                    if (enable)
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT 54533251-82be-4824-96c1-47b60b740d00 5d76a2ca-e8c1-401f-a6cb-a77a63db5774 1");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 54533251-82be-4824-96c1-47b60b740d00 5d76a2ca-e8c1-401f-a6cb-a77a63db5774 1");
                    }
                    else
                    {
                        RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT 54533251-82be-4824-96c1-47b60b740d00 5d76a2ca-e8c1-401f-a6cb-a77a63db5774 0");
                        RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 54533251-82be-4824-96c1-47b60b740d00 5d76a2ca-e8c1-401f-a6cb-a77a63db5774 0");
                    }
                }
                RunCommand("powercfg", "-setactive SCHEME_CURRENT");
            }
            else if (subKey == "LatencyBCDTweak")
            {
                if (valueName == "HPET_Disable")
                {
                    if (enable)
                    {
                        RunCommand("bcdedit", "/set useplatformclock no");
                        RunCommand("bcdedit", "/set disabledynamictick yes");
                    }
                    else
                    {
                        RunCommand("bcdedit", "/deletevalue useplatformclock");
                        RunCommand("bcdedit", "/deletevalue disabledynamictick");
                    }
                }
            }
            else if (subKey == "MemoryCompression")
            {
                if (valueName == "DisableMemComp")
                {
                    SetMemoryCompression(!enable);
                }
            }
            else if (subKey == "FixedPagefile")
            {
                if (valueName == "PagefileConfig")
                {
                    if (enable)
                    {
                        SetFixedPagefile();
                    }
                    else
                    {
                        RunCommand("wmic", string.Format("computersystem where name=\"{0}\" set AutomaticManagedPagefile=True", Environment.MachineName));
                    }
                }
            }
            else if (subKey == "OptionalFeatures")
            {
                if (valueName == "DisableFeatures")
                {
                    foreach (string feature in optionalFeatures)
                    {
                        if (enable)
                        {
                            if (IsFeatureEnabled(feature)) SetFeatureState(feature, false);
                        }
                        else
                        {
                            if (!IsFeatureEnabled(feature)) SetFeatureState(feature, true);
                        }
                    }
                }
            }
            else if (subKey == "DefenderExclusions")
            {
                if (valueName == "GameExclusions")
                {
                    if (enable)
                    {
                        RunCommand("powershell", "-Command \"Add-MpPreference -ExclusionPath 'C:\\Program Files (x86)\\Steam\\steamapps\\common', 'C:\\Program Files\\Epic Games', 'C:\\Program Files (x86)\\GOG Galaxy' -ErrorAction SilentlyContinue\"");
                    }
                    else
                    {
                        RunCommand("powershell", "-Command \"Remove-MpPreference -ExclusionPath 'C:\\Program Files (x86)\\Steam\\steamapps\\common', 'C:\\Program Files\\Epic Games', 'C:\\Program Files (x86)\\GOG Galaxy' -ErrorAction SilentlyContinue\"");
                    }
                }
            }
            else if (subKey == "ShaderCache")
            {
                if (valueName == "AmdIntelShaderCache")
                {
                    SetAmdIntelShaderCache(enable);
                }
            }
            else if (subKey == "GpuMsi")
            {
                if (valueName == "EnableMsiMode")
                {
                    // The original MSISupported/DevicePriority values are captured by
                    // CreateBackupInternal's OptimizeGpuMsi(false, writer) pass and reverted via the
                    // GPU_MSI restore branch, so this stays fully reversible.
                    OptimizeGpuMsi(enable, null);
                }
            }
        }

        // CPU & Latency commands
        static void ApplySystemCommands(bool enable)
        {
            if (enable)
            {
                // Create and Activate Ultimate Performance Scheme
                RunCommand("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 e9a42b02-d5df-448d-aa00-03f14749eb61");
                RunCommand("powercfg", "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Apply detailed CPU, USB, and PCIe power settings to the active scheme
                RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 SUB_PROCESSOR CPMINCORES 100"); // Unpark cores
                RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ee3-487e-a10f-1b3f3a902462 0"); // USB Selective Suspend Off
                RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 501a4d13-42af-4429-9fd1-a821a10cd04b ee12f27e-b9a3-4b6a-9dfc-ea3a450011c5 0"); // PCIe ASPM Off
                RunCommand("powercfg", "-setacvalueindex e9a42b02-d5df-448d-aa00-03f14749eb61 54533251-82be-4824-96c1-47b60b740d00 5d76a2ca-e8c1-401f-a6cb-a77a63db5774 1"); // CPU C-States (idle disable) = 1
                RunCommand("powercfg", "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Disable Hibernation
                RunCommand("powercfg", "-h off");

                // Apply Network TCP Stack Optimizations
                RunCommand("netsh", "int tcp set heuristics disabled");
                RunCommand("netsh", "int tcp set global rss=enabled");
                RunCommand("netsh", "int tcp set global autotuninglevel=normal");
                RunCommand("netsh", "int tcp set global chimney=disabled");

                // Stop & Disable Services (Reduces background CPU, RAM, & Disk spikes)
                foreach (string svc in managedServices)
                {
                    RunCommand("sc", "config " + svc + " start= disabled");
                    RunCommand("sc", "stop " + svc);
                }

                // Disable Telemetry & Diagnostics Scheduled Tasks (Reduces background CPU/disk spikes)
                foreach (string task in telemetryTasks)
                {
                    string status = GetTaskStatus(task);
                    if (status != "NotFound" && status != "Disabled")
                    {
                        SetTaskState(task, false);
                    }
                }

                // Configure DNS and NetBIOS for all Interfaces
                string ipInterfacesPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
                using (RegistryKey root = Registry.LocalMachine.OpenSubKey(ipInterfacesPath, true))
                {
                    if (root != null)
                    {
                        foreach (string subKeyName in root.GetSubKeyNames())
                        {
                            using (RegistryKey key = root.OpenSubKey(subKeyName, true))
                            {
                                if (key != null) key.SetValue("NameServer", "1.1.1.1,1.0.0.1", RegistryValueKind.String);
                            }
                        }
                    }
                }

                string netbtPath = @"SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces";
                using (RegistryKey root = Registry.LocalMachine.OpenSubKey(netbtPath, true))
                {
                    if (root != null)
                    {
                        foreach (string subKeyName in root.GetSubKeyNames())
                        {
                            using (RegistryKey key = root.OpenSubKey(subKeyName, true))
                            {
                                if (key != null) key.SetValue("NetbiosOptions", 2, RegistryValueKind.DWord);
                            }
                        }
                    }
                }
                
                RunCommand("ipconfig", "/flushdns");

                // Add Windows Defender Game Exclusions to speed up loading
                RunCommand("powershell", "-Command \"Add-MpPreference -ExclusionPath 'C:\\Program Files (x86)\\Steam\\steamapps\\common', 'C:\\Program Files\\Epic Games', 'C:\\Program Files (x86)\\GOG Galaxy' -ErrorAction SilentlyContinue\"");

                // Configure Fixed Pagefile
                SetFixedPagefile();

                // Disable Windows Optional Features (Hyper-V, SMB1, IE, XPS)
                foreach (string feature in optionalFeatures)
                {
                    if (IsFeatureEnabled(feature))
                    {
                        SetFeatureState(feature, false);
                    }
                }

                // Disable HPET and Dynamic Tick (Reduces latency and stabilizes CPU cycle timing)
                RunCommand("bcdedit", "/set useplatformclock no");
                RunCommand("bcdedit", "/set disabledynamictick yes");
            }
            else
            {
                // Restore defaults
                RunCommand("powercfg", "-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 5");
                RunCommand("powercfg", "-setactive SCHEME_CURRENT");

                // Enable Hibernation
                RunCommand("powercfg", "-h on");

                // Restore TCP defaults
                RunCommand("netsh", "int tcp set heuristics enabled");
                RunCommand("netsh", "int tcp set global rss=enabled");
                RunCommand("netsh", "int tcp set global autotuninglevel=normal");
                RunCommand("netsh", "int tcp set global chimney=default");

                // Restore Services
                RunCommand("sc", "config DiagTrack start= auto");
                RunCommand("sc", "start DiagTrack");
                RunCommand("sc", "config dmwappushservice start= demand");
                RunCommand("sc", "start dmwappushservice");
                RunCommand("sc", "config SysMain start= auto");
                RunCommand("sc", "start SysMain");
                RunCommand("sc", "config TrkWks start= auto");
                RunCommand("sc", "start TrkWks");
                RunCommand("sc", "config WSearch start= delayed-auto");
                RunCommand("sc", "start WSearch");
                RunCommand("sc", "config WerSvc start= demand");
                RunCommand("sc", "config Spooler start= auto");
                RunCommand("sc", "start Spooler");
                RunCommand("sc", "config DoSvc start= demand");
                RunCommand("sc", "config RemoteRegistry start= demand");

                // Remove Windows Defender Exclusions
                RunCommand("powershell", "-Command \"Remove-MpPreference -ExclusionPath 'C:\\Program Files (x86)\\Steam\\steamapps\\common', 'C:\\Program Files\\Epic Games', 'C:\\Program Files (x86)\\GOG Galaxy' -ErrorAction SilentlyContinue\"");

                // Restore Automatic Pagefile management
                RunCommand("wmic", string.Format("computersystem where name=\"{0}\" set AutomaticManagedPagefile=True", Environment.MachineName));

                // Delete Custom Power Scheme
                RunCommand("powercfg", "-delete e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Restore HPET and Dynamic Tick
                RunCommand("bcdedit", "/deletevalue useplatformclock");
                RunCommand("bcdedit", "/deletevalue disabledynamictick");
            }
        }

        static void RunCommand(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = fileName;
                start.Arguments = arguments;
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    if (process != null && !process.WaitForExit(60000))
                    {
                        try { process.Kill(); } catch {}
                    }
                }
            }
            catch {}
        }

        static void RestartExplorer()
        {
            try
            {
                foreach (Process process in Process.GetProcessesByName("explorer"))
                {
                    using (process)
                    {
                        try { process.Kill(); process.WaitForExit(3000); } catch {}
                    }
                }
            }
            catch {}
            try { using (Process.Start("explorer.exe")) { } } catch {}
        }

        // GPU Interrupt Optimization (MSI Mode)
        static void OptimizeGpuMsi(bool enable, StreamWriter backupWriter)
        {
            string pciPath = @"SYSTEM\CurrentControlSet\Enum\PCI";
            using (RegistryKey pciRoot = Registry.LocalMachine.OpenSubKey(pciPath, true))
            {
                if (pciRoot == null) return;
                foreach (string deviceKeyName in pciRoot.GetSubKeyNames())
                {
                    using (RegistryKey deviceKey = pciRoot.OpenSubKey(deviceKeyName, true))
                    {
                        if (deviceKey == null) continue;
                        foreach (string instanceKeyName in deviceKey.GetSubKeyNames())
                        {
                            using (RegistryKey instanceKey = deviceKey.OpenSubKey(instanceKeyName, true))
                            {
                                if (instanceKey == null) continue;
                                object classGuid = instanceKey.GetValue("ClassGUID", null);
                                if (classGuid != null && classGuid.ToString().Equals("{4d36e968-e325-11ce-bfc1-08002be10318}", StringComparison.OrdinalIgnoreCase))
                                {
                                    string devParamPath = @"Device Parameters";
                                    using (RegistryKey devParams = instanceKey.OpenSubKey(devParamPath, true))
                                    {
                                        if (devParams == null) continue;
                                        
                                        string msiPath = @"Interrupt Management\MessageSignaledInterruptProperties";
                                        using (RegistryKey msiKey = CreateOrOpenSubKey(devParams, msiPath))
                                        {
                                            if (msiKey != null)
                                            {
                                                if (backupWriter != null)
                                                {
                                                    object msiVal = msiKey.GetValue("MSISupported", null);
                                                    string status = (msiVal != null) ? "Present" : "NotPresent";
                                                    string data = (msiVal != null) ? msiVal.ToString() : "";
                                                    backupWriter.WriteLine(string.Format("GPU_MSI|{0}\\{1}\\{2}\\{3}|MSISupported|DWord|{4}|{5}", 
                                                        pciPath, deviceKeyName, instanceKeyName, devParamPath + "\\" + msiPath, status, data));
                                                }
                                                if (enable) msiKey.SetValue("MSISupported", 1, RegistryValueKind.DWord);
                                            }
                                        }

                                        string affinityPath = @"Interrupt Management\Affinity Policy";
                                        using (RegistryKey affinityKey = CreateOrOpenSubKey(devParams, affinityPath))
                                        {
                                            if (affinityKey != null)
                                            {
                                                if (backupWriter != null)
                                                {
                                                    object priVal = affinityKey.GetValue("DevicePriority", null);
                                                    string status = (priVal != null) ? "Present" : "NotPresent";
                                                    string data = (priVal != null) ? priVal.ToString() : "";
                                                    backupWriter.WriteLine(string.Format("GPU_MSI|{0}\\{1}\\{2}\\{3}|DevicePriority|DWord|{4}|{5}", 
                                                        pciPath, deviceKeyName, instanceKeyName, devParamPath + "\\" + affinityPath, status, data));
                                                }
                                                if (enable) affinityKey.SetValue("DevicePriority", 3, RegistryValueKind.DWord);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Cleaner Operations
        private void CleanDirectoryWithBackup(string path, string backupFolder)
        {
            string[] files;
            try { files = Directory.GetFiles(path); } catch { return; }

            foreach (string file in files)
            {
                try
                {
                    FileInfo fi = new FileInfo(file);
                    long fileSize = fi.Length;

                    string fullPath = Path.GetFullPath(file);
                    string driveLetter = fullPath.Substring(0, 1);
                    string relativePath = fullPath.Substring(3);
                    string destFile = Path.Combine(backupFolder, driveLetter, relativePath);

                    string destDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    File.Copy(file, destFile, true);
                    File.Delete(file);

                    totalBytesCleaned += fileSize;
                    filesDeletedCount++;
                }
                catch { filesSkippedCount++; }
            }

            string[] subDirs;
            try { subDirs = Directory.GetDirectories(path); } catch { return; }
            foreach (string subDir in subDirs)
            {
                if (IsReparsePoint(subDir))
                {
                    try { Directory.Delete(subDir, false); foldersDeletedCount++; }
                    catch { filesSkippedCount++; }
                    continue;
                }
                CleanDirectoryWithBackup(subDir, backupFolder);
                try { Directory.Delete(subDir, true); foldersDeletedCount++; } catch {}
            }
        }

        private void CleanSoftwareDistribution()
        {
            string path = @"C:\Windows\SoftwareDistribution\Download";
            if (!Directory.Exists(path)) return;

            RunCommand("net", "stop wuauserv");
            RunCommand("net", "stop bits");

            try
            {
                string randomName = "Download_Old_" + Guid.NewGuid().ToString().Substring(0, 8);
                string parentDir = @"C:\Windows\SoftwareDistribution";
                string oldPath = Path.Combine(parentDir, randomName);
                Directory.Move(path, oldPath);
                Directory.CreateDirectory(path);

                // Run async command shell to delete in background
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "cmd.exe";
                start.Arguments = string.Format("/c rmdir /s /q \"{0}\"", oldPath);
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                using (Process.Start(start)) { } // background rmdir; dispose the handle, the deletion continues
            }
            catch
            {
                try
                {
                    ProcessStartInfo start = new ProcessStartInfo();
                    start.FileName = "cmd.exe";
                    start.Arguments = string.Format("/c del /f /q /s \"{0}\\*\"", path);
                    start.UseShellExecute = false;
                    start.CreateNoWindow = true;
                    using (Process process = Process.Start(start))
                    {
                        if (process != null && !process.WaitForExit(10000)) { try { process.Kill(); } catch {} }
                    }
                }
                catch {}
            }

            RunCommand("net", "start wuauserv");
            RunCommand("net", "start bits");
        }

        private void EmptyRecycleBin()
        {
            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            }
            catch {}
        }

        private void OptimizeDrives()
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "defrag.exe";
                start.Arguments = "/O";
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    if (process != null && !process.WaitForExit(90000)) { try { process.Kill(); } catch {} }
                }
            }
            catch {}
        }

        static bool IsReparsePoint(string path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                return (di.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }
            catch { return false; }
        }

        // Power Scheme Helpers
        static string GetActivePowerScheme()
        {
            try
            {
                object activeScheme = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes", "ActivePowerScheme", "381b4222-f694-41f0-9685-ff5bb260df2e");
                return (activeScheme != null) ? activeScheme.ToString() : "381b4222-f694-41f0-9685-ff5bb260df2e";
            }
            catch { return "381b4222-f694-41f0-9685-ff5bb260df2e"; }
        }

        static int GetServiceStartupType(string serviceName)
        {
            try
            {
                object startVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\" + serviceName, "Start", 3);
                return (startVal != null) ? Convert.ToInt32(startVal) : 3;
            }
            catch { return 3; }
        }

        // Reads the current AutomaticManagedPagefile flag via wmic. Returns "True"/"False"/"Unknown".
        static string GetAutomaticManagedPagefile()
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "wmic.exe";
                start.Arguments = "computersystem get AutomaticManagedPagefile /value";
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    if (process != null && !process.WaitForExit(8000)) { try { process.Kill(); } catch {} return "Unknown"; }
                    if (output.IndexOf("AutomaticManagedPagefile=TRUE", StringComparison.OrdinalIgnoreCase) >= 0) return "True";
                    if (output.IndexOf("AutomaticManagedPagefile=FALSE", StringComparison.OrdinalIgnoreCase) >= 0) return "False";
                }
            }
            catch {}
            return "Unknown";
        }

        // Scheduled Tasks Helpers
        static string GetTaskStatus(string taskPath)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "schtasks.exe";
                start.Arguments = string.Format("/query /tn \"{0}\" /fo CSV", taskPath);
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                // Do not redirect stderr: it is never read, and an unread redirected pipe can
                // deadlock the child if it fills the buffer. Leaving it un-redirected discards it safely.
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        string[] lines = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length >= 2)
                        {
                            string lastLine = lines[lines.Length - 1];
                            string[] parts = lastLine.Split(',');
                            if (parts.Length >= 3)
                            {
                                return parts[parts.Length - 1].Trim('\"', ' ');
                            }
                        }
                    }
                }
            }
            catch {}
            return "NotFound";
        }

        static void SetTaskState(string taskPath, bool enable)
        {
            try
            {
                RunCommand("schtasks", string.Format("/change /tn \"{0}\" /{1}", taskPath, enable ? "enable" : "disable"));
            }
            catch {}
        }

        // Optional Features Helpers
        static bool IsFeatureEnabled(string featureName)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "dism.exe";
                start.Arguments = string.Format("/online /get-featureinfo /featurename:{0}", featureName);
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(10000);
                    return output.Contains("State : Enabled") || output.Contains("State: Enabled");
                }
            }
            catch {}
            return false;
        }

        // Like IsFeatureEnabled but distinguishes a failed/unreadable query as "Unknown" so the
        // backup never records a real "Enabled" feature as "False" on a transient dism failure.
        static string GetFeatureState(string featureName)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "dism.exe";
                start.Arguments = string.Format("/online /get-featureinfo /featurename:{0}", featureName);
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    if (process != null && !process.WaitForExit(15000)) { try { process.Kill(); } catch {} return "Unknown"; }
                    if (process.ExitCode != 0) return "Unknown";
                    if (output.Contains("State : Enabled") || output.Contains("State: Enabled")) return "True";
                    if (output.Contains("State : Disabled") || output.Contains("State: Disabled")) return "False";
                }
            }
            catch {}
            return "Unknown";
        }

        static void SetFeatureState(string featureName, bool enable)
        {
            try
            {
                string action = enable ? "enable-feature" : "disable-feature";
                string extra = enable ? "/all " : "";
                RunCommand("dism.exe", string.Format("/online /{0} /featurename:{1} {2}/norestart", action, featureName, extra));
            }
            catch {}
        }

        // Startup Manager Helpers
        class StartupEntry
        {
            public string Name;
            public string Command;
            public string LocationType; // RegistryHKCU, RegistryHKLM, FolderUser, FolderCommon
            public string KeyPath; // Registry key path or shortcut path
            public bool IsEnabled;
        }

        static List<StartupEntry> GetStartupEntries()
        {
            List<StartupEntry> entries = new List<StartupEntry>();
            
            // 1. HKCU Run
            try
            {
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (runKey != null)
                    {
                        using (RegistryKey approvedKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", false))
                        {
                            foreach (string name in runKey.GetValueNames())
                            {
                                bool isEnabled = true;
                                if (approvedKey != null)
                                {
                                    byte[] val = approvedKey.GetValue(name) as byte[];
                                    if (val != null && val.Length > 0 && (val[0] & 1) != 0)
                                    {
                                        isEnabled = false;
                                    }
                                }
                                object valData = runKey.GetValue(name);
                                entries.Add(new StartupEntry {
                                    Name = name,
                                    Command = valData != null ? valData.ToString() : "",
                                    LocationType = "RegistryHKCU",
                                    KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
                                    IsEnabled = isEnabled
                                });
                            }
                        }
                    }
                }
            }
            catch {}

            // 2. HKLM Run
            try
            {
                using (RegistryKey runKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (runKey != null)
                    {
                        using (RegistryKey approvedKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", false))
                        {
                            foreach (string name in runKey.GetValueNames())
                            {
                                bool isEnabled = true;
                                if (approvedKey != null)
                                {
                                    byte[] val = approvedKey.GetValue(name) as byte[];
                                    if (val != null && val.Length > 0 && (val[0] & 1) != 0)
                                    {
                                        isEnabled = false;
                                    }
                                }
                                object valData = runKey.GetValue(name);
                                entries.Add(new StartupEntry {
                                    Name = name,
                                    Command = valData != null ? valData.ToString() : "",
                                    LocationType = "RegistryHKLM",
                                    KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run",
                                    IsEnabled = isEnabled
                                });
                            }
                        }
                    }
                }
            }
            catch {}

            // 3. Folder User Startup
            try
            {
                string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (Directory.Exists(userStartup))
                {
                    using (RegistryKey approvedKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder", false))
                    {
                        foreach (string file in Directory.GetFiles(userStartup, "*"))
                        {
                            string fileName = Path.GetFileName(file);
                            bool isEnabled = true;
                            if (approvedKey != null)
                            {
                                byte[] val = approvedKey.GetValue(fileName) as byte[];
                                if (val != null && val.Length > 0 && (val[0] & 1) != 0)
                                {
                                    isEnabled = false;
                                }
                            }
                            entries.Add(new StartupEntry {
                                Name = fileName,
                                Command = file,
                                LocationType = "FolderUser",
                                KeyPath = file,
                                IsEnabled = isEnabled
                            });
                        }
                    }
                }
            }
            catch {}

            // 4. Folder Common Startup
            try
            {
                string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                if (Directory.Exists(commonStartup))
                {
                    using (RegistryKey approvedKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder", false))
                    {
                        foreach (string file in Directory.GetFiles(commonStartup, "*"))
                        {
                            string fileName = Path.GetFileName(file);
                            bool isEnabled = true;
                            if (approvedKey != null)
                            {
                                byte[] val = approvedKey.GetValue(fileName) as byte[];
                                if (val != null && val.Length > 0 && (val[0] & 1) != 0)
                                {
                                    isEnabled = false;
                                }
                            }
                            entries.Add(new StartupEntry {
                                Name = fileName,
                                Command = file,
                                LocationType = "FolderCommon",
                                KeyPath = file,
                                IsEnabled = isEnabled
                            });
                        }
                    }
                }
            }
            catch {}

            return entries;
        }

        static void ToggleStartupEntry(StartupEntry entry)
        {
            try
            {
                byte[] valData = entry.IsEnabled ? 
                    new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } : 
                    new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                if (entry.LocationType.StartsWith("Registry"))
                {
                    RegistryKey baseKey = entry.LocationType == "RegistryHKCU" ? Registry.CurrentUser : Registry.LocalMachine;
                    string approvedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
                    
                    using (RegistryKey key = baseKey.OpenSubKey(approvedPath, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(entry.Name, valData, RegistryValueKind.Binary);
                        }
                    }
                }
                else if (entry.LocationType.StartsWith("Folder"))
                {
                    RegistryKey baseKey = entry.LocationType == "FolderUser" ? Registry.CurrentUser : Registry.LocalMachine;
                    string approvedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";
                    
                    using (RegistryKey key = baseKey.OpenSubKey(approvedPath, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(entry.Name, valData, RegistryValueKind.Binary);
                        }
                    }
                }
            }
            catch {}
        }

        // Game Priority Helpers
        static void SetGamePriorityProfile(string exeName, int cpuPriority, int ioPriority, bool excludeCore0)
        {
            string keyPath = string.Format(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{0}\PerfOptions", exeName);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\" + keyPath, "CpuPriorityClass", cpuPriority, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\" + keyPath, "IoPriority", ioPriority, RegistryValueKind.DWord);
            if (excludeCore0)
            {
                long mask = (1L << Environment.ProcessorCount) - 2;
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\" + keyPath, "CpuAffinityMask", mask, RegistryValueKind.QWord);
            }
            else
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, true))
                {
                    if (key != null) key.DeleteValue("CpuAffinityMask", false);
                }
            }
        }

        static void RemoveGamePriorityProfile(string exeName)
        {
            string keyPath = string.Format(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{0}", exeName);
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, true))
            {
                if (key != null)
                {
                    key.DeleteSubKeyTree("PerfOptions", false);
                    if (key.SubKeyCount == 0 && key.ValueCount == 0)
                    {
                        using (RegistryKey parentKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true))
                        {
                            if (parentKey != null) parentKey.DeleteSubKey(exeName, false);
                        }
                    }
                }
            }
        }

        // RAM standby list purging helpers
        static bool EnablePrivilege(string privilegeName)
        {
            IntPtr hToken = IntPtr.Zero;
            try
            {
                LUID luid;
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken)) return false;
                if (!LookupPrivilegeValue(null, privilegeName, out luid)) return false;

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Privilege.Luid = luid;
                tp.Privilege.Attributes = SE_PRIVILEGE_ENABLED;

                return AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch { return false; }
            finally
            {
                // Always release the process token handle (was leaking on every purge).
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
            }
        }

        static void PurgeStandbyList()
        {
            try
            {
                EnablePrivilege("SeProfileSingleProcessPrivilege");

                int command = 4; // MemoryPurgeStandbyList
                IntPtr pCommand = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    Marshal.WriteInt32(pCommand, command);
                    Program.NtSetSystemInformation(80, pCommand, sizeof(int));
                }
                finally { Marshal.FreeHGlobal(pCommand); }

                int cmdLow = 5; // MemoryPurgeLowPriorityStandbyList
                IntPtr pCmdLow = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    Marshal.WriteInt32(pCmdLow, cmdLow);
                    Program.NtSetSystemInformation(80, pCmdLow, sizeof(int));
                }
                finally { Marshal.FreeHGlobal(pCmdLow); }
            }
            catch {}
        }

        // Configure Pagefile fixed size based on C: drive free space
        static void SetFixedPagefile()
        {
            try
            {
                DriveInfo cDrive = new DriveInfo("C");
                long freeSpaceGB = cDrive.AvailableFreeSpace / (1024 * 1024 * 1024);
                
                // Disable automatic management first
                RunCommand("wmic", string.Format("computersystem where name=\"{0}\" set AutomaticManagedPagefile=False", Environment.MachineName));
                
                // Ensure pagefilesetting exists
                RunCommand("wmic", "pagefilesetting create name=\"C:\\\\pagefile.sys\"");

                if (freeSpaceGB > 25)
                {
                    RunCommand("wmic", "pagefilesetting where name=\"C:\\\\pagefile.sys\" set InitialSize=16384,MaximumSize=16384");
                }
                else if (freeSpaceGB > 15)
                {
                    RunCommand("wmic", "pagefilesetting where name=\"C:\\\\pagefile.sys\" set InitialSize=8192,MaximumSize=8192");
                }
            }
            catch {}
        }

        static RegistryKey CreateOrOpenSubKey(RegistryKey parent, string subKeyPath)
        {
            try { return parent.CreateSubKey(subKeyPath); }
            catch
            {
                try { return parent.OpenSubKey(subKeyPath, true); }
                catch { return null; }
            }
        }
    }
}
