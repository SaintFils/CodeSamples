using ECSTest.Components;
using Unity.Entities;

public abstract class OnHitTag : Tag
{
    public abstract void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb0);
}
