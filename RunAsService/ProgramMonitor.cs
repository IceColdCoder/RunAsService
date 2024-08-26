using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32.TaskScheduler;

namespace RunAsService
{
    public class ProgramMonitor : IProgramMonitor
    {
        public string ProcessPath;
        public string LaunchPath;
        public string WorkingDir;
        public string ProgramArgs;
        private System.Diagnostics.Process _prog;
        private string _consolePath;
        private string _exeName;
        private StreamReader _outputStream;
        private Microsoft.Win32.TaskScheduler.Task _windowsTask = null;

        public ProgramMonitor(string processPath, string launchPath, string workingDir, string progArgs)
        {
            ProcessPath = processPath;
            LaunchPath = string.IsNullOrEmpty(launchPath) ? processPath : launchPath;
            WorkingDir = workingDir;
            ProgramArgs = progArgs;
            _consolePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileNameWithoutExtension(processPath), "Console.txt");
        }

        public void StartProg()
        {
            var taskName = "Test";
            var workingDir = string.IsNullOrEmpty(WorkingDir) ? null : WorkingDir;
            var launchPath = string.IsNullOrEmpty(LaunchPath) ? ProcessPath : LaunchPath;
  
            using (_windowsTask = TaskService.Instance.AddTask(taskName, new RegistrationTrigger(),
                    new ExecAction(launchPath, ProgramArgs, workingDir))) {}
            TaskFolder taskFolder = TaskService.Instance.GetFolder("\\");
            taskFolder.DeleteTask(taskName);
        }

        //public void StartProg()
        //{

        //    var processInfo = new System.Diagnostics.ProcessStartInfo
        //    {
        //        FileName = ProcessPath,
        //        CreateNoWindow = false,
        //        RedirectStandardError = true,
        //        RedirectStandardInput = true,
        //        RedirectStandardOutput = true,
        //        UseShellExecute = false,
        //        WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal,
        //        Arguments = !String.IsNullOrEmpty(ProgramArgs) ? ProgramArgs : string.Empty
        //    };

        //    try
        //    {
        //        _prog = System.Diagnostics.Process.Start(processInfo);
        //        _outputStream = _prog.StandardOutput;
        //    }
        //    catch (ObjectDisposedException ex)
        //    {
        //        WinLog.Log.WriteError($"{_prog} threw exception at process start: {ex}");
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        WinLog.Log.WriteError($"{_prog} threw exception at process start: {ex}");
        //    }
        //    catch (System.ComponentModel.Win32Exception ex)
        //    {
        //        WinLog.Log.WriteError($"{_prog} threw exception at process start: {ex}");
        //    }
        //    catch (System.IO.FileNotFoundException ex)
        //    {
        //        WinLog.Log.WriteError($"{_prog} threw exception at process start: {ex}");
        //    }
        //    finally
        //    {
        //        if(_prog != null)
        //        {
        //            _prog.Refresh();
        //            if (_prog.HasExited)
        //            {
        //                WinLog.Log.WriteError($"{_prog.ToString()} has terminated at process start with exit code {_prog.ExitCode}.");
        //            }
        //        }
        //    }
        //}

        public bool CloseProg()
        {
            var prog = GetProcess();
            if (prog == null) return false;
            try
            {
                return prog.CloseMainWindow();
            }
            catch (PlatformNotSupportedException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process close: {ex}");
            }
            catch (InvalidOperationException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process close: {ex}");
            }
            return false;            
        }

        public void KillProg()
        {
            var prog = GetProcess();
            try
            {
                prog?.Kill();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process kill: {ex}");
            }
            catch (NotSupportedException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process kill: {ex}");
            }
            catch (InvalidOperationException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process kill: {ex}");
            }
        }

        public bool ProgResponding()
        {
            var prog = GetProcess();
            if (prog == null) return false;
            try
            {
                return prog.Responding;
            }
            catch (PlatformNotSupportedException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process responding check: {ex}");
            }
            catch (InvalidOperationException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process responding check: {ex}");
            }
            catch (NotSupportedException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process responding check: {ex}");
            }
            return false;
        }

        public bool ProgRunning()
        {
            var prog = GetProcess();
            if (prog == null) return false;
            try
            {
                return !prog.HasExited;
            }
            catch (InvalidOperationException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process exit check: {ex}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process exit check: {ex}");
            }
            catch (NotSupportedException ex)
            {
                WinLog.Log.WriteError($"{_prog} threw exception at process exit check: {ex}");
            }
            return false;
        }

        public void PollConsoleToFile()
        {
            if (_prog != null)
            {
                try
                {
                    var dirPath = Path.GetDirectoryName(_consolePath);
                    System.IO.Directory.CreateDirectory(dirPath);

                    //Console.WriteLine(_outputStream.ReadLine());
                    var consoleOutput = _outputStream.ReadToEnd();
                    File.AppendAllText(_consolePath, consoleOutput);
                }
                catch (OutOfMemoryException ex)
                {
                    WinLog.Log.WriteError($"{_prog} threw exception at console poll: {ex}");
                }
                catch (NotSupportedException ex)
                {
                    WinLog.Log.WriteError($"{_prog} threw exception at console poll: {ex}");
                }
                catch (Exception ex) when (ex is System.IO.IOException || ex is PathTooLongException || ex is DirectoryNotFoundException)
                {
                    WinLog.Log.WriteError($"{_prog} threw exception at console poll: {ex}");
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException)
                {
                    WinLog.Log.WriteError($"{_prog} threw exception at console poll: {ex}");
                }
                catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
                {
                    WinLog.Log.WriteError($"{_prog} threw exception at console poll: {ex}");
                }
            }
        }

        private System.Diagnostics.Process GetProcess()
        {
            System.Diagnostics.Process prog = _prog;
            if (prog == null)
            {
                var procList = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ProcessPath));
                if (procList.Length > 0) prog = procList.First();
            }
            return prog;
        }

        private void ClearConsole()
        {
            if (File.Exists(_consolePath))
            {
                try
                {
                    File.Delete(_consolePath);
                }
                catch (Exception ex)
                {
                    WinLog.Log.WriteError($"{_prog} threw exception at console delete: {ex}");
                }
            }
        }
    }
}
