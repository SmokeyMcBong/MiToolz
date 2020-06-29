using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MiToolzTimerService
{
    [RunInstallerAttribute(true)]
    public partial class MiToolzTimerServiceInstaller : Installer
    {
        public MiToolzTimerServiceInstaller()
        {
            InitializeComponent();
            serviceInstaller.Description = "MiToolz Timer Service";
            serviceInstaller.DisplayName = "MiToolz Timer Service";
            serviceInstaller.ServiceName = "MiToolzTimerService";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
        }
    }
}
