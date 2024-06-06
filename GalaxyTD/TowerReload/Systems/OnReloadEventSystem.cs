using ECSTest.Components;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ReloadingSystem))]
public partial struct OnReloadEventSystem : ISystem
{
    private EntityQuery reloadQuery;

    public void OnCreate(ref SystemState state)
    {
        reloadQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ReloadEvent>()
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        var buffers = InitializeBuffers();
        
        EntityManager manager = state.EntityManager;
        
        for (int i = 0; i < buffers.entities.Length; i++)
        {
            TagsComponent tagsComponent = manager.GetComponentData<TagsComponent>(buffers.reloadEvents[i].Tower);
            OnReload(buffers.reloadEvents[i].Tower, tagsComponent.Tags, manager);
            buffers.ecb.SetComponentEnabled<ReloadEvent>(buffers.entities[i], false);
        }
        
        Dispose(buffers, manager);
    }
    
    private (EntityCommandBuffer ecb, NativeArray<ReloadEvent> reloadEvents, NativeArray<Entity> entities) InitializeBuffers()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        
        NativeArray<Entity> entities = reloadQuery.ToEntityArray(Allocator.Temp);
        NativeArray<ReloadEvent> reloadEvents = reloadQuery.ToComponentDataArray<ReloadEvent>(Allocator.Temp);

        return (ecb, reloadEvents, entities);
    }

    private void OnReload(Entity attacker, List<Tag> tags, EntityManager manager)
    {
        try
        {
            foreach (Tag tag in tags)
                if (tag is OnReloadTag reloadTag)
                    reloadTag.OnReload(attacker, manager);
            
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"-> error in OnReload tags: {e}");
        }
    }
    
    private void Dispose((EntityCommandBuffer ecb, NativeArray<ReloadEvent> reloadEvents, NativeArray<Entity> entities) buffers, EntityManager manager)
    {
        buffers.reloadEvents.Dispose();
        buffers.entities.Dispose();
        
        buffers.ecb.Playback(manager);
        buffers.ecb.Dispose();
    }
}
