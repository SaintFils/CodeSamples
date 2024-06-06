using ECSTest.Components;
using Unity.Entities;

public struct OnHitTagData
{
    public GunCollisionEvent GunCollisionEvent;
    public AOECollisionEvent AoeCollisionEvent;
    public Entity CreepEntity;
    public RefRW<CashComponent> CashComponentRefRw;
    public int TagIndex;
}
