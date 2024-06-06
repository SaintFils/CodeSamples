using Unity.Entities;

namespace ECSTest.Components
{
    public struct CreepComponent : IComponentData, IEnableableComponent
    {
        public int FullReward => (int) (CashReward * CashRewardMultiplayer);
        public int CashReward;
        public int WaveNumber;
        public float CashRewardMultiplayer;
        public float MaxHp;
        public float Hp;
        public float Mass;
        public bool Escaped;
        public bool IsCaringRelic;
    }
}