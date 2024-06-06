using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class TeleportationOnHitTag : OnHitTag
{
    [SerializeField, BoxGroup("Chance Bounds")] private float minProbabilityBound = .1f;
    [SerializeField, BoxGroup("Chance Bounds")] private float maxProbabilityBound = .9f;
    [SerializeField, BoxGroup("Chance Bounds")] private float minAmmoBound = 100f;
    [SerializeField, BoxGroup("Chance Bounds")] private float maxAmmoBound = 1f;
    [SerializeField, InfoBox("Iterations to find position for teleportation")] private int iterations = 4;
    [SerializeField] private float teleportationDistance = 2f;

    private OutFlowField outFlowField;
    
    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        float teleportationProbability = Utilities.GetLerpedValue(minAmmoBound, maxAmmoBound, minProbabilityBound, maxProbabilityBound, attackerComponent.AttackStats.ReloadStats.MagazineSize);
        float rand = Random.Range(0f, 100f);

        if (rand <= teleportationProbability)
        {
            if(!manager.Exists(onHitTagData.CreepEntity))
                return;
            
            EntityQuery entityQuery = manager.CreateEntityQuery(new ComponentType[] {typeof(OutFlowField)});
            outFlowField = entityQuery.GetSingleton<OutFlowField>();
            
            PositionComponent creepPositionComponent = manager.GetComponentData<PositionComponent>(onHitTagData.CreepEntity);

            SetUpTeleportation(iterations, outFlowField, creepPositionComponent, teleportationDistance, onHitTagData.CreepEntity, manager);
        }
    }

    public static void SetUpTeleportation(int iterations, OutFlowField outFlowField, PositionComponent creepPositionComponent, float teleportationDistance, Entity creepEntity, EntityManager manager)
    {
        for (int i = 0; i < iterations; i++)
        {
            float randomAngle = Random.Range(0, math.PI);
            float2 vector = Utilities.GetRotated(Vector2.right, randomAngle);
            float2 newCreepPosition = creepPositionComponent.Position - vector * teleportationDistance;
                
            if (IsCellAvailable(newCreepPosition))
            {
                SetNewPosition(newCreepPosition);
                break;
            }

            if (i == iterations - 1)
            {
                newCreepPosition = creepPositionComponent.Position - creepPositionComponent.Direction * teleportationDistance;
                    
                if (IsCellAvailable(newCreepPosition))
                    SetNewPosition(newCreepPosition);
                // At first, the logic of destroying a creep was implied here. But the game designer decided that in this case nothing would simply happen to the creep
            }
        }
        
        bool IsCellAvailable(float2 newCreepPosition)
        {
            int2 gridPos = new((int)newCreepPosition.x, (int)newCreepPosition.y);
            float2 cellDirection = outFlowField.GetDirection(gridPos);

            return !cellDirection.Equals(float2.zero);
        }

        void SetNewPosition(float2 newCreepPosition)
        {
            creepPositionComponent.Position = newCreepPosition;
            manager.SetComponentData(creepEntity, creepPositionComponent);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/TeleportationOnHit");
}