using Unity.Entities;
using static AllEnums;

public struct SharedCreepData : ISharedComponentData
{
    public float CollisionRange;
    public float MaxHp;
    public float MaxForce;
    public float Speed;
    public float NeighborRange;
    public CreepType CreepType;
    public ArmorType ArmorType;
    public FleshType FleshType;
    public ObstacleType ObstacleType;

    public SharedCreepData(CreepStats creep)
    {
        CreepType = creep.CreepType;
        CollisionRange = creep.CollisionRange;
        MaxHp = creep.MaxHP;
        MaxForce = creep.MaxForce;
        Speed = creep.Speed;
        NeighborRange = creep.NeighborRange;
        ObstacleType = creep.ObstacleType;
        ArmorType = creep.ArmorType;
        FleshType = creep.FleshType;
    }
}
