using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RunAsService
{
    public partial class RunAsService : ServiceBase
    {
        private bool _run;
        private MonitorSpinner _monitorSpinner;
        private static System.Threading.CancellationToken _cancelToken = new System.Threading.CancellationToken();
        private readonly string _defualtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Targets.txt");

        public RunAsService()
        {
            InitializeComponent();
        }

        public void Start(string[] args)
        {
            _monitorSpinner = new MonitorSpinner();
            _monitorSpinner.Init(_defualtPath);
            _monitorSpinner.StartAsync(_cancelToken);
        }

        protected override void OnStart(string[] args)
        {
            Start(args);
        }

        protected override void OnStop()
        {
            Task.WaitAll(_monitorSpinner.StopAsync(_cancelToken));
        }
    }
}
