using System;
using Terraria.ModLoader.Config;

namespace ZensSky.Core.Config.Elements;

/// <summary>
/// "Locks" a config from being changed if the type's target member is true (target member MUST be of type <see cref="bool"/>!)<br/>
/// Requires a corresponding <see cref="ILockedConfigElement"/> config element.<br/>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LockedElementAttribute : CustomModConfigItemAttribute
{
    #region Public Properties

    public Type TargetType { get; init; }

    public string MemberName { get; init; }

    #endregion

    #region Public Constructors

    public LockedElementAttribute(Type configType, Type targetType, string memberName)
        : base(configType)
    {
        TargetType = targetType;

        MemberName = memberName;
    }

    #endregion
}
