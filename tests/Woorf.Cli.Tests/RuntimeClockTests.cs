using System.Reflection;
using Woorf.Engine;
using Woorf.Engine.Scheduling;

static class RuntimeClockTests
{
    public static int Run()
    {
        // Missing timer drainage or using wall time instead of the supplied clock loses these prints.
        try
        {
            var method = typeof(LuauRuntime).GetMethod("Execute", [typeof(string), typeof(string), typeof(IRuntimeClock)]);
            if (method is null) throw new Exception("Runtime has no injectable scheduler clock.");
            var clock = new ControlledClock(10);
            var original = Console.Out;
            using var output = new StringWriter();
            try
            {
                Console.SetOut(output);
                var status = method.Invoke(null, ["task.delay(2, function() print('two') end); task.wait(1); print('one')", "clock-test.luau", clock]);
                if (!Equals(status, 0)) throw new Exception("Scheduled execution failed.");
            }
            finally { Console.SetOut(original); }
            if (output.ToString().Replace("\r\n", "\n") != "one\ntwo\n" || clock.ElapsedSeconds != 12 || clock.Sleeps != 2)
                throw new Exception($"Unexpected clock execution: {output}, now={clock.ElapsedSeconds}, sleeps={clock.Sleeps}");
            Console.WriteLine("PASS advances scheduler with a controlled clock");
            return RunOversleepCase(method);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL advances scheduler with a controlled clock: " + error.Message);
            return 1;
        }
    }

    private static int RunOversleepCase(MethodInfo method)
    {
        var clock = new ControlledClock(0, 2);
        var original = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            method.Invoke(null, ["task.delay(2, function() print('two') end); task.delay(1, function() print('one') end)", "oversleep.luau", clock]);
        }
        finally { Console.SetOut(original); }
        if (output.ToString().Replace("\r\n", "\n") != "one\ntwo\n")
            throw new Exception("Overdue tasks did not execute in deadline order: " + output);
        Console.WriteLine("PASS orders overdue timers by deadline");
        return 0;
    }

    private sealed class ControlledClock(double initialSeconds, double oversleepSeconds = 0) : IRuntimeClock
    {
        public double ElapsedSeconds { get; private set; } = initialSeconds;
        public int Sleeps { get; private set; }
        public void Sleep(TimeSpan duration) { ElapsedSeconds += duration.TotalSeconds + oversleepSeconds; Sleeps++; }
    }
}
