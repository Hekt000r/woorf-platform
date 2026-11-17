using System.Diagnostics;

if (args.Length == 1 && args[0] == "--obby") return ObbyTests.Run();

if (args.Length == 1 && args[0] == "--character-controller") return CharacterControllerTests.Run();
if (args.Length == 1 && args[0] == "--character-lifecycle") return CharacterLifecycleTests.Run();
if (args.Length == 1 && args[0] == "--character-view") return CharacterViewTests.Run();
if (args.Length == 1 && args[0] == "--character-input") return CharacterViewTests.RunInput();
if (args.Length == 1 && args[0] == "--character-replication") return CharacterReplicationTests.Run();
if (args.Length == 1 && args[0] == "--socket") return await SocketTransportTests.RunAsync(Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll"));
if (args.Length == 1 && args[0] == "--character-graphics") return await CharacterGraphicsTests.RunAsync(Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll"));
if (args.Length == 1 && args[0] == "--character-network") return await CharacterNetworkTests.RunAsync(Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll"));

if (args.Length == 1 && args[0] == "--graphics-math") return GraphicsMathTests.Run();
if (args.Length == 1 && args[0] == "--graphics-scene") return GraphicsSceneTests.Run();
if (args.Length == 1 && args[0] == "--physics-isolation") return PhysicsIsolationTests.Run();

if (args.Length == 1 && args[0] == "--graphics")
    return GraphicsDeviceTests.Run() + await GraphicsIntegrationTests.RunAsync(Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll"))
        + await CharacterGraphicsTests.RunAsync(Path.Combine(AppContext.BaseDirectory, "Woorf.Cli.dll")) == 0 ? 0 : 1;

if (args.Length == 2 && args[0] == "--reference-game")
    return await ReferenceGameTests.RunAsync(args[1]);

if (args.Length == 2 && args[0] == "--reference-host")
    return ReferenceGameTests.RunHost(args[1]);

if (args.Length == 2 && args[0] == "--lifecycle")
    return await ScriptLifecycleTests.RunAsync(args[1]);

if (args.Length == 2 && args[0] == "--inbound")
    return await InboundLimitTests.RunAsync(args[1]);

if (args.Length == 2 && args[0] == "--identities")
    return await StableIdentityTests.RunAsync(args[1]);

if (args.Length == 2 && args[0] == "--persistence")
    return await BackendPersistenceTests.RunAsync(args[1]);

if (args.Length == 1 && args[0] == "--physics-clock")
    return PhysicsClockTests.Run();

if (args.Length == 1 && args[0] == "--physics-foundation")
    return PhysicsFoundationTests.Run() == 0 ? 0 : 1;

if (args.Length == 2 && args[0] == "--physics-network")
    return await PhysicsNetworkTests.RunAsync(args[1]);

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
    .Concat(InstanceEventCases.GetCases()).Concat(SchedulerCases.GetCases()).Concat(BackendApiCases.GetCases()).Concat(RuntimeSafetyCases.GetCases()).Concat(PhysicsCases.GetCases()).Concat(PhysicsShapeCases.GetCases()).Concat(CharacterBindingCases.GetCases()).ToArray();
if (args.Length == 2 && args[0] == "--case")
    cases = cases.Where(test => test.Name.Contains(args[1], StringComparison.OrdinalIgnoreCase)).ToArray();
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
if (args.Length == 2 && args[0] == "--case") return failedCount == 0 ? 0 : 1;
failedCount += await NetworkProjectTests.RunAsync(cliPath);
failedCount += await ProjectTests.RunAsync(cliPath);
failedCount += await ProjectValidationTests.RunAsync(cliPath);
failedCount += RuntimeClockTests.Run();
failedCount += PhysicsClockTests.Run();
failedCount += PhysicsFoundationTests.Run();
failedCount += PhysicsIsolationTests.Run();
failedCount += GraphicsMathTests.Run();
failedCount += GraphicsSceneTests.Run();
failedCount += CharacterControllerTests.Run();
failedCount += ObbyTests.Run();
failedCount += CharacterLifecycleTests.Run();
failedCount += CharacterViewTests.Run();
failedCount += CharacterViewTests.RunInput();
failedCount += CharacterReplicationTests.Run();
failedCount += NetworkLifecycleTests.Run();
failedCount += await SocketTransportTests.RunAsync(cliPath);
failedCount += await SocketBackpressureTests.RunAsync(cliPath);
failedCount += await SocketMetricsTests.RunAsync();
failedCount += DirtyReplicationTests.Run();
failedCount += await BackendPersistenceTests.RunAsync(cliPath);
failedCount += await ReferenceGameTests.RunAsync(cliPath);
failedCount += await ScriptLifecycleTests.RunAsync(cliPath);
failedCount += await InboundLimitTests.RunAsync(cliPath);
failedCount += await StableIdentityTests.RunAsync(cliPath);
failedCount += await PhysicsNetworkTests.RunAsync(cliPath);
failedCount += await CharacterNetworkTests.RunAsync(cliPath);
return failedCount == 0 ? 0 : 1;
