using System.Diagnostics;

// These cases catch skipped execution, lost output, Lua substitution, and swallowed errors.
var cliPath = Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll");
var cases = new (string Name, string? Source, int Exit, string Output, string Error)[]
{
    ("prints hello world", "print(\"hello world\")", 0, "hello world\n", ""),
    ("compiles Luau syntax", "local n: number = 0\nfor i = 1, 3 do n += i end\nprint(n)", 0, "6\n", ""),
    ("reports syntax errors", "local =", 1, "", ":1:"),
    ("reports runtime errors", "error('intentional failure')", 1, "", "intentional failure"),
    ("requires a file argument", null, 2, "", "Usage:"),
    ("reports missing files", "MISSING_FILE", 1, "", "missing.luau"),
    ("preserves UTF-8 output", "print('cześć 世界')", 0, "cześć 世界\n", ""),
}.Concat(Vector3Cases.GetCases()).Concat(CFrameCases.GetCases()).Concat(PartCases.GetCases()).Concat(PartCFrameCases.GetCases())
    .Concat(HierarchyCases.GetCases()).Concat(SignalCases.GetCases())
    .Concat(PartPivotCases.GetCases()).Concat(ModelPivotCases.GetCases())
    .Concat(ModelBoundsCases.GetCases())
    .Concat(InstanceEventCases.GetCases()).Concat(SchedulerCases.GetCases()).Concat(BackendApiCases.GetCases()).ToArray();
var failedCount = 0;
foreach (var test in cases)
{
    var scriptPath = Path.Combine(Path.GetTempPath(), $"woorf-{Guid.NewGuid():N}.luau");
    try
    {
        if (test.Source is not null && test.Source != "MISSING_FILE")
            File.WriteAllText(scriptPath, test.Source);
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(cliPath);
        if (test.Source is not null)
            start.ArgumentList.Add(test.Source == "MISSING_FILE" ? "missing.luau" : scriptPath);
        using var process = Process.Start(start)!;
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw; }
        var output = (await outputTask).Replace("\r\n", "\n");
        var error = await errorTask;
        var isExpectedError = test.Error.Length == 0 ? error.Length == 0 : error.Contains(test.Error);
        if (process.ExitCode != test.Exit || output != test.Output || !isExpectedError)
            throw new Exception($"exit={process.ExitCode}, stdout={output}, stderr={error}");
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception error)
    {
        failedCount++;
        Console.Error.WriteLine($"FAIL {test.Name}: {error.Message}");
    }
    finally { File.Delete(scriptPath); }
}
failedCount += await NetworkProjectTests.RunAsync(cliPath);
failedCount += await ProjectTests.RunAsync(cliPath);
failedCount += await ProjectValidationTests.RunAsync(cliPath);
failedCount += RuntimeClockTests.Run();
failedCount += NetworkLifecycleTests.Run();
failedCount += await SocketTransportTests.RunAsync(cliPath);
failedCount += await SocketBackpressureTests.RunAsync(cliPath);
failedCount += await SocketMetricsTests.RunAsync();
failedCount += DirtyReplicationTests.Run();
failedCount += await BackendPersistenceTests.RunAsync(cliPath);
return failedCount == 0 ? 0 : 1;
