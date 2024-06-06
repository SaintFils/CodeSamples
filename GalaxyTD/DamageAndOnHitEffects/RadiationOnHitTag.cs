using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class RadiationOnHitTag : OnHitTag
{
    [SerializeField, InfoBox("Percent of damage to creep. 100% is 1")] private float percentOfDamageToRadiation = .1f;
    [SerializeField] private float dotTime = 5;

    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        if(!manager.Exists(onHitTagData.CreepEntity))
            return;
        
        Entity targetEntity = onHitTagData.CreepEntity;
        RadiationComponent radiationComponent = manager.GetComponentData<RadiationComponent>(targetEntity);

        radiationComponent.DPS = damage * percentOfDamageToRadiation;
        radiationComponent.Time = dotTime;

        manager.SetComponentData(targetEntity, radiationComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/Radiation");
}