using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunAsService
{
    public static class StringExtensions
    {
        public static string TrimFirstOccuranceBothSides(this string self, char c)
        {
            if (self.Length == 0) return string.Empty;
            var first = self.IndexOf(c);
            var last = self.LastIndexOf(c);
            StringBuilder sb = new StringBuilder(self);
            sb.Remove(last, 1);
            if(first != last) sb.Remove(first, 1);
            return sb.ToString();
        }
    }

    public class MonitorSpinner : BackgroundService
    {
        public enum IType : int
        {
            Null = -1,
            ExecPath = 0,
            LaunchPath = 1,
            WorkingDir = 2,
            ProgArgs = 3
        }

        public class ITypePair
        {
            public IType IType { get; private set; }
            public int StartIdx { get; private set; }
            public ITypePair(IType iType, int startIdx)
            {
                IType = iType;
                StartIdx = startIdx;
            }
        }

        private static string[] _paramStrs = { "execpath", "launchpath", "workingdir", "progargs" };
        private static int _paramStrsMaxLen = _paramStrs.Aggregate(0, (seed, f) => (f?.Length ?? 0) > seed ? f.Length : seed);

        private List<ProgramMonitor> _progMonitors;
        public MonitorSpinner() 
        {
            _progMonitors = new List<ProgramMonitor>();
        }

        public void Init(string fPath)
        {
            LoadConfig(fPath, out string[] progPaths, out string[] launchPaths, out string[] workingDirs, out string[] programArgs);
            for (int i = 0; i < progPaths.Length; i++)
            {
                var progMonitor = new ProgramMonitor(progPaths[i], launchPaths[i], workingDirs[i], programArgs[i]);
                _progMonitors.Add(progMonitor);
            }
        }

        public bool LoadConfig(string fPath, out string[] progPaths, out string[] launchPaths, out string[] workingDirs, out string[] programArgs)
        {
            progPaths = launchPaths = workingDirs = programArgs = null;
            if (ReadConfigFile(fPath, out IEnumerable<string> lines))
                return ParseConfigFile(lines, out progPaths, out launchPaths, out workingDirs, out programArgs);
            return false;
        }

        private bool ReadConfigFile(string fPath, out IEnumerable<string> lines)
        {
            bool readSuccessful = false;
            lines = null;
            try
            {
                lines = File.ReadLines(fPath);
                readSuccessful = true;
            }
            catch (ArgumentException ex)
            {
                WinLog.Log.WriteError($"Failed to read config file with error {ex}");
            }
            catch (DirectoryNotFoundException ex)
            {
                WinLog.Log.WriteError($"Failed to read config file with error {ex}");
            }
            catch (FileNotFoundException ex)
            {
                WinLog.Log.WriteError($"Failed to read config file with error {ex}");
            }
            catch (IOException ex)
            {
                WinLog.Log.WriteError($"Failed to read config file with error {ex}");
            }
            catch (System.Security.SecurityException ex)
            {
                WinLog.Log.WriteError($"Failed to read config file with error {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                WinLog.Log.WriteError($"Failed to read config file with error {ex}");
            }
            return readSuccessful;
        }

        private bool ParseConfigFile(IEnumerable<string> lines, out string[] outPathArray, out string[] outLaunchPathArray, out string[] outWorkingDirArray, out string[] outArgArray)
        {
            List<string> execPaths = new List<string>();
            List<string> launchPaths = new List<string>();
            List<string> workingDirs = new List<string>();
            List<string> appArgs = new List<string>();

            Queue<ITypePair> iList = new Queue<ITypePair>();
            const int kNotFound = -1;
            if (lines != null)
            {
                foreach (var iLine in lines)
                {
                    //First section locates the parameter declrations. eg execpath=.
                    var line = iLine.Trim();
                    if (line.StartsWith("#")) continue;

                    var maxLength = 0;
                    foreach (var param in _paramStrs)
                        if (param.Length > maxLength) maxLength = param.Length;

                    var startIdx = 0;
                    var len = Math.Max(line.Length, 0);
                    var eqIdx = line.IndexOf('=', startIdx);//Break up the line by the '=' sign. Not all '=' signs will be parameters.
                    do
                    {
                        var walkBackIdx = eqIdx - _paramStrsMaxLen;//The maximum count of characters we need to walk back from the '=' sign.
                        walkBackIdx = walkBackIdx > 0 ? walkBackIdx : 0;
                        var s = line.Substring(walkBackIdx, _paramStrsMaxLen).ToLowerInvariant();//Extract the substring before the '=' sign.
                        for (int i = 0; i < _paramStrs.Length; i++)
                        {
                            if (s.Contains(_paramStrs[i]))
                            {
                                IType iType = IType.Null;
                                switch (i)
                                {
                                    case 0:
                                        iType = IType.ExecPath;
                                        break;
                                    case 1:
                                        iType = IType.LaunchPath;
                                        break;
                                    case 2:
                                        iType = IType.WorkingDir;
                                        break;
                                    case 3:
                                        iType = IType.ProgArgs;
                                        break;
                                    default:
                                        break;
                                }
                                if (iType != IType.Null)
                                {
                                    iList.Enqueue(new ITypePair(iType, walkBackIdx));
                                    break;
                                }
                            }
                        }
                        startIdx = eqIdx + 1;//Shift the search window beyond the first parameter.
                        eqIdx = line.IndexOf('=', startIdx);//Get the next '='.
                    } while (startIdx < len && eqIdx != kNotFound);

                    System.Diagnostics.Debug.WriteLine("[" + string.Join(", ", iList.Select(s => $"({s.IType},{s.StartIdx})")) + "]");

                    var execPath = string.Empty;
                    var launchPath = string.Empty;
                    var workingDir = string.Empty;
                    var progArgs = string.Empty;

                    //The second section extracts the string declared by the parameter.
                    ITypePair first = null;
                    ITypePair second = null;
                    while (iList.Count > 0 || second != null)
                    {
                        first = second ?? iList.Dequeue();//In order to get the bounds for the string to extract we need to know where the next parameter starts.
                        second = iList.Count > 0 ? iList.Dequeue() : null;
                        var paramIdx = first.StartIdx + _paramStrs[(int)first.IType].Length + 1;//Index of the parameter in the string.
                        var strLen = second != null ? second.StartIdx - paramIdx : line.Length - paramIdx;//Length of the parameter string.
                        //Extract the string and clean up the quotation marks. 
                        //The first set of quotations should always be ours and not something in the parameter.
                        var paramStr = line.Substring(paramIdx, strLen).TrimFirstOccuranceBothSides('"');

                        switch (first.IType)
                        {
                            case IType.ExecPath:
                                execPath = paramStr;
                                break;
                            case IType.LaunchPath:
                                launchPath = paramStr;
                                break;
                            case IType.WorkingDir:
                                workingDir = paramStr;
                                break;
                            case IType.ProgArgs:
                                progArgs = paramStr;
                                break;
                            default:
                                break;
                        }
                    }
#if DEBUG
                    System.Diagnostics.Debug.Write($"Entry:" +
                        $"{Environment.NewLine}\tExecPath: {execPath}" +
                        $"{Environment.NewLine}\tLaunchPath: {launchPath}" +
                        $"{Environment.NewLine}\tWorkingDir: {workingDir}" +
                        $"{Environment.NewLine}\tProgArgs: {progArgs}");
#endif
                    execPaths.Add(execPath);
                    launchPaths.Add(launchPath);
                    workingDirs.Add(workingDir);
                    appArgs.Add(progArgs);
                }
            }

            outPathArray = execPaths.ToArray();
            outLaunchPathArray = launchPaths.ToArray();
            outWorkingDirArray = workingDirs.ToArray();
            outArgArray = appArgs.ToArray();
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"\"Paths: {string.Join(", ", outPathArray)}\"");
            System.Diagnostics.Debug.WriteLine($"\"LaunchPaths: {string.Join(", ", outLaunchPathArray)}\"");
            System.Diagnostics.Debug.WriteLine($"\"WorkingDirs: {string.Join(", ", outWorkingDirArray)}\"");
            System.Diagnostics.Debug.WriteLine($"\"Args: {string.Join(", ", outArgArray)}\"");
#endif
            return outPathArray.Length > 0;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            WinLog.Log.WriteLog($"RunAsService started monitoring programs.");
            await Spin(stoppingToken);
        }

        private async Task<int> Spin(CancellationToken stoppingToken)
        {
            int sleepTime = 10;
#if DEBUG
            sleepTime = 1;
#endif
            while (!stoppingToken.IsCancellationRequested)
            {
                CheckProgStates();

                Console.WriteLine($"Spin sleeping for {sleepTime} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(sleepTime));
            }
            return await Task.FromResult(0);
        }

        private void CheckProgStates()
        {
            foreach(var prog in _progMonitors)
            {
                Console.WriteLine($"Checking status of program: {Path.GetFileName(prog.ProcessPath)}...");

                if(!prog.ProgRunning())
                {
                    prog.StartProg();
                    if (!prog.ProgRunning())
                    {
                        WinLog.Log.WriteError($"Failed to (re)start program {prog.ProcessPath} after it was in a closed state.");
                    }
                }
                else if(!prog.ProgResponding())
                {
                    prog.KillProg();
                    prog.StartProg();
                    if (!prog.ProgResponding())
                    {
                        WinLog.Log.WriteError($"Failed to restart program {prog.ProcessPath} after it was in a hung state.");
                    }
                }
                else
                {
                    prog.PollConsoleToFile();
                }
            }
        }

        public override async Task StartAsync(CancellationToken stoppingToken)
        {
            await base.StartAsync(stoppingToken);
            WinLog.Log.WriteLog($"RunAsService has started.");
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
            WinLog.Log.WriteLog($"RunAsService has stopped.");
        }
    }
}
