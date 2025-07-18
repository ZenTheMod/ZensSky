using System;

namespace ZensSky.Core.Systems.ModCall;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ModCallAttribute : Attribute
{
    public string AlternameName;

    public ModCallAttribute() =>
        AlternameName = string.Empty;

    public ModCallAttribute(string alternameName) =>
        AlternameName = alternameName;
}
