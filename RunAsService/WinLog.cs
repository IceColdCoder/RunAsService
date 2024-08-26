using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WinLog;

namespace RunAsService
{
    class WinLog
    {
        public static WinLog Log = new WinLog();
        private WindowsLogAdapter _winLogAdapter = null;

        public WinLog()
        {
            _winLogAdapter = new WindowsLogAdapter("RunAsService");
            _winLogAdapter.Init();
        }

        public void WriteLog(string logMsg)
        {
            string eventMsg = ($"{System.DateTime.Now} -> INFO: {logMsg}");
            _winLogAdapter.WriteEntry(eventMsg);
            System.Diagnostics.Debug.WriteLine(eventMsg);
        }

        public void WriteError(string errMsg)
        {
            string eventMsg = ($"{System.DateTime.Now} -> ERROR: {errMsg}");
            _winLogAdapter.WriteEntry(eventMsg);
            System.Diagnostics.Debug.WriteLine(eventMsg);
        }
    }
}
