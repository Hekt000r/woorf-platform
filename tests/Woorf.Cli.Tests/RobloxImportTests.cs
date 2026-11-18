using Woorf.Engine;
using Woorf.Engine.DataModel;
using Woorf.Engine.Projects;

internal static class RobloxImportTests
{
    private static void CheckBinary(string directory)
    {
        var input = Path.Combine(directory, "Binary.rbxl");
        var output = Path.Combine(directory, "BinaryWoorf");
        using (var file = new BinaryWriter(File.Create(input)))
        {
            file.Write("<roblox!"u8);
            file.Write(new byte[] { 0x89, 0xff, 13, 10, 26, 10, 0, 0 });
            file.Write(2); file.Write(2); file.Write(0L);
            WriteChunk(file, "INST", chunk => WriteClass(chunk, 0, "Workspace", 0));
            WriteChunk(file, "INST", chunk => WriteClass(chunk, 1, "Part", 1));
            WriteChunk(file, "PROP", chunk =>
            {
                chunk.Write(1); WriteString(chunk, "Name"); chunk.Write((byte)1);
                WriteString(chunk, "BinaryTile");
            }, true);
            WriteChunk(file, "PROP", chunk =>
            {
                chunk.Write(1); WriteString(chunk, "Anchored"); chunk.Write((byte)2); chunk.Write((byte)1);
            });
            WriteChunk(file, "PROP", chunk =>
            {
                chunk.Write(1); WriteString(chunk, "CFrame"); chunk.Write((byte)0x10);
                chunk.Write((byte)0x20);
                chunk.Write(new byte[] { 0x7f, 0, 0, 0, 0x80, 0, 0, 0, 0x81, 0, 0, 0 });
            });
            WriteChunk(file, "PRNT", chunk =>
            {
                chunk.Write((byte)0); chunk.Write(2);
                chunk.Write(new byte[] { 0, 0, 0, 0, 0, 0, 2, 1 });
                chunk.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });
            });
            WriteChunk(file, "END\0", chunk => chunk.Write("</roblox>"u8));
        }
        RobloxPlaceImporter.Import(input, output);
        if (ProjectLoader.Load(output).Game.Workspace.FindFirstChild("BinaryTile") is not Part { Anchored: true } tile ||
            tile.Position != new Woorf.Engine.Datatypes.Vector3(1, 2, 4) || tile.CFrame.LookVector.X != -1)
            throw new Exception("LZ4 binary place geometry and names must survive import.");
    }

    private static void WriteClass(BinaryWriter writer, int id, string name, byte referent)
    {
        writer.Write(id); WriteString(writer, name);
        writer.Write((byte)0); writer.Write(1);
        writer.Write(new byte[] { 0, 0, 0, (byte)(referent * 2) });
    }

    private static void WriteString(BinaryWriter writer, string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        writer.Write(bytes.Length); writer.Write(bytes);
    }

    private static void WriteChunk(BinaryWriter file, string name, Action<BinaryWriter> write, bool compress = false)
    {
        using var buffer = new MemoryStream();
        using var chunk = new BinaryWriter(buffer);
        write(chunk);
        var bytes = buffer.ToArray();
        file.Write(System.Text.Encoding.ASCII.GetBytes(name));
        file.Write(compress ? bytes.Length + 2 : 0);
        file.Write(bytes.Length); file.Write(0);
        if (compress) { file.Write((byte)0xf0); file.Write((byte)(bytes.Length - 15)); }
        file.Write(bytes);
    }

    internal static int Run()
    {
        var directory = Path.Combine(Environment.CurrentDirectory, ".local", "import-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var input = Path.Combine(directory, "Place.rbxlx");
        var output = Path.Combine(directory, "Woorf");
        File.WriteAllText(input, """
            <roblox version="4">
              <Item class="Workspace" referent="RBX0"><Properties><float name="Gravity">80</float></Properties>
                <Item class="Model" referent="RBX1"><Properties><string name="Name">Arena</string><Ref name="PrimaryPart">RBX2</Ref></Properties>
                  <Item class="Part" referent="RBX2"><Properties><string name="Name">Tile</string><bool name="Anchored">true</bool>
                    <Vector3 name="size"><X>40</X><Y>2</Y><Z>30</Z></Vector3>
                    <CoordinateFrame name="CFrame"><X>10</X><Y>2</Y><Z>30</Z><R00>0</R00><R01>0</R01><R02>1</R02><R10>0</R10><R11>1</R11><R12>0</R12><R20>-1</R20><R21>0</R21><R22>0</R22></CoordinateFrame>
                  </Properties></Item>
                  <Item class="Part" referent="RBX3"><Properties><string name="Name">Tile</string></Properties></Item>
                  <Item class="BrickColorValue"><Properties><int name="Value">21</int></Properties></Item>
                  <Item class="Decal"><Properties><string name="Name">Art</string></Properties></Item>
                </Item>
              </Item>
              <Item class="ServerScriptService"><Item class="ModuleScript"><Properties><string name="Name">Multiplier</string><ProtectedString name="Source">return 2</ProtectedString></Properties></Item><Item class="BindableFunction"><Properties><string name="Name">Query</string></Properties></Item><Item class="Script"><Properties><string name="Name">../Init</string>
                <ProtectedString name="Source"><![CDATA[assert(workspace.Arena.PrimaryPart.Anchored)
                local query = script.Parent.Query
                query.OnInvoke = function(value) return value * require(script.Parent.Multiplier) end
                assert(query:Invoke(4) == 8)]]></ProtectedString>
              </Properties></Item></Item>
            </roblox>
            """);
        var report = RobloxPlaceImporter.ImportXml(input, output);
        var project = ProjectLoader.Load(output);
        var workspace = (Workspace)project.Game.Workspace;
        var model = workspace.FindFirstChild("Arena") as Model ?? throw new Exception("Arena model missing.");
        if (workspace.Gravity != 80 || model.PrimaryPart is not Part { Anchored: true } tile ||
            tile.Size.X != 40 || tile.Position.Z != 30 || tile.CFrame.LookVector.X != -1 ||
            model.GetChildren().Count(item => item.Name == "Tile") != 2)
            throw new Exception("Imported geometry, duplicate names, or references did not survive loading.");
        if (model.FindFirstChild("BrickColorValue") is not ValueObject color ||
            !Equals(color.Value, new Woorf.Engine.Datatypes.BrickColor(21)))
            throw new Exception("Imported BrickColorValue lost its typed color.");
        if (!report.Warnings.Any(warning => warning.Contains("Decal", StringComparison.Ordinal)))
            throw new Exception("Unsupported classes must appear in the import report.");
        if (!project.Scripts.Any(entry => entry.Instance.Name == "../Init") || LuauRuntime.ExecuteProject(project) != 0)
            throw new Exception("Imported script source did not execute in the expected place.");
        CheckBinary(directory);
        Console.WriteLine("PASS Roblox XML and LZ4 binary places load as Woorf projects");
        return 0;
    }
}
