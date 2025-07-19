using System;

namespace ZensSky.Core.Systems.ModCall;

/// <summary>
/// Adds this method to <see cref="ModCallSystem.Handlers"/> under its name and <see cref="NameAliases"/> if provided.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ModCallAttribute : Attribute
{
    public string[] NameAliases;

    public ModCallAttribute() =>
        NameAliases = [];

    public ModCallAttribute(params string[] nameAliases) =>
        NameAliases = nameAliases;
}
