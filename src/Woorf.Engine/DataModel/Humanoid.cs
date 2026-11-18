using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed partial class Humanoid : Instance
{
    private bool autoRotate = true;
    internal Datatypes.Vector3 CameraHeading { get; set; }

    public bool AutoRotate
    {
        get => autoRotate;
        set
        {
            if (autoRotate == value) return;
            autoRotate = value;
            FirePropertyChanged(nameof(AutoRotate));
        }
    }

    private double health = 100;
    private double maxHealth = 100;
    internal Humanoid() : base("Humanoid") => GetSignal("Destroying").Connect(_ => { Died.Close(); HealthChanged.Close(); });
    public ScriptSignal Died { get; } = new();
    public ScriptSignal HealthChanged { get; } = new();
    public double MaxHealth
    {
        get => maxHealth;
        set { Validate(value); if (value < 0) throw new InvalidOperationException("MaxHealth must be nonnegative."); maxHealth = value; FirePropertyChanged("MaxHealth"); Health = health; }
    }
    public double Health
    {
        get => health;
        set
        {
            Validate(value);
            var next = Math.Clamp(value, 0, maxHealth);
            if (next == health) return;
            var previous = health;
            health = next;
            FirePropertyChanged("Health");
            HealthChanged.Fire(health);
            if (previous > 0 && health == 0) Died.Fire();
        }
    }
    public void TakeDamage(double damage) { Validate(damage); Health -= damage; }
    private static void Validate(double value) { if (!double.IsFinite(value)) throw new InvalidOperationException("Expected finite health value."); }
    protected override bool HasProperty(string property) => property is "Health" or "MaxHealth" or "WalkSpeed" or "JumpPower" or "MoveDirection" or "Jump" or "AutoRotate" || base.HasProperty(property);
    protected override void CopyPropertiesTo(Instance clone)
    {
        var humanoid = (Humanoid)clone;
        humanoid.health = health; humanoid.maxHealth = maxHealth;
        humanoid.autoRotate = autoRotate;
        humanoid.walkSpeed = walkSpeed; humanoid.jumpPower = jumpPower;
    }
}
