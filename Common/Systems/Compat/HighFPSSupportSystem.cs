using System;
using System.Reflection;
using Terraria.ModLoader;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[Autoload(Side = ModSide.Client)]
public sealed class HighFPSSupportSystem : ModSystem
{
    #region Private Fields

    private static PropertyInfo? GetIsPartialTick;

    #endregion

    #region Public Properties

    public static bool IsPartialTick =>
        (bool?)GetIsPartialTick?.GetValue(null) ?? false;

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        if (!ModLoader.TryGetMod("HighFPSSupport", out Mod highFPSSupport))
            return;

        IsEnabled = true;

        Assembly fablessAsm = highFPSSupport.Code;

        Type? tickRateModifier = fablessAsm.GetType("HighFPSSupport.TickRateModifier");

        GetIsPartialTick = tickRateModifier?.GetProperty("IsPartialTick", Public | Static);
    }

    #endregion
}
