using ECSTest.Components;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static AllEnums;
using Random = UnityEngine.Random;

public static class DamageSystem
{
    private const float damageDropDistance = 1f;
    private const float knockBackDamageK = 0.5f;
    private const float minPercentDamage = .25f;

    public static void DoDamageByWall(EntityManager manager, KnockBackWallDamageEvent wallDamageEvent, RefRW<CashComponent> cashComponent, EntityCommandBuffer ecb)
    {
        if (!manager.Exists(wallDamageEvent.Creep))
            return;

        CreepComponent creepComponent = manager.GetComponentData<CreepComponent>(wallDamageEvent.Creep);
        SharedCreepData sharedCreepData = manager.GetSharedComponent<SharedCreepData>(wallDamageEvent.Creep);

        if (wallDamageEvent.Speed == float.MaxValue)
        {
            DoInstantKill(ref creepComponent, wallDamageEvent.Position, ecb);
        }
        else
        {
            float damage = GetKnockBackDamage(wallDamageEvent.Speed, creepComponent.Mass);
            creepComponent.Hp -= damage;
            ShowDamageText(damage, wallDamageEvent.Position, ecb);
        }

        manager.SetComponentData(wallDamageEvent.Creep, creepComponent);

        DestroyComponent destroyComponent = manager.GetComponentData<DestroyComponent>(wallDamageEvent.Creep);
        if (destroyComponent.IsNeedToDestroy)
            return;

        CheckCreepDeath(wallDamageEvent.Creep, manager, cashComponent, creepComponent, wallDamageEvent.Position, ecb, sharedCreepData.FleshType, sharedCreepData.ArmorType);
    }

    public static void DoInstantKill(ref CreepComponent creepComponent, float2 position, EntityCommandBuffer ecb) 
    {
        float damage = creepComponent.Hp + creepComponent.MaxHp; 
        creepComponent.Hp = -creepComponent.MaxHp;

        ShowTagEffect(ecb, position, 0, TagEffectType.InstantKill, "InstantKillEffectEvent");
        ShowDamageText(damage, position, ecb);
    }

    public static void DoTagAoeDamage(CreepsLocator creepsLocator, float2 position, float aoe, float damage, float knockback, Entity tower, EntityManager manager, RefRW<CashComponent> cashComponent,
        EntityCommandBuffer ecb)
    {
        NativeList<CreepInfo> creeps = new(Allocator.Temp);
        creepsLocator.LocateNearestCreeps(position, aoe, ref creeps, 20);

        for (int i = 0; i < creeps.Length; i++)
        {
            DestroyComponent destroyComponent = manager.GetComponentData<DestroyComponent>(creeps[i].Entity);
            if (destroyComponent.IsNeedToDestroy)
                continue;

            CalculateAoeKnockbackAndDamage(creeps[i].Position, position, aoe, knockback, damage, out float damageFomDistance, out float2 knockbackDirection);

            CreepComponent creepComponent = manager.GetComponentData<CreepComponent>(creeps[i].Entity);

            OnDoDamageToCreep(ref creepComponent, creeps[i].FleshType, knockbackDirection, damageFomDistance, manager, creeps[i].Entity, tower, creeps[i].Position, ecb);
            manager.SetComponentData(creeps[i].Entity, creepComponent);
            CheckCreepDeath(creeps[i].Entity, manager, cashComponent, creepComponent, creeps[i].Position, ecb, creeps[i].FleshType, creeps[i].ArmorType);
        }

        ShowTagEffect(ecb, position, aoe, TagEffectType.AoeDamage, "AoeEffectEvent");
    }

    public static void ShowTagEffect(EntityCommandBuffer ecb, float2 point, float range, TagEffectType type, string name)
    {
        Entity aoeEffectEntity = ecb.CreateEntity();
        ecb.SetName(aoeEffectEntity, name);
        ecb.AddComponent(aoeEffectEntity, new TagEffectEvent {Point = point, AoeRange = range, EffectType = type});
    }

