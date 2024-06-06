using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RecyclingTag : OnReloadTag
{
    [SerializeField, InfoBox("Percent from BulletCost. 100% is 1. Positive numbers for decrease BulletCost, negative increase")]
    private float decreasePercent = 0.01f;

    public override void OnReload(Entity tower, EntityManager manager)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        
        float decreaseAmount = attackerComponent.AttackStats.ReloadStats.BulletCost * decreasePercent;
        attackerComponent.AttackStats.ReloadStats.BulletCost = math.max(0, attackerComponent.AttackStats.ReloadStats.BulletCost - decreaseAmount);
        
        manager.SetComponentData(tower, attackerComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/Recycling").Replace("{param}", (decreasePercent * 100).ToString() + "<color=#1fb2de>%</color>");
}