using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config.Elements;

    // TODO: API for other mods to be able to lock our config elements.
public interface ILockedConfigElement
{
    #region Private Fields

    private const string LockTooltipKey = "LockReason";

    #endregion

    #region Private Properties

    protected object? TargetInstance { get; set; }

    protected PropertyFieldWrapper? TargetMember { get; set; }

    #endregion

    #region Public Properties

    public sealed bool IsLocked =>
        (bool?)TargetMember?.GetValue(TargetInstance) ?? false;

    #endregion

    #region Public Methods

    public sealed void InitializeLockedElement(ConfigElement @this)
    {
        LockedElementAttribute? attri =
            ConfigManager.GetCustomAttributeFromMemberThenMemberType<LockedElementAttribute>(@this.MemberInfo, @this.Item, @this.List);

        if (attri is null)
            return;

        Type type = attri.TargetType;

        string name = attri.MemberName;

        FieldInfo? field = type.GetField(name, Static | Instance | Public | NonPublic);
        PropertyInfo? property = type.GetProperty(name, Static | Instance | Public | NonPublic);

        if (field is not null)
            TargetMember = new(field);
        else
            TargetMember = new(property);

        TargetInstance = null;

        if (!TargetMember.IsStatic)
        {
            if (ConfigManager.Configs.TryGetValue(ModContent.GetInstance<ZensSky>(), out List<ModConfig>? value))
                TargetInstance = value.Find(c => c.Name == type.Name);
            else if (Utilities.TryGetInstance(type, out object? instance))
                TargetInstance = instance;
        }

        string tooltip =
            ConfigManager.GetLocalizedTooltip(@this.MemberInfo);

        string? lockReason =
            ConfigManager.GetLocalizedText<LockedKeyAttribute, LockedArgsAttribute>(@this.MemberInfo, LockTooltipKey);

        @this.TooltipFunction = () =>
            tooltip +
            (IsLocked && lockReason is not null ?
            (string.IsNullOrEmpty(tooltip) ? string.Empty : "\n") +
            $"[c/{Color.Red.Hex3()}:" + lockReason + "]" :
            string.Empty);
    }

    #endregion
}
