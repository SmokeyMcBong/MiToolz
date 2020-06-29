using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace MiToolzTimerService
{
    public partial class MiToolzTimerService : ServiceBase
    {
        public MiToolzTimerService()
        {
            InitializeComponent();
            ServiceName = "MiToolzTimerService";
            CanStop = true;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetTimerResolution(int desiredResolution, bool setResolution, out int currentResolution);
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryTimerResolution(out int minimumResolution, out int maximumResolution, out int actualResolution);

        private static int CurrentTimerRes()
        {
            NtQueryTimerResolution(out _, out _, out var currentResolution);
            return currentResolution;
        }

        private static int MaximumTimerRes()
        {
            NtQueryTimerResolution(out _, out var maximumResolution, out _);
            return maximumResolution;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            if (CurrentTimerRes().Equals(MaximumTimerRes())) return;
            var desiredResolution = MaximumTimerRes();
            NtSetTimerResolution(desiredResolution, true, out _);
        }

        protected override void OnStop()
        {
        }
    }
}
