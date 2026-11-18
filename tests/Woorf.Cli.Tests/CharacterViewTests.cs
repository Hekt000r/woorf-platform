using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Graphics;

static class CharacterViewTests
{
    internal static int RunInput()
    {
        try
        {
            var controls = new CharacterControls();
            var camera = new PerspectiveCamera { Position = new(0, 5, 10), Target = Vector3.Zero };
            CharacterInput Read(float forward, float right, bool jump, bool reset) => controls.Update(camera, forward, right, jump, reset);
            var first = Read(1, 0, true, true);
            var direction = first.MoveDirection;
            if (MathF.Abs(direction.X) > 0.0001f || direction.Y != 0 || MathF.Abs(direction.Z + 1) > 0.0001f)
                throw new Exception("Forward input must follow camera heading on the horizontal plane.");
            if (!first.ShouldJump || !first.ShouldReset) throw new Exception("Pressed action keys were lost.");
            var held = Read(1, 1, true, true);
            if (held.ShouldJump || held.ShouldReset) throw new Exception("Held keys must not repeat jump/reset requests.");
            if (MathF.Abs(held.MoveDirection.Magnitude - 1) > 0.0001f)
                throw new Exception("Diagonal movement must not exceed axial speed.");
            Read(0, 0, false, false);
            camera.Position = new(10, 5, 0);
            var turned = Read(1, 0, true, false);
            if (MathF.Abs(turned.MoveDirection.X + 1) > 0.0001f || !turned.ShouldJump)
                throw new Exception("Movement must follow a rotated camera and permit a new jump after release.");
            CheckViewer();
            CheckMouseCamera();
            Console.WriteLine("PASS character controls follow camera heading cap diagonal speed and edge-trigger jump/reset");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL character controls: " + (error.InnerException?.Message ?? error.Message)); return 1; }
    }

    private static void CheckViewer()
    {
        var player = (Player)Instance.Create("Player");
        var model = (Model)Instance.Create("Model");
        var root = (Part)Instance.Create("Part"); root.Position = new(0, 4, 0); root.Parent = model;
        var humanoid = (Humanoid)Instance.Create("Humanoid"); humanoid.Parent = model;
        model.PrimaryPart = root; player.Character = model;
        var viewer = new CharacterViewer(player);
        var camera = new PerspectiveCamera(); viewer.Follow(camera, 0);
        root.Position = new(10, 4, 0); viewer.Follow(camera, 0.1f);
        if (camera.Target.X <= 1 || camera.Position.X <= 1) throw new Exception("The character camera must follow root motion.");
        viewer.ApplyInput(camera, 1, 0, true, false, 0, 0, 0.016f);
        if (humanoid.MoveDirection.Z > -0.99f || !humanoid.Jump) throw new Exception("Viewer input must reach the current Humanoid.");
        humanoid.Jump = false;
        viewer.ApplyInput(camera, 1, 0, true, false, 0, 0, 0.016f);
        if (humanoid.Jump) throw new Exception("Viewer must not repeat held jump input.");
        viewer.ApplyInput(camera, 0, 0, false, true, 0, 0, 0.016f);
        if (humanoid.Health != 0) throw new Exception("Reset input must trigger the character's real death lifecycle.");
        var composer = new CharacterFrameComposer(player);
        var scene = new[] { new RenderItem(MeshShape.Box, Matrix4x4.Identity, Vector3.One) };
        var frame = composer.Capture(scene, 0);
        if (frame.Length < 7 || frame[0] != scene[0]) throw new Exception("Character rendering must preserve scene geometry and add posed limbs.");
        if (!ReferenceEquals(frame, composer.Capture(scene, 0.1))) throw new Exception("Combined rendering must reuse its draw buffer.");
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 100; index++) composer.Capture(scene, index);
        if (GC.GetAllocatedBytesForCurrentThread() != allocated) throw new Exception("Steady character composition must not allocate.");
        player.Character = null;
        if (!ReferenceEquals(scene, composer.Capture(scene, 0.2))) throw new Exception("Removed characters must stop contributing draw items.");
        var replacement = (Model)Instance.Create("Model");
        var replacementRoot = (Part)Instance.Create("Part"); replacementRoot.Position = new(-20, 4, 0); replacementRoot.Parent = replacement;
        var replacementHumanoid = (Humanoid)Instance.Create("Humanoid"); replacementHumanoid.Parent = replacement;
        replacement.PrimaryPart = replacementRoot; player.Character = replacement;
        viewer.Follow(camera, 0.001f);
        if (MathF.Abs(camera.Target.X + 20) > 0.0001f) throw new Exception("The camera must snap to a respawned character.");
        viewer.ApplyInput(camera, 1, 0, false, false, 0, 0, 0.016f);
        if (replacementHumanoid.MoveDirection.Z > -0.99f) throw new Exception("Input must rebind to the replacement Humanoid.");
        var respawned = composer.Capture(scene, 0.3);
        if (MathF.Abs(respawned[1].Transform.M41 + 20) > 0.0001f || respawned[1].Color.Z <= respawned[1].Color.X)
            throw new Exception("Composition must follow the replacement character and restore its living appearance.");
    }

