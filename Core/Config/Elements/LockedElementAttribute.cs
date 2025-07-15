using System;
using Terraria.ModLoader.Config;

namespace ZensSky.Core.Config.Elements;

/// <summary>
/// To be used with <see cref="CustomModConfigItemAttribute"/> for locking an element based on another config member.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LockedElementAttribute : Attribute
{
    public Type TargetConfig;

    public string MemberName { get; }

    public bool Mode { get; }

    public LockedElementAttribute(Type targetConfig, string memberName, bool mode)
    {
        TargetConfig = targetConfig;

        MemberName = memberName;

        Mode = mode;
    }
}