    public static void DoAOEDamage(CreepsLocator creepsLocator, EntityManager manager, AOECollisionEvent collisionAoeEvent, RefRW<CashComponent> cashComponent, EntityCommandBuffer ecb)
    {
        Entity attackerEntity = collisionAoeEvent.Tower;

        if (attackerEntity == Entity.Null || !manager.Exists(attackerEntity))
        {
            Debug.LogWarning("attackerEntity is null");
            return;
        }

        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(attackerEntity);

        NativeList<CreepInfo> creeps = new(Allocator.Temp);
        creepsLocator.LocateNearestCreeps(collisionAoeEvent.Point, collisionAoeEvent.AOE, ref creeps, 20);

        TagsComponent attackerTagsComponent = manager.GetComponentData<TagsComponent>(attackerEntity);

        for (int i = 0; i < creeps.Length; i++)
        {
            CreepComponent creepComponent = manager.GetComponentData<CreepComponent>(creeps[i].Entity);
            SharedCreepData sharedCreepData = manager.GetSharedComponent<SharedCreepData>(creeps[i].Entity);

            CalculateAoeKnockbackAndDamage(creeps[i].Position, collisionAoeEvent.Point, collisionAoeEvent.AOE, attackerComponent.AttackStats.KnockBackPerBullet, collisionAoeEvent.Damage,
                out float damageFomDistance, out float2 knockbackDirection);

            CheckDamageModifiers(attackerEntity, sharedCreepData, ref damageFomDistance, manager);

            OnHitTagData onHitTagData = new OnHitTagData {AoeCollisionEvent = collisionAoeEvent};
            OnHit(onHitTagData, ref creepComponent, ref damageFomDistance, attackerEntity, attackerTagsComponent.Tags, manager, ecb);
            manager.SetComponentData(creeps[i].Entity, creepComponent);

            OnDoDamageToCreep(ref creepComponent, sharedCreepData.FleshType, knockbackDirection, damageFomDistance, manager, creeps[i].Entity, attackerEntity, creeps[i].Position, ecb);

            DestroyComponent destroyComponent = manager.GetComponentData<DestroyComponent>(creeps[i].Entity);
            if (destroyComponent.IsNeedToDestroy)
                continue;

            OnKill(ref creepComponent, sharedCreepData, creeps[i].Entity, attackerEntity, attackerTagsComponent.Tags, cashComponent, manager, ecb);
            manager.SetComponentData(creeps[i].Entity, creepComponent);
            CheckCreepDeath(creeps[i].Entity, manager, cashComponent, creepComponent, creeps[i].Position, ecb, sharedCreepData.FleshType, sharedCreepData.ArmorType);
        }
    }

    private static void CalculateAoeKnockbackAndDamage(float2 creepPosition, float2 aoePosition, float aoeRange, float knockback, float damage, out float damageFomDistance,
        out float2 knockbackDirection)
    {
        float distanceToCreep = math.length(creepPosition - aoePosition);
        float aoeDistanceDecrease = 1 - (1 - minPercentDamage) * distanceToCreep / aoeRange;
        float2 attackDirection = creepPosition.Equals(aoePosition) ? aoeRange * Random.Range(0f, 1f) : aoeRange * math.normalize(creepPosition - aoePosition);
        knockbackDirection = attackDirection * knockback * aoeDistanceDecrease;
        damageFomDistance = aoeDistanceDecrease * damage;
    }

    public static void DoDamage(GunCollisionEvent collisionEvent, EntityManager manager, RefRW<CashComponent> cashComponent, EntityCommandBuffer ecb)
    {
        if (!manager.Exists(collisionEvent.Target) || !manager.Exists(collisionEvent.Tower))
            return;

        Entity attacker = collisionEvent.Tower;
        float damage = collisionEvent.Damage;
        CreepComponent creepComponent = manager.GetComponentData<CreepComponent>(collisionEvent.Target);
        SharedCreepData sharedCreepData = manager.GetSharedComponent<SharedCreepData>(collisionEvent.Target);

        if (attacker == Entity.Null)
        {
            OnDoDamageToCreep(ref creepComponent, sharedCreepData.FleshType, 0, damage, manager, collisionEvent.Target, Entity.Null, collisionEvent.Point, ecb);
        }
        else
        {
            TagsComponent tagsComponent = manager.GetComponentData<TagsComponent>(attacker);

            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(attacker);
            GunStatsComponent gunStatsComponent = manager.GetComponentData<GunStatsComponent>(attacker);

            damage = GetDamageAfterDistance(damage, gunStatsComponent.DamageFallof, collisionEvent.DistanceTraveled);
            CheckDamageModifiers(attacker, sharedCreepData, ref damage, manager);

            float2 knockbackDirection = collisionEvent.CollisionDirection * attackerComponent.AttackStats.KnockBackPerBullet;

            PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(collisionEvent.Target);

            OnHitTagData onHitTagData = new OnHitTagData {GunCollisionEvent = collisionEvent, CreepEntity = collisionEvent.Target, CashComponentRefRw = cashComponent};
            OnHit(onHitTagData, ref creepComponent, ref damage, attacker, tagsComponent.Tags, manager, ecb);
            manager.SetComponentData(collisionEvent.Target, creepComponent);
           
            OnDoDamageToCreep(ref creepComponent, sharedCreepData.FleshType, knockbackDirection, damage, manager, collisionEvent.Target, attacker, positionComponent.Position, ecb);

            DestroyComponent destroyComponent = manager.GetComponentData<DestroyComponent>(collisionEvent.Target);
            if (destroyComponent.IsNeedToDestroy)
                return;

            creepComponent = manager.GetComponentData<CreepComponent>(collisionEvent.Target);
            OnKill(ref creepComponent, sharedCreepData, collisionEvent.Target, attacker, tagsComponent.Tags, cashComponent, manager, ecb);
        }

        manager.SetComponentData(collisionEvent.Target, creepComponent);
        CheckCreepDeath(collisionEvent.Target, manager, cashComponent, creepComponent, collisionEvent.Point, ecb, sharedCreepData.FleshType, sharedCreepData.ArmorType);
    }

    private static void CheckDamageModifiers(Entity attacker, SharedCreepData sharedCreepData, ref float damage, EntityManager manager)
    {
        DamageModifiers damageModifiersComponent = manager.GetComponentData<DamageModifiers>(attacker);
        float oldDamage = damage;
        
        switch (sharedCreepData.ArmorType)
        {
            case ArmorType.Unarmored:
                damage += oldDamage * damageModifiersComponent.DamageToUnarmored;
                break;
            case ArmorType.Light:
                damage += oldDamage * damageModifiersComponent.DamageToLight;
                break;
            case ArmorType.Heavy:
                damage += oldDamage * damageModifiersComponent.DamageToHeavy;
                break;
        }

        switch (sharedCreepData.FleshType)
        {
            case FleshType.Bio:
                damage += oldDamage * damageModifiersComponent.DamageToBio;
                break;
            case FleshType.Mech:
                damage += oldDamage * damageModifiersComponent.DamageToMechanical;
                break;
            case FleshType.Energy:
                damage += oldDamage * damageModifiersComponent.DamageToEnergy;
                break;
        }
    }

    private static void OnDoDamageToCreep(ref CreepComponent creepComponent, FleshType fleshType, float2 knockbackDirection, float damage, EntityManager manager, Entity creepEntity,
        Entity towerEntity, float2 position, EntityCommandBuffer ecb)
    {
        Knockback knockback = manager.GetComponentData<Knockback>(creepEntity);

        knockback.Direction += knockbackDirection / creepComponent.Mass;

        if (towerEntity != Entity.Null)
        {
            AttackerStatisticComponent towerStatistics = manager.GetComponentData<AttackerStatisticComponent>(towerEntity);

            if (creepComponent.Hp > 0)
            {
                towerStatistics.CreepDamage += (creepComponent.Hp - damage) switch
                {
                    <= 0 => creepComponent.Hp, // E.g. Creep(100) - Damage(200) = (-100) then result is 100 (Creep.Hp)
                    _ => damage
                };

                if (damage > creepComponent.Hp && creepComponent.Hp > 0)
                    towerStatistics.OverheatDamage += (damage - creepComponent.Hp);
            }

            manager.SetComponentData(towerEntity, towerStatistics);
        }

        creepComponent.Hp -= damage;
        creepComponent.Hp = math.min(creepComponent.MaxHp, creepComponent.Hp);

        manager.SetComponentData(creepEntity, creepComponent);
        manager.SetComponentData(creepEntity, knockback);

        if (manager.HasComponent<AnimationComponent>(creepEntity))
        {
            AnimationComponent animation = manager.GetComponentData<AnimationComponent>(creepEntity);
            animation.DamageTaken = true;
            manager.SetComponentData(creepEntity, animation);
        }

        ShowDamageText(damage, position, ecb);

        MusicManager.PlayCreepHit(fleshType, position);
    }

