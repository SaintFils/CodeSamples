using Unity.Entities;

namespace ECSTest.Components
{
    public struct DestroyComponent : IComponentData
    {
        public bool IsNeedToDestroy;
        public float DestroyDelay;
    }
}