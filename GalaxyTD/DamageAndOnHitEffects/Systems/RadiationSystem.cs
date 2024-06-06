using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct RadiationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<CashComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            BeginFixedStepSimulationEntityCommandBufferSystem.Singleton singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            new CalculateRadiation()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                CashComponent = SystemAPI.GetSingletonRW<CashComponent>().ValueRW,
                CommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged)
            }.Schedule();
        }

        [BurstCompile]
        private partial struct CalculateRadiation : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            public CashComponent CashComponent;
            public EntityCommandBuffer CommandBuffer;

            public void Execute(Entity creep, ref RadiationComponent radiationComponent,
                ref CreepComponent creepComponent, ref DestroyComponent destroyComponent, in PositionComponent positionComponent)
            {
                if (destroyComponent.IsNeedToDestroy)
                    return;

                if (radiationComponent.Time > 0)
                {
                    creepComponent.Hp -= radiationComponent.DPS * DeltaTime;
                    if (creepComponent.Hp <= 0)
                    {
                        DamageSystem.DestroyCreep(CommandBuffer, creep, ref destroyComponent);
                        CashComponent.CreepDestroyCashChange(creepComponent.FullReward, CommandBuffer, positionComponent.Position);
                    }
                    radiationComponent.Time -= DeltaTime;
                    if (radiationComponent.Time <= 0) radiationComponent.Time = 0;
                }
            }
        }
    }
}
