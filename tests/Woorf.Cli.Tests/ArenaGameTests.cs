using Woorf.Engine.DataModel;
using Woorf.Engine.Gui;
using Woorf.Engine.Networking;
using Woorf.Engine.Projects;
using Woorf.Engine.Scheduling;
using Woorf.Engine.Scripting;

internal static class ArenaGameTests
{
    internal static int Run()
    {
        try
        {
            if(!File.Exists("examples/last-platform/woorf.project.json")) throw new Exception("Last Platform Standing demo is missing.");
            var clock=new Clock(); var project=ProjectLoader.Load("examples/last-platform");
            using var host=new LocalPlayHost(project,clock);
            for(var index=1;index<=480;index++) { clock.Now=index/60d; host.Pump(); }
            var state=project.Game.GetService("ReplicatedStorage").FindFirstChild("ArenaState")!;
            if(state.GetAttribute("Phase") as string!="Fight" || !Equals(state.GetAttribute("Alive"),4d))
                throw new Exception("Solo launch must start a four-contestant round with three bots.");
            var bots=project.Game.Workspace.FindFirstChild("ArenaBots")!.GetChildren().OfType<Model>().ToArray();
            if(bots.Length!=3||bots.All(bot=>bot.PrimaryPart!.AssemblyLinearVelocity.Magnitude<1))
                throw new Exception("Bots must actually move through the shared Humanoid controller.");
            var player=((Players)project.Game.GetService("Players")).GetPlayers()[0];
            foreach(var bot in bots) ((Humanoid)bot.FindFirstChild("Humanoid")!).Health=0;
            for(var index=481;index<=500;index++) { clock.Now=index/60d; host.Pump(); }
            if(!Equals((player.FindFirstChild("leaderstats")?.FindFirstChild("Wins") as ValueObject)?.Value,1d)||state.GetAttribute("Phase") as string!="Result")
                throw new Exception("Eliminating all bots must award the surviving human exactly one round win.");
            for(var index=501;index<=950;index++) { clock.Now=index/60d; host.Pump(); }
            if(state.GetAttribute("Phase") as string!="Fight"||!Equals(state.GetAttribute("Round"),2d))
                throw new Exception("Results must automatically rebuild the arena and begin the next round.");
            var screen=host.Player.FindFirstChild("PlayerGui")?.FindFirstChild("ArenaHud");
            if(screen?.FindFirstChild("Shove") is not GuiInstance) throw new Exception("Client HUD must expose the shove action.");
            var players = ((Players)host.Game.GetService("Players")).GetPlayers();
            var leaderboard = host.Game.CoreOverlay.Capture(players, 800, 600);
            if (screen.FindFirstChild("Board") is not null ||
                !leaderboard.Any(item => item.Content == "Wins") ||
                !leaderboard.Any(item => item.Content == "1"))
                throw new Exception("The built-in leaderboard must render the replicated win count.");
            CheckRules();
            if(host.HasFailed) throw new Exception("Arena scripts failed during acceptance.");
            Console.WriteLine("PASS arena solo bots fight elimination scoring HUD and automatic round restart"); return 0;
        }
        catch(Exception error) { Console.Error.WriteLine("FAIL arena game: "+error); return 1; }
    }

    private static void CheckRules()
    {
        var game=new DataModel(); using var session=new LuauSession(game,new Clock());
        var modules=new[]{"Arena","Roster","Combat","Bots","Match"};
        var source="local modules={}\n";
        foreach(var module in modules)
            source+=$"modules['{module}']=(function() local require=function(item) return modules[item] end; local script={{Parent=setmetatable({{}},{{__index=function(_,key)return key end}})}}\n"+
                File.ReadAllText($"examples/last-platform/src/ServerScriptService/{module}.luau")+"\nend)()\n";
        source+="""
            local match=modules.Match.new()
            local p=Instance.new('Player',game:GetService('Players')); match:addPlayer(p)
            match:update(6.1)
            assert(match.phase=='Fight' and #match.roster.actors==4)
            local actor=match.roster.byPlayer[p]; local victim=match.roster.actors[2]
            actor.root.Position=Vector3.new(0,4,0); victim.root.Position=Vector3.new(0,4,-4)
            actor.face=Vector3.new(0,0,-1)
            assert(match:request(p,'Shove'))
            assert(not match:request(p,'Shove'),'Shove cooldown must be authoritative')
            assert(not match:request(p,'Unknown'),'Unknown actions must be rejected')
            assert(not match:request(p,'Shove',victim),'Extra payload must be rejected')
            match:update(.31)
            assert(victim.pushUntil>match.time,'Visible windup must resolve into nearby knockback')
            local before=victim.root.Position
            match:update(.1)
            assert((victim.root.Position-before).Magnitude>2,'Knockback must actually displace its victim')
            match.arena:update(15,.1)
            local warned=0
            for _,tile in match.arena.tiles do if tile.state=='Warning' then warned+=1 end end
            assert(warned>0,'Outer tiles must visibly warn before they disappear')
            match.arena:update(17,.1)
            local gone=0
            for _,tile in match.arena.tiles do if tile.state=='Falling' then gone+=1 end end
            assert(gone>0,'Warned tiles must become noncolliding fall hazards')
            local stranger=Instance.new('Player',game:GetService('Players')); match:addPlayer(stranger)
            assert(not match:request(stranger,'Bots'),'Only the host may change bot settings')
            assert(match:request(p,'Bots'),'Host must be able to disable bots for the next round')
            assert(match.botsEnabled==false)
            match:finish(actor)
            match:finish(actor)
            match:finish(actor)
            assert(p.leaderstats.Wins.Value==3 and match.champion)
            match:update(8.1)
            assert(p.leaderstats.Wins.Value==0 and match.round==1)
            """;
        if(session.Execute(source,"arena-rules")!=0||session.HasFailed) throw new Exception("Arena combat/collapse/host authority checks failed.");
    }

    private sealed class Clock:IRuntimeClock
    {
        internal double Now;
        public double ElapsedSeconds=>Now;
        public void Sleep(TimeSpan duration)=>Now+=duration.TotalSeconds;
    }
}
