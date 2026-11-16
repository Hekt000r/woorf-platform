using Woorf.Engine;
using Woorf.Engine.Scheduling;

static class PhysicsClockTests
{
    internal static int Run()
    {
        try
        {
            const string setup = "workspace.Gravity=10; local p=Instance.new('Part',workspace); p.CanCollide=false; p.Position=Vector3.new(0,10,0); ";
            var single = Execute(setup + "task.wait(1); print(p.Position.Y, p.AssemblyLinearVelocity.Y)");
            var divided = Execute(setup + "for i=1,60 do task.wait(1/60) end; print(p.Position.Y, p.AssemblyLinearVelocity.Y)");
            var first = single.Split('\t').Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var second = divided.Split('\t').Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            if (Math.Abs(first[0] - 5) > 0.06 || Math.Abs(first[1] + 10) > 0.001
                || Math.Abs(first[0] - second[0]) > 0.0001 || Math.Abs(first[1] - second[1]) > 0.0001)
                throw new Exception("Physics depends on scheduler partition: " + single + " / " + divided);
            Console.WriteLine("PASS physics uses fixed steps independent of scheduler timer partition");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL fixed-step physics: " + error.Message); return 1; }
    }
    private static string Execute(string source)
    {
        using var output = new StringWriter(); using var error = new StringWriter();
        var previous = Console.Out; var previousError = Console.Error;
        try
        {
            Console.SetOut(output); Console.SetError(error);
            if (LuauRuntime.Execute(source, "physics-clock.luau", new Clock()) != 0 || error.ToString() != "")
                throw new Exception(error.ToString());
        }
        finally { Console.SetOut(previous); Console.SetError(previousError); }
        return output.ToString().Trim();
    }
    private sealed class Clock : IRuntimeClock
    {
        public double ElapsedSeconds { get; private set; }
        private int sleeps;
        public void Sleep(TimeSpan duration)
        {
            if (++sleeps > 2000) throw new Exception("Clock failed to make progress at a sub-tick deadline.");
            ElapsedSeconds += duration.TotalSeconds;
        }
    }
}
