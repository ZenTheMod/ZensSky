using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
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
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() => 
            PatchSunLightColor?.Dispose());

        ModifyInMenu = null;

        IL_Main.DrawBlack -= PreventDrawBlackOverAir;
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

                // If there is no tile then break.
            c.EmitDelegate((Tile tile) =>
                !tile.HasTile && tile.WallType == WallID.None);

            c.EmitBrtrue(breakTarget);
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
