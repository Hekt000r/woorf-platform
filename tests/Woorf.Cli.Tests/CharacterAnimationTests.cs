using System.Numerics;
using Woorf.Engine.DataModel;
using Woorf.Graphics;

internal static class CharacterAnimationTests
{
    internal static int Run()
    {
        try
        {
            var root = (Part)Instance.Create("Part");
            var humanoid = (Humanoid)Instance.Create("Humanoid");
            humanoid.Move(new(0,0,-1));
            var view = new CharacterView();
            root.AssemblyLinearVelocity = new(0,20,0); view.Capture(root,humanoid,0);
            root.Position = new(0,4,-1); root.AssemblyLinearVelocity = new(0,0,-16);
            var first = Lean(view.Capture(root,humanoid,.1),4);
            var second = Lean(view.Capture(root,humanoid,.1+1d/240),4);
            if (MathF.Abs(first)<.1f || MathF.Abs(second)<.1f || first*second<0)
                throw new Exception("Landing gait must persist between physics publications instead of resetting to idle.");
            if (MathF.Abs(second-first)<.0001f)
                throw new Exception("Published velocity must advance gait with elapsed time between position updates.");
            root.AssemblyLinearVelocity = default;
            if (MathF.Abs(Lean(view.Capture(root,humanoid,.12),4))>.0001f)
                throw new Exception("A stopped character must return to idle even with movement input held.");
            Console.WriteLine("PASS shared landing animation advances between physics publications and stops with actual motion");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL character animation: "+error.Message); return 1; }
    }

    private static float Lean(RenderItem[] items, int limb)
    {
        var torso = items[0].Transform; var pose = items[limb].Transform;
        var forward = Vector3.Normalize(new(-torso.M31,-torso.M32,-torso.M33));
        return Vector3.Dot(Vector3.Normalize(new(pose.M21,pose.M22,pose.M23)),forward);
    }
}