    private static void CheckMouseCamera()
    {
        var game = new DataModel();
        var player = (Player)Instance.Create("Player");
        var model = (Model)Instance.Create("Model");
        var root = (Part)Instance.Create("Part");
        root.Parent = model;
        model.PrimaryPart = root;
        Instance.Create("Humanoid").Parent = model;
        model.Parent = game.Workspace;
        player.Parent = game.GetService("Players");
        player.Character = model;
        var viewer = new CharacterViewer(player);
        var camera = new PerspectiveCamera();
        viewer.Follow(camera, 0);
        var initial = camera.Position;
        viewer.ApplyPointer(100, 0, 0, false);
        viewer.Follow(camera, 0);
        if (camera.Position != initial) throw new Exception("Third-person look requires right drag.");
        viewer.ApplyPointer(100, 0, 1, true);
        viewer.Follow(camera, 0);
        if (camera.Position.X == initial.X || Vector3.Distance(camera.Position, camera.Target) >= 12)
            throw new Exception("Right drag and wheel must orbit and zoom.");
        viewer.ApplyPointer(0, 0, 30, false);
        viewer.Follow(camera, 0);
        using var world = new WorldView(game, player);
        if (!viewer.IsFirstPerson || world.Capture(0, viewer.IsFirstPerson).Items.Length != 0)
            throw new Exception("Full zoom must enter first person and hide the local avatar.");
        var heading = camera.Target - camera.Position;
        viewer.ApplyPointer(100, 0, 0, false);
        viewer.Follow(camera, 0);
        if (camera.Target - camera.Position == heading) throw new Exception("First-person mouse look must work without right drag.");
        viewer.ApplyPointer(0, 0, -1, false);
        if (viewer.IsFirstPerson || world.Capture(0, viewer.IsFirstPerson).Items.Length == 0)
            throw new Exception("Scrolling out must restore the third-person avatar.");

        using var session = new Woorf.Engine.Scripting.LuauSession(game, new Woorf.Engine.Scheduling.MonotonicClock());
        if (session.Execute("""
            local player = game:GetService("Players"):GetPlayers()[1]
            player.CameraMinZoomDistance = 5
            player.CameraMaxZoomDistance = 10
            assert(player.CameraMinZoomDistance == 5 and player.CameraMaxZoomDistance == 10)
            assert(player.CameraMode == Enum.CameraMode.Classic)
            """, "camera-settings") != 0)
            throw new Exception("Documented Roblox Player camera properties must be scriptable.");
        viewer.ApplyPointer(0, 0, 30, false);
        if (viewer.IsFirstPerson) throw new Exception("The configured minimum must prevent first person.");
        if (session.Execute("""
            game:GetService("Players"):GetPlayers()[1].CameraMode = Enum.CameraMode.LockFirstPerson
            """, "camera-mode") != 0 || !viewer.IsFirstPerson)
            throw new Exception("CameraMode.LockFirstPerson must override the zoom range.");
    }

    internal static int Run()
    {
        try
        {
            var view = new CharacterView();
            var root = (Part)Instance.Create("Part"); root.Size = new(2, 4, 2); root.Position = new(0, 4, 0);
            var humanoid = (Humanoid)Instance.Create("Humanoid");
            RenderItem[] Read(double time) => view.Capture(root, humanoid, time);
            var first = Read(0);
            if (first.Length < 6) throw new Exception("Character geometry must include a head, torso and four limbs.");
            var head = first[1];
            if (head.Transform.M42 <= first[0].Transform.M42) throw new Exception("Character head must be above its torso.");
            root.Position = new(1, 4, 0);
            var walking = Read(0.1);
            if (MathF.Abs(Lean(walking, 4)) < 0.1f || Lean(walking, 4) * Lean(walking, 5) >= 0)
                throw new Exception("Walking must swing left and right legs in opposite directions.");
            var idle = Read(0.2);
            if (MathF.Abs(Lean(idle, 4)) > 0.0001f) throw new Exception("Blocked/stationary characters must not walk in place.");
            if (!ReferenceEquals(first, idle)) throw new Exception("Character posing must reuse its render buffer.");
            root.AssemblyLinearVelocity = new(0, 20, 0);
            var airborne = Read(0.3);
            if (MathF.Abs(Lean(airborne, 4)) < 0.05f || MathF.Abs(Lean(airborne, 4) - Lean(airborne, 5)) > 0.0001f)
                throw new Exception("Airborne characters must use an airborne limb pose.");
            humanoid.Health = 0;
            var dead = Read(0.4);
            if (dead[0].Color.Z > dead[0].Color.X * 1.1f) throw new Exception("Death must visibly distinguish the inactive character before respawn.");
            Console.WriteLine("PASS character view follows motion and poses walking airborne and idle limbs centrally");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL character view: " + (error.InnerException?.Message ?? error.Message)); return 1; }
    }

    private static float Lean(RenderItem[] items, int limb)
    {
        var torso = items[0].Transform; var pose = items[limb].Transform;
        var forward = Vector3.Normalize(new(-torso.M31, -torso.M32, -torso.M33));
        return Vector3.Dot(Vector3.Normalize(new(pose.M21, pose.M22, pose.M23)), forward);
    }
}
