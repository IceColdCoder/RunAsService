namespace RunAsService
{
    public interface IProgramMonitor
    {
        bool CloseProg();
        void KillProg();
        void PollConsoleToFile();
        bool ProgResponding();
        bool ProgRunning();
        void StartProg();
    }
}