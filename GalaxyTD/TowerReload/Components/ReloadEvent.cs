using Unity.Entities;

namespace ECSTest.Components
{
    public struct ReloadEvent : IComponentData, IEnableableComponent
    {
        public Entity Tower;
    }
}