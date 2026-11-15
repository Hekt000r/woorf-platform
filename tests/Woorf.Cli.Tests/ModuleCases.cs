static class ModuleCases
{
    public static IEnumerable<(string Name, Action<string> Arrange, int Exit, string Output, string Error)> GetCases()
    {
        yield return ("requires isolated modules with shared cached identity", root =>
        {
            Write(root, "src/ReplicatedStorage/Config.luau", "assert(leaked == nil); leaked = true; print('load'); return {name = script.Name, game = game}");
            Write(root, "src/ServerScriptService/A.server.luau", "leaked = true; local c = require(game.ReplicatedStorage.Config); assert(c.name == 'Config' and c.game == game); c.shared = 42");
            Write(root, "src/ServerScriptService/B.server.luau", "local m = game.ReplicatedStorage.Config; assert(require(m) == require(m) and require(m).shared == 42 and leaked == nil)");
        }, 0, "load\n", "");
        yield return ("requires nested modules and caches false and nil", root =>
        {
            Write(root, "src/ReplicatedStorage/False.luau", "print('false'); return false");
            Write(root, "src/ReplicatedStorage/Nil.luau", "print('nil'); return nil");
            Write(root, "src/ReplicatedStorage/Outer.luau", "local s = script.Parent; assert(require(s.False) == false and require(s.False) == false); assert(require(s.Nil) == nil and require(s.Nil) == nil); return 'outer'");
            Write(root, "src/ServerScriptService/Main.server.luau", "assert(require(game.ReplicatedStorage.Outer) == 'outer')");
        }, 0, "false\nnil\n", "");
        yield return Failure("rejects missing module return", "local value = 1", "exactly one value");
        yield return Failure("rejects multiple module returns", "return 1, 2", "exactly one value");
        yield return Failure("reports module syntax errors", "local =", "Config");
        yield return Failure("reports module runtime errors", "error('module failure')", "module failure");
        yield return ("detects circular module dependencies", root =>
        {
            Write(root, "src/ReplicatedStorage/A.luau", "return require(script.Parent.B)");
            Write(root, "src/ReplicatedStorage/B.luau", "return require(script.Parent.A)");
            Write(root, "src/ServerScriptService/Main.server.luau", "require(game.ReplicatedStorage.A)");
        }, 1, "", "Circular module dependency");
        yield return ("requires clones and callback modules while rejecting invalid inputs", root =>
        {
            Write(root, "src/ReplicatedStorage/Config.luau", "return {name=script.Name}");
            Write(root, "src/ServerScriptService/Main.server.luau", """
                for _, value in {1, 'path', {}, workspace, script} do assert(not pcall(require, value)) end
                assert(not pcall(require))
                local module = game.ReplicatedStorage.Config
                local clone = module:Clone(); clone.Name = 'Copy'
                assert(require(clone).name == 'Copy' and require(clone) ~= require(module))
                local value
                workspace.ChildAdded:Once(function() value = require(module) end)
                Instance.new('Folder', workspace)
                assert(value == require(module))
                """);
        }, 0, "", "");
        yield return ("caches failed module execution", root =>
        {
            Write(root, "src/ReplicatedStorage/Config.luau", "print('attempt'); error('cached failure')");
            Write(root, "src/ServerScriptService/Main.server.luau", "local m = game.ReplicatedStorage.Config; for i=1,2 do local ok, err = pcall(require,m); assert(not ok and string.find(err,'cached failure')) end");
        }, 0, "attempt\n", "");
    }

    private static (string, Action<string>, int, string, string) Failure(string name, string source, string error) =>
        (name, root =>
        {
            Write(root, "src/ReplicatedStorage/Config.luau", source);
            Write(root, "src/ServerScriptService/Main.server.luau", "require(game.ReplicatedStorage.Config)");
        }, 1, "", error);

    private static void Write(string root, string path, string source)
    {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, source);
    }
}
