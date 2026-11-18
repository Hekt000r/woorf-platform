using Woorf.Engine.DataModel;
using Woorf.Engine.Gui;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;

static class GuiInputPayloadTests
{
    internal static int Run()
    {
        try
        {
            var game = new DataModel();
            using var session = new LuauSession(game, new MonotonicClock());
            var status = session.Execute("""
                local screen = Instance.new("ScreenGui", game:GetService("ReplicatedStorage"))
                local button = Instance.new("TextButton", screen)
                button.Size = UDim2.fromOffset(100, 50)
                button.InputBegan:Connect(function(input)
                    assert(typeof(input) == "InputObject" and input.UserInputState == Enum.UserInputState.Begin)
                    button:SetAttribute("SawBegin", true)
                end)
                button.InputEnded:Connect(function(input)
                    assert(typeof(input) == "InputObject" and input.UserInputState == Enum.UserInputState.End)
                    button:SetAttribute("SawEnd", true)
                end)
                button.Activated:Connect(function(input, count)
                    assert(typeof(input) == "InputObject", "Activated must supply InputObject")
                    assert(input.UserInputType == Enum.UserInputType.MouseButton1 and count == 1)
                    assert(input.UserInputState == Enum.UserInputState.End)
                    assert(input.Position == Vector3.new(20, 20, 0))
                    assert(not pcall(function() input.Position = Vector3.zero end))
                    button.Text = "Activated correctly"
                end)
                """, "gui-input-payload");
            if (status != 0) throw new Exception("GUI input fixture did not start.");
            var scene = new GuiScene(); scene.Capture(game.GetService("ReplicatedStorage"), 200, 100);
            scene.Pointer(20, 20, true); scene.Pointer(20, 20, false);
            var button = (GuiInstance)game.GetService("ReplicatedStorage").FindFirstChild("ScreenGui")!.FindFirstChild("TextButton")!;
            if (button.Get<string>("Text") != "Activated correctly") throw new Exception("Lua Activated callback did not receive a typed immutable input payload.");
            if (button.GetAttribute("SawBegin") is not true || button.GetAttribute("SawEnd") is not true)
                throw new Exception("InputBegan/InputEnded did not receive stateful InputObjects.");
            Console.WriteLine("PASS GUI Activated forwards immutable typed input coordinates and click count to Luau"); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL GUI input payload: " + error.Message); return 1; }
    }
}
