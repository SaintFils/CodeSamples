using ECSTest.Components;
using System;
using Unity.Collections;
using Unity.Entities;

public struct OnKillData
{
    public Entity Creep { private set; get; }
    public Entity Tower { private set; get; }
    public RefRW<CashComponent> CashComponent { private set; get; }
    public CreepsLocator CreepsLocator { private set; get; }
    public EntityManager Manager { private set; get; }
    public EntityCommandBuffer EntityCommandBuffer { private set; get; }
    public SharedCreepData SharedData { private set; get; }

    public bool Exist()
    {
        return Manager.Exists(Tower);
    }
    
    public OnKillData(Entity creep, Entity tower, RefRW<CashComponent> cashComponent, CreepsLocator creepsLocator, EntityManager manager, EntityCommandBuffer ecb, SharedCreepData sharedCreepData)
    {
        Creep = creep;
        Tower = tower;
        CashComponent = cashComponent;
        CreepsLocator = creepsLocator;
        Manager = manager;
        EntityCommandBuffer = ecb;
        SharedData = sharedCreepData;
    }

    public void AoeEffectOnKill(float aoeRange, Action<CreepInfo> action)
    {
        NativeList<CreepInfo> nearestCreeps = new(Allocator.Temp);
        PositionComponent creepPositionComponent = Manager.GetComponentData<PositionComponent>(Creep);

        CreepsLocator.LocateNearestCreeps(creepPositionComponent.Position, aoeRange, ref nearestCreeps, 20);

        foreach (CreepInfo creepInfo in nearestCreeps)
            action.Invoke(creepInfo);

        nearestCreeps.Dispose();

        DamageSystem.ShowTagEffect(EntityCommandBuffer, creepPositionComponent.Position, aoeRange, TagEffectType.AoeDebuff, "AoeEffectEvent");
    }
}