namespace ConsoleApp1.Services;

public class PMSessionManger(ProcessTreeMonitor processMonitor): IDisposable
{

    public Action<int>? onSessionRotation;

    private readonly System.Timers.Timer _timer = new(5_000)
    {
        AutoReset = true,
    };
    
    public void InitSessionManagement()
    {
        _timer.Elapsed += (sender, e) =>
        {
            processMonitor.CaptureCurrentState();
            onSessionRotation?.Invoke(e.SignalTime.Microsecond);
        };

        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
