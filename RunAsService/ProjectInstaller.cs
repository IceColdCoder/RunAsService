using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace RunAsService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            //
            ServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;

            ServiceInstaller.DisplayName = "RunAsService";
            ServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        }

        private void ServiceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            using (System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(ServiceInstaller.ServiceName))
            {
                sc.Start();
            }
        }
    }
}
