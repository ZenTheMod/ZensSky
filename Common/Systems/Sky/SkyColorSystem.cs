using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Compat;
using ZensSky.Common.Systems.Sky.Space;
using ZensSky.Core;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Utils;
using hook_ModifySunLightColor = Terraria.ModLoader.SystemLoader.DelegateModifySunLightColor;

namespace ZensSky.Common.Systems.Sky;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="LightingInMenu"/><br/>
///         Allows <see cref="ModSystem.ModifySunLightColor"/> to run on the main menu using <see cref="ModifyInMenu"/>.
///     </item>
///     <item>
///         <see cref="DrawBlackNonSolid"/><br/>
///         Fixes multiple issues where DrawBlack would render over non-solid/edge tiles/walls.
///     </item>
///     <item>
///         <see cref="DrawTilesNonSolid"/><br/>
///         Forces non-solid/edge tiles to draw regardless of low light.
///     </item>
///     <item>
///         <see cref="DrawWallsNonSolid"/><br/>
///         Forces non-solid/edge walls to draw regardless of low light.
///     </item>
/// </list>
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SkyColorSystem : ModSystem
{
    #region Private Fields

    private delegate void orig_ModifySunLightColor(ref Color tileColor, ref Color backgroundColor);

    private static Hook? PatchSunLightColor;

    #endregion

    #region Public Events

    public static event hook_ModifySunLightColor? ModifyInMenu;

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() =>
        {
            MethodInfo? modifySunLightColor = typeof(SystemLoader).GetMethod(nameof(SystemLoader.ModifySunLightColor));

            if (modifySunLightColor is not null)
                PatchSunLightColor = new(modifySunLightColor,
                    LightingInMenu);
        });

        ModifyInMenu += ModifySunLightColor;

        IL_Main.DrawBlack += DrawBlackNonSolid;

        IL_TileDrawing.DrawSingleTile += DrawTilesNonSolid;

        IL_WallDrawing.DrawWalls += DrawWallsNonSolid;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() => 
            PatchSunLightColor?.Dispose());

        ModifyInMenu = null;

        IL_Main.DrawBlack -= DrawBlackNonSolid;

        IL_TileDrawing.DrawSingleTile -= DrawTilesNonSolid;

        IL_WallDrawing.DrawWalls -= DrawWallsNonSolid;
    }

    #endregion

    #region Invoke Lighting

    private void LightingInMenu(orig_ModifySunLightColor orig, ref Color tileColor, ref Color backgroundColor)
    {
        orig(ref tileColor, ref backgroundColor);

        if (Main.gameMenu)
            ModifyInMenu?.Invoke(ref tileColor, ref backgroundColor);
    }

    #endregion

    #region DrawBlack Fixes

    private void DrawBlackNonSolid(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? breakTarget = c.DefineLabel();

            int tileXIndex = -1;
            int tileYIndex = -1;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdloc(out _),
                i => i.MatchBrtrue(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchBrfalse(out breakTarget),
                i => i.MatchLdsfld<Main>(nameof(Main.drawToScreen)));

            c.GotoPrev(MoveType.After,
                i => i.MatchLdsflda<Main>(nameof(Main.tile)),
                i => i.MatchLdloc(out tileXIndex),
                i => i.MatchLdloc(out tileYIndex),
                i => i.MatchCall<Tilemap>("get_Item"),
                i => i.MatchStloc(out _));

            c.EmitLdloc(tileXIndex);
            c.EmitLdloc(tileYIndex);

            c.EmitCall(Utilities.IgnoresDrawBlack);

            c.EmitBrtrue(breakTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region TileDrawing Fixes

    private void DrawTilesNonSolid(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int tileXIndex = -1; // arg
            int tileYIndex = -1; // arg

            c.GotoNext(
                i => i.MatchLdsflda<Main>(nameof(Main.tile)),
                i => i.MatchLdarg(out tileXIndex),
                i => i.MatchLdarg(out tileYIndex),
                i => i.MatchCall<Tilemap>("get_Item"));

            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg(out _),
                i => i.MatchLdflda<TileDrawInfo>(nameof(TileDrawInfo.tileLight)),
                i => i.MatchCall<Color>($"get_{nameof(Color.R)}"),
                i => i.MatchLdcI4(1),
                i => i.MatchBge(out _));

            c.GotoPrev(MoveType.Before,
                i => i.MatchStloc(out _));

            c.EmitPop();

            c.EmitLdarg(tileXIndex);
            c.EmitLdarg(tileYIndex);

            c.EmitCall(Utilities.IgnoresDrawBlack);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region WallDrawing Fixes

    private void DrawWallsNonSolid(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? jumpColorCheckTarget = c.DefineLabel();

            int tileXIndex = -1;
            int tileYIndex = -1;

            int colorIndex = -1;

            c.GotoNext(
                i => i.MatchLdloc(out tileXIndex),
                i => i.MatchLdloc(out tileYIndex),
                i => i.MatchCall<Terraria.Lighting>(nameof(Terraria.Lighting.GetColor)),
                i => i.MatchStloc(out colorIndex));

            c.GotoNext(MoveType.Before,
                i => i.MatchLdloca(colorIndex),
                i => i.MatchCall<Color>($"get_{nameof(Color.R)}"),
                i => i.MatchBrtrue(out jumpColorCheckTarget));

            c.MoveAfterLabels();

            c.EmitLdloc(tileXIndex);
            c.EmitLdloc(tileYIndex);

            c.EmitCall(Utilities.IgnoresDrawBlack);

            c.EmitBrtrue(jumpColorCheckTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Lighting

    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (!SkyConfig.Instance.PitchBlackBackground || DarkSurfaceSystem.IsEnabled)
            return;

        float interpolator = Easings.InCubic(StarSystem.StarAlpha);

        backgroundColor = Color.Lerp(Main.ColorOfTheSkies, Color.Black, interpolator);
        tileColor = Color.Lerp(Main.ColorOfTheSkies, Color.Black, interpolator);
    }

    #endregion
}
