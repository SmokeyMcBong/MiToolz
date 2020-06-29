using System.ServiceProcess;

namespace MiToolzTimerService
{
    internal static class Service
    {
        private static void Main()
        {
            var service = new MiToolzTimerService();
            ServiceBase.Run(service);
        }
    }
}
