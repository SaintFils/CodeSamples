using Unity.Entities;

public abstract class OnReloadTag : Tag
{
    public abstract void OnReload(Entity tower, EntityManager manager); 
}