using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace MiToolz
{
    internal static class ServiceManager
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetTimerResolution(int desiredResolution, bool setResolution, out int currentResolution);
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryTimerResolution(out int minimumResolution, out int maximumResolution, out int actualResolution);

        private static readonly string ServicePath = Directory.GetCurrentDirectory() + @"\bin\MiToolzTimerService.exe";
        public static readonly ServiceController MiToolzService = new ServiceController("MiToolzTimerService");

        public static bool ServiceExists()
        {
            return ServiceController.GetServices().Any(serviceController => serviceController.ServiceName.Equals("MiToolzTimerService"));
        }

        private static bool ServiceRunning()
        {
            bool isRunning;
            switch (MiToolzService.Status)
            {
                case ServiceControllerStatus.Running:
                    isRunning = true;
                    break;
                default:
                    isRunning = false;
                    break;
            }
            return isRunning;
        }

        public static int CurrentTimerRes()
        {
            NtQueryTimerResolution(out _, out _, out var currentResolution);
            return currentResolution;
        }

        public static void InstallService()
        {
            if (ServiceExists()) return;
            var installServiceProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Microsoft.NET\Framework\v4.0.30319\installutil.exe",
                    Arguments = "\"" + ServicePath + "\"",
                    RedirectStandardError = false,
                    RedirectStandardOutput = false
                }
            };
            installServiceProcess.Start();
            installServiceProcess.WaitForExit(500);
            StartStopService();
        }

        public static void UninstallService()
        {
            if (!ServiceExists()) return;
            StartStopService();
            var uninstallServiceProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Microsoft.NET\Framework\v4.0.30319\installutil.exe",
                    Arguments = "/u \"" + ServicePath + "\"",
                    RedirectStandardError = false,
                    RedirectStandardOutput = false
                }
            };
            uninstallServiceProcess.Start();
            uninstallServiceProcess.WaitForExit(500);
        }

        public static void StartStopService()
        {
            if (!ServiceExists()) return;

            if (!ServiceRunning())
            {
                MiToolzService.Start();
            }

            if (ServiceRunning())
            {
                MiToolzService.Stop();
            }
        }
    }
}
