using Unity.Entities;

public struct ReloadEvent : IComponentData, IEnableableComponent
{
    public Entity Tower;
}