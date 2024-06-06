using Unity.Entities;

public struct DestroyComponent : IComponentData
{
    public bool IsNeedToDestroy;
    public float DestroyDelay;
}
