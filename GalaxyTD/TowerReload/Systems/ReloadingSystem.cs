using ECSTest.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using static AllEnums;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(TargetingSystemBase))]
public partial struct ReloadingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CashComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);

        ReloadingJob reloadingJob = new()
        {
            CashComponent = SystemAPI.GetSingletonRW<CashComponent>().ValueRW,
            EntityCommandBuffer = ecb
        };
        state.Dependency = reloadingJob.Schedule(state.Dependency);
    }

    [BurstCompile(CompileSynchronously = true)]
    public partial struct ReloadingJob : IJobEntity
    {
        internal EntityCommandBuffer EntityCommandBuffer;
        public CashComponent CashComponent;

        private void Execute(ref AttackerComponent attacker, ref AttackerStatisticComponent statistic, in PositionComponent position, Entity attackerEntity)
        {
            int reloadCost = attacker.AttackStats.ReloadStats.ReloadCost;

            if(ShouldReload(ref attacker, reloadCost))
            {
                ReloadIfNecessary(ref attacker, ref statistic, reloadCost, position.Position);
                CreateReloadEvent(attackerEntity);
            }
        }

        private bool ShouldReload(ref AttackerComponent attacker, int reloadCost)
        {
            return attacker.Bullets <= 0 && attacker.AutoReload && CashComponent.CanSpendCash(reloadCost) 
                   && attacker.AttackPattern != AttackPattern.Off;
        }

        private void ReloadIfNecessary(ref AttackerComponent attacker, ref AttackerStatisticComponent statistic, int reloadCost, float2 position)
        {
            Reload(ref attacker);
            
            statistic.Reloads++;
            statistic.CashReloadSpent += reloadCost;
            CashComponent.ReloadAttacker(attacker, EntityCommandBuffer, position);
        }

        private void CreateReloadEvent(Entity attackerEntity)
        {
            Entity reloadEntity = EntityCommandBuffer.CreateEntity();
            EntityCommandBuffer.SetName(reloadEntity, nameof(ReloadEvent));
            EntityCommandBuffer.AddComponent(reloadEntity, new ReloadEvent() { Tower = attackerEntity });
        }
    }

    public static void Reload(ref AttackerComponent attacker)
    {
        attacker.Bullets = attacker.AttackStats.ReloadStats.MagazineSize;
        attacker.ReloadTimer = attacker.AttackStats.ReloadStats.ReloadTime;
        attacker.BulletLeftInCurrentBurst = attacker.AttackStats.ShootingStats.ShotsPerBurst;

        attacker.WindUpTimer = attacker.AttackStats.ShootingStats.WindUpTime;
        attacker.ShootTimer = 0;
        attacker.BurstTimer = 0;
    }
}
