using Woorf.Engine.DataModel;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;

internal static class ArenaEngineTests
{
    internal static int Run()
    {
        var failures = 0;
        foreach (var (name,check) in new (string,Action)[] { ("NPC Humanoid moves jumps and is posed by shared presentation",CheckNpc),
            ("keyboard input reaches Luau with processed state and release edges",CheckInput),
            ("shared viewer honors game view distance and initial pitch attributes",CheckView) })
        {
            try { check(); Console.WriteLine("PASS "+name); }
            catch(Exception error) { failures++; Console.Error.WriteLine("FAIL "+name+": "+error.GetBaseException().Message); }
        }
        return failures;
    }

    private static void CheckNpc()
    {
        var game = new DataModel(); var clock = new Clock();
        using var session = new LuauSession(game,clock);
        if(session.Execute("""
            workspace.Gravity=80
            local floor=Instance.new('Part',workspace); floor.Anchored=true; floor.Size=Vector3.new(100,1,100)
            floor.CustomPhysicalProperties=PhysicalProperties.new(.7,.5,0)
            local bot=Instance.new('Model',workspace); bot.Name='Bot'
            local root=Instance.new('Part',bot); root.Name='HumanoidRootPart'; root.Size=Vector3.new(2,4,2); root.Position=Vector3.new(0,3,0)
            root.CustomPhysicalProperties=PhysicalProperties.new(.7,0,0); bot.PrimaryPart=root
            local humanoid=Instance.new('Humanoid',bot); humanoid:Move(Vector3.new(1,0,0))
            ""","npc")!=0) throw new Exception("NPC fixture failed.");
        for(var index=1;index<=120;index++) { clock.Now=index/120d; session.Step(clock.Now); }
        var model=(Model)game.Workspace.FindFirstChild("Bot")!;
        if(model.PrimaryPart!.Position.X<14) throw new Exception("NPC Humanoid:Move must drive the same physical character controller as players.");
        using var view = new WorldView(game,null);
        if(view.Capture(clock.Now).Items.Length!=9) throw new Exception("NPCs must have eight posed body pieces and hide their physical root.");
        var humanoid=(Humanoid)model.FindFirstChild("Humanoid")!; humanoid.Jump=true;
        clock.Now+=1d/60; session.Step(clock.Now);
        if(model.PrimaryPart.AssemblyLinearVelocity.Y<20) throw new Exception("Grounded NPC Jump must launch its collider.");
        model.Destroy(); clock.Now+=1d/60; session.Step(clock.Now);
        if(view.Capture(clock.Now).Items.Length!=1) throw new Exception("Destroyed NPCs must leave no visible body or controller.");
    }

    private static void CheckInput()
    {
        var game=new DataModel(); ((RunService)game.GetService("RunService")).IsClientRuntime=true;
        using var session=new LuauSession(game,new Clock());
        var service=(UserInputService)game.GetService("UserInputService");
        if(session.Execute("""
            local u=game:GetService('UserInputService')
            assert(Enum.KeyCode.E.Value==101)
            u.InputBegan:Connect(function(input,processed)
                assert(typeof(input)=='InputObject' and input.KeyCode==Enum.KeyCode.E)
                assert(input.UserInputState==Enum.UserInputState.Begin)
                assert(not pcall(function() input.KeyCode=Enum.KeyCode.Q end))
                assert(input.UserInputType==Enum.UserInputType.Keyboard and u:IsKeyDown(Enum.KeyCode.E))
                workspace:SetAttribute('Began',(workspace:GetAttribute('Began') or 0)+1)
                workspace:SetAttribute('Processed',processed)
            end)
            u.InputEnded:Connect(function(input)
                assert(input.UserInputState==Enum.UserInputState.End and not u:IsKeyDown(input.KeyCode))
                workspace:SetAttribute('Ended',true)
            end)
            ""","input")!=0) throw new Exception("Keyboard API fixture failed.");
        service.SetKey("E",true,true); service.SetKey("E",true,true); service.SetKey("E",false,false);
        if(!Equals(game.Workspace.GetAttribute("Began"),1d)||game.Workspace.GetAttribute("Ended") is not true||game.Workspace.GetAttribute("Processed") is not true)
            throw new Exception("Input must deliver typed begin/end once and preserve the GUI-processed flag.");
    }

    private static void CheckView()
    {
        var player=(Player)Instance.Create("Player");
        var model=(Model)Instance.Create("Model"); var root=(Part)Instance.Create("Part"); root.Parent=model; model.PrimaryPart=root;
        var humanoid=(Humanoid)Instance.Create("Humanoid"); humanoid.Parent=model; player.Character=model;
        player.SetAttribute("ViewDistance",30d); player.SetAttribute("ViewPitch",.9d);
        var viewer=new CharacterViewer(player); var camera=new Woorf.Graphics.PerspectiveCamera(); viewer.Follow(camera,0);
        var offset=camera.Position-camera.Target;
        if(MathF.Abs(offset.Length()-30)>.001f||MathF.Abs(offset.Y-30*MathF.Sin(.9f))>.001f)
            throw new Exception("An arena needs a shared wider camera; game attributes must affect both hosts.");
    }

    private sealed class Clock : IRuntimeClock
    {
        internal double Now;
        public double ElapsedSeconds=>Now;
        public void Sleep(TimeSpan duration)=>Now+=duration.TotalSeconds;
    }
}
