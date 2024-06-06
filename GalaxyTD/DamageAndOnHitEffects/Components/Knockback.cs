using Unity.Entities;
using Unity.Mathematics;

public struct Knockback : IComponentData
{
    /// <summary>
    /// This is tower who is responsible For Knockback
    /// </summary>
    public Entity OriginTower;
    public float2 Direction;

    private const float minValueSqr =  0.04f;
    public bool Exists => math.lengthsq(Direction) > minValueSqr;
}
