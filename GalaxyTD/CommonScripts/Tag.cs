using Sirenix.OdinInspector;
using System;

[Serializable]
public abstract class Tag : SerializedScriptableObject
{
    public abstract string GetDescription();
}
