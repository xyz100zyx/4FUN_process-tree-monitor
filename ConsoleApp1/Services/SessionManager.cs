using System.Timers;

namespace ConsoleApp1.Services;

public class SessionManager {

    private System.Timers.Timer _timer = new();

    public SessionManager(double sessionInterval, ElapsedEventHandler handler)
    {
        _timer.Interval = sessionInterval;
        _timer.Elapsed += handler;
        _timer.Start();
    }
}