namespace Woorf.Engine.Datatypes;

public readonly record struct PhysicalProperties
{
    public float Density { get; }
    public float Friction { get; }
    public float Elasticity { get; }
    public float FrictionWeight { get; }
    public float ElasticityWeight { get; }
    public static PhysicalProperties Default { get; } = new(0.7f, 0.3f, 0.5f);
    public PhysicalProperties(float density, float friction, float elasticity, float frictionWeight = 1, float elasticityWeight = 1)
    {
        Density = Clamp(density, 0.0001f, 100);
        Friction = Clamp(friction, 0, 2);
        Elasticity = Clamp(elasticity, 0, 1);
        FrictionWeight = Clamp(frictionWeight, 0, 100);
        ElasticityWeight = Clamp(elasticityWeight, 0, 100);
    }
    private static float Clamp(float value, float minimum, float maximum) => float.IsFinite(value)
        ? Math.Clamp(value, minimum, maximum) : throw new ArgumentException("Expected finite physical property.");
    internal float[] GetComponents() => [Density, Friction, Elasticity, FrictionWeight, ElasticityWeight];
    internal static PhysicalProperties FromComponents(float[] values) => values.Length == 5
        ? new(values[0], values[1], values[2], values[3], values[4]) : throw new ArgumentException("Expected five physical properties.");
}
