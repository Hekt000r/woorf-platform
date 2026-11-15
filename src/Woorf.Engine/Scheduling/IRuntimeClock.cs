namespace Woorf.Engine.Scheduling;

public interface IRuntimeClock
{
    double ElapsedSeconds { get; }
    void Sleep(TimeSpan duration);
}
