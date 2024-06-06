using System.Collections.Generic;
using Unity.Entities;

public sealed class TagsComponent : IComponentData
{
    public List<Tag> Tags;
    public TagsComponent()
    {
        Tags = new List<Tag>();
    }
}
