using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinLog
{
    public class WindowsLogAdapter
    {
        private string _eventSource;
        private string _appName;
        private System.Diagnostics.EventLog _winLog = null;

        public WindowsLogAdapter(string appName)
        {
            _appName = appName;
        }

        public void Init()
        {
            _eventSource = CreateEventSource(_appName);
            _winLog = new System.Diagnostics.EventLog()
            {
                Source = _eventSource
            };
        }

        private string CreateEventSource(string appName)
        {
            string eventSource = appName;
            try
            {
                if (!System.Diagnostics.EventLog.SourceExists(appName))
                {
                    System.Diagnostics.EventLog.CreateEventSource(eventSource, appName);
                }
            }
            catch (System.Security.SecurityException)
            {
                eventSource = "Application";
            }
            return eventSource;
        }

        public void WriteEntry(string logMsg)
        {
            try
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(logMsg);
#endif
                _winLog?.WriteEntry(logMsg);
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}
