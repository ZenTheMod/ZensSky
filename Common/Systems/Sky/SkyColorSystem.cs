using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
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
///         <see cref="PreventDrawBlackOverAir"/><br/>
///         Fixes an issue where DrawBlack would could air as solid(?) and would draw over it, hiding the background.
///     </item>
///     <item>
///         <see cref="DrawNonSolidTiles"/><br/>
///         Forces non-solid tiles to draw regardless of low light.
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

        IL_Main.DrawBlack += PreventDrawBlackOverAir;

        IL_TileDrawing.DrawSingleTile += DrawNonSolidTiles;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() => 
            PatchSunLightColor?.Dispose());

        ModifyInMenu = null;

        IL_Main.DrawBlack -= PreventDrawBlackOverAir;

        IL_TileDrawing.DrawSingleTile -= DrawNonSolidTiles;
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

            c.EmitDelegate((Tile tile) =>
                tile.BlocksLight);

            c.EmitBrfalse(breakTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region TileDrawing Fixes

    private void DrawNonSolidTiles(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int drawDataIndex = -1; // arg

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(out drawDataIndex),
                i => i.MatchLdflda<TileDrawInfo>(nameof(TileDrawInfo.tileLight)),
                i => i.MatchCall<Color>($"get_{nameof(Color.R)}"),
                i => i.MatchLdcI4(1),
                i => i.MatchBge(out _));

            c.GotoPrev(MoveType.Before,
                i => i.MatchStloc(out _));

            c.EmitPop();

            c.EmitLdarg(drawDataIndex);

            c.EmitDelegate((TileDrawInfo drawData) =>
                !drawData.tileCache.BlocksLight);
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
