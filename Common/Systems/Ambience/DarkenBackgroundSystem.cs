using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Compat;
using ZensSky.Common.Systems.Stars;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Ambience;

[Autoload(Side = ModSide.Client)]
public sealed class DarkenBackgroundSystem : ModSystem
{
    #region Private Fields

    private static ILHook? ModifySunLightOnMenu;

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() =>
        {
            MethodInfo? modifySunLightColor = typeof(SystemLoader).GetMethod(nameof(SystemLoader.ModifySunLightColor));

            if (modifySunLightColor is not null)
                ModifySunLightOnMenu = new(modifySunLightColor,
                    AllowSunLightColor);
        });

        IL_Main.DrawBlack += PreventDrawBlackOverAir;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() => 
            ModifySunLightOnMenu?.Dispose());

        IL_Main.DrawBlack -= PreventDrawBlackOverAir;
    }

    #endregion

    private void AllowSunLightColor(ILContext il)
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

    private void PreventDrawBlackOverAir(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? breakTarget = c.DefineLabel();

            int tileIndex = -1;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdloc(out _),
                i => i.MatchBrtrue(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchBrfalse(out breakTarget),
                i => i.MatchLdsfld<Main>(nameof(Main.drawToScreen)));

            c.GotoPrev(MoveType.After,
                i => i.MatchLdloc(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchCall<Tilemap>("get_Item"),
                i => i.MatchStloc(out tileIndex));

            c.EmitLdloc(tileIndex);

                // If there is no tile then break.
            c.EmitDelegate(static (Tile tile) =>
                !tile.HasTile && tile.WallType == WallID.None);

            c.EmitBrtrue(breakTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (!SkyConfig.Instance.PitchBlackBackground || DarkSurfaceSystem.IsEnabled)
            return;

        float interpolator = MathF.Pow(StarSystem.StarAlpha, 3);

        backgroundColor = Color.Lerp(Main.ColorOfTheSkies, Color.Black, interpolator);
        tileColor = Color.Lerp(Main.ColorOfTheSkies, Color.Black, interpolator);
    }
}