    private static void ShowDamageText(float damage, float2 position, EntityCommandBuffer ecb)
    {
        if (damage <= 0) return;

        Entity damageEntity = ecb.CreateEntity();
        ecb.SetName(damageEntity, "AnimatedText");
        ecb.AddComponent(damageEntity,
            new AnimatedTextComponent
            {
                NonCashValue = (int)damage,
                Position = new float2(position.x, position.y + GameServices.Instance.RenderDataHolder.TextAnimationData.DamagePopUpYStartOffset),
                Timer = 0,
                Color = GameServices.Instance.RenderDataHolder.TextAnimationData.DamageTextColor,
                TextType = TextType.Damage
            });
    }

    private static void CheckCreepDeath(Entity target, EntityManager manager, RefRW<CashComponent> cashComponent, CreepComponent creepComponent, float2 position, EntityCommandBuffer ecb,
        FleshType fleshType, ArmorType armorType)
    {
        if (creepComponent.Hp > 0) return;

        MusicManager.PlayCreepDeath(fleshType, armorType, position);

        DestroyCreep(manager, target);
        int creepCashReward = (int)math.round(creepComponent.FullReward);
        cashComponent.ValueRW.CreepDestroyCashChange(creepCashReward, ecb, position);
    }

    public static void DestroyCreep(EntityManager manager, Entity creep)
    {
        DestroyComponent destroyComponent = manager.GetComponentData<DestroyComponent>(creep);
        if (!destroyComponent.IsNeedToDestroy)
            manager.SetComponentData(creep, new DestroyComponent {IsNeedToDestroy = true, DestroyDelay = 2});
        manager.SetComponentEnabled<CreepComponent>(creep, false);
    }

    public static void DestroyCreep(EntityCommandBuffer commandBuffer, Entity creep, ref DestroyComponent destroyComponent)
    {
        if (!destroyComponent.IsNeedToDestroy)
        {
            destroyComponent.IsNeedToDestroy = true;
            destroyComponent.DestroyDelay = 2;
        }
        commandBuffer.SetComponentEnabled<CreepComponent>(creep, false);
    }

    private static void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, List<Tag> tags, EntityManager manager, EntityCommandBuffer ecb)
    {
        try
        {
            for (int index = 0; index < tags.Count; index++)
            {
                onHitTagData.TagIndex = index;
                Tag tag = tags[index];
                if (tag is OnHitTag hitTag)
                    hitTag.OnHit(onHitTagData, ref creepComponent, ref damage, tower, manager, ecb);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"-> error in OnHit tags: {e}");
        }
    }

    private static float GetDamageAfterDistance(float damage, float damageFalloff, float distanceTraveled)
    {
        return damage * Mathf.Pow(damageFalloff, distanceTraveled / damageDropDistance);
    }

    private static void OnKill(ref CreepComponent creepComponent, SharedCreepData sharedData, Entity creep, Entity tower, List<Tag> tags, RefRW<CashComponent> cashComponent, EntityManager manager,
        EntityCommandBuffer ecb)
    {
        if (creepComponent.Hp > 0) return;

        AttackerStatisticComponent statistics = manager.GetComponentData<AttackerStatisticComponent>(tower);
        statistics.Kills++;
        manager.SetComponentData(tower, statistics);

        manager.CompleteDependencyBeforeRW<CreepsLocator>();
        EntityQuery query = manager.CreateEntityQuery(typeof(CreepsLocator));
        CreepsLocator creepsLocator = query.GetSingleton<CreepsLocator>();

        OnKillData handler = new OnKillData(creep, tower, cashComponent, creepsLocator, manager, ecb, sharedData);

        try
        {
            foreach (Tag tag in tags)
                if (tag is OnKillTag killTag)
                    killTag.OnKill(handler, ref creepComponent);
        }
        catch (Exception e)
        {
            Debug.LogError($"-> error in OnKill tags: {e}");
        }
    }

    private static float GetKnockBackDamage(float speed, float mass)
    {
        return speed * speed * mass * knockBackDamageK;
    }
}