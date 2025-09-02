using System;
using System.Reflection;
using Terraria.ModLoader;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[ExtendsFromMod("HighFPSSupport")]
[Autoload(Side = ModSide.Client)]
public sealed class HighFPSSupportSystem : ModSystem
{
    #region Private Fields

    private static PropertyInfo? IsPartialTickInfo;

    #endregion

    #region Public Properties

    public static bool IsPartialTick =>
        (bool?)IsPartialTickInfo?.GetValue(null) ?? false;

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        if (!ModLoader.TryGetMod("HighFPSSupport", out Mod highFPSSupport))
            return;

        IsEnabled = true;

        Assembly highFPSAsm = highFPSSupport.Code;

        Type? tickRateModifier = highFPSAsm.GetType("HighFPSSupport.TickRateModifier");

        IsPartialTickInfo = tickRateModifier?.GetProperty("IsPartialTick", Public | Static);
        ArgumentNullException.ThrowIfNull(IsPartialTickInfo);
    }

    #endregion
}
