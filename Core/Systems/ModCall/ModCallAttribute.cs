using System;

namespace ZensSky.Core.Systems.ModCall;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ModCallAttribute : Attribute
{
    public string[] AlternameNames;

    public ModCallAttribute() =>
        AlternameNames = [];

    public ModCallAttribute(params string[] alternameNames) =>
        AlternameNames = alternameNames;
}
