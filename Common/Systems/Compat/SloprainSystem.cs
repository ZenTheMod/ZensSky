using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Sloprain.Common.Configs;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Core.Exceptions;
using SloprainSys = Sloprain.Common.Systems.SloprainSystem;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("Sloprain")]
[ExtendsFromMod("Sloprain")]
[Autoload(Side = ModSide.Client)]
public sealed class SloprainSystem : ModSystem
{
    #region Private Fields

    private static ILHook? PatchUpdateRain;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? getCanUpdateRain = typeof(SloprainSys).GetProperty(nameof(SloprainSys.CanUpdateRain), Public | Static)?.GetGetMethod();

        if (getCanUpdateRain is not null)
            PatchUpdateRain = new(getCanUpdateRain,
                AllowRainInMainMenu);
    }

    public override void Unload() =>
        PatchUpdateRain?.Dispose();

    #endregion

    private void AllowRainInMainMenu(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)));

            c.EmitPop();
            c.EmitLdcI4(0);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void QueueRain(Action action)
    {
        if (!RainConfig.Instance.PerformanceMode)
            return;

        SloprainSys.Queue(action, true);
    }
}
