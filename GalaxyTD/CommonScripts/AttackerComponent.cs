using ECSTest.Structs;
using Unity.Entities;
using UnityEngine;
using static AllEnums;

namespace ECSTest.Components
{
    public struct AttackerComponent : IComponentData, IEnableableComponent
    {
        public TowerId TowerType;
        public AttackPattern AttackPattern;

        public float CurrentDeviation;

        public float AttackDelay;
        public float BurstTimer;
        public float ReloadTimer;
        public float WindUpTimer;
        public float ShootTimer;
        public float IdleTimer;
        public float IdleRandomizer;
        public float StartOffset;

        public int Bullets;
        public int BulletLeftInCurrentBurst;
        public int Level;

        public Entity Target;

        public AttackStatsStruct AttackStats;

        public bool AutoReload;

        public AttackerComponent(Tower towerPrototype)
        {
            AttackStats = towerPrototype.AttackStats.GetStats();
            AttackPattern = towerPrototype.AttackStats.ShootingStats.GetNextAvailableAttackPattern(AttackPattern.Off);
            Bullets = towerPrototype.AttackStats.ReloadStats.MagazineSize;
            TowerType = towerPrototype.TowerId;
            BulletLeftInCurrentBurst = towerPrototype.AttackStats.ShootingStats.ShotsPerBurst;
            StartOffset = towerPrototype.StartOffset;

            CurrentDeviation = AttackDelay = BurstTimer = 0;
            ReloadTimer = ShootTimer = IdleTimer = 0;
            WindUpTimer = AttackStats.ShootingStats.WindUpTime;
            IdleRandomizer = Random.Range(1.0f, 2.0f);

            AutoReload = towerPrototype.AutoReload;

            Level = 0;
            Target = Entity.Null;
        }
    }
}