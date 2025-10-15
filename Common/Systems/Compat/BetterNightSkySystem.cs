using BetterNightSky;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using ZensSky.Common.Config;
using ZensSky.Core.Exceptions;
using static BetterNightSky.BetterNightSky;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.Sky.Space.StarHooks;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonHooks;
using BetterNightSystem = BetterNightSky.BetterNightSky.BetterNightSkySystem;

namespace ZensSky.Common.Systems.Compat;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="On_Main_DrawStarsInBackground"/><br/>
///         Deliberately unapply this hook to use our star rendering.
///     </item>
///     <item>
///         <see cref="JumpReset"/><br/>
///         Fix the resetting of star types to not index a texture out of bounds;
///         additionally stops the replacement of moon textures when the Sun and Moon rework is active.
///     </item>
///     <item>
///         <see cref="JumpReplacement"/><br/>
///         Stops moon textures from being replaced when the Sun and Moon rework is active.
///     </item>
///     <item>
///         <see cref="NoReloading"/><br/>
///         Allows the config controling the large moon texture in this mod to be swapped freely when the Sun and Moon rework is active.
///     </item>
///     <item>
///         <see cref="IgnoreReload"/><br/>
///         Fixes the config menu for the above.
///     </item>
/// </list>
/// </summary>
[JITWhenModsEnabled("BetterNightSky")]
[ExtendsFromMod("BetterNightSky")]
[Autoload(Side = ModSide.Client)]
public sealed class BetterNightSkySystem : ModSystem
{
    #region Private Fields

    private static ILHook? PatchLoad;
    private static ILHook? PatchUnload;

    private static ILHook? PatchConfigReloading;

    private static ILHook? PatchNeedsReload;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    public static bool UseBigMoon 
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => NightConfig.Config.UseHighResMoon;
    }

    #endregion

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        PostDrawStars += StarsSpecialPostDraw;

        ModifyMoonTexture += UseBigMoonTexture;

        On_Main.DrawStarsInBackground -= On_Main_DrawStarsInBackground;

        MethodInfo? doUnloads = typeof(BetterNightSystem).GetMethod(nameof(BetterNightSystem.DoUnloads), Public | Instance);

        if (doUnloads is not null)
            PatchUnload = new(doUnloads,
                JumpReset);

        if (!SkyConfig.Instance.UseSunAndMoon)
            return;

        MethodInfo? onModLoad = typeof(BetterNightSystem).GetMethod(nameof(BetterNightSystem.OnModLoad), Public | Instance);

        if (onModLoad is not null)
            PatchLoad = new(onModLoad,
                JumpReplacement);

            // When using our moon rework the asset replacement is irrelevant and the reload is not required to activate the visual that is used.
        MethodInfo? onBind = typeof(ConfigElement).GetMethod(nameof(ConfigElement.OnBind), Public | Instance);

        if (onBind is not null)
            PatchConfigReloading = new(onBind,
                NoReloading);

        MethodInfo? needsReload = typeof(ModConfig).GetMethod(nameof(ModConfig.NeedsReload), Public | Instance);

        if (needsReload is not null)
            PatchNeedsReload = new(needsReload,
                IgnoreReload);
    }

    public override void Unload() 
    { 
        PatchLoad?.Dispose();
        PatchUnload?.Dispose();

        PatchConfigReloading?.Dispose();
        PatchNeedsReload?.Dispose();
    }

    #endregion

    #region Skip Moon Replacement

    private void JumpReset(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel skipLoopTarget = c.DefineLabel();

            // Make sure the reset star type does not try to index out of bounds.
            c.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(4));

            c.EmitPop();

            c.EmitLdcI4(3);

            c.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(5));

            c.EmitPop();

            c.EmitLdcI4(4);


            if (!SkyConfig.Instance.UseSunAndMoon)
                return;

            c.GotoNext(i => i.MatchRet());

            c.GotoPrev(MoveType.Before,
                i => i.MatchLdcI4(-1),
                i => i.MatchCall<Star>(nameof(Star.SpawnStars)));

            c.MarkLabel(skipLoopTarget);

            c.GotoPrev(MoveType.Before,
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out _),
                i => i.MatchBr(out _));

            c.EmitBr(skipLoopTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private void JumpReplacement(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<NightConfig>(nameof(NightConfig.Config)),
                i => i.MatchLdfld<NightConfig>(nameof(NightConfig.UseHighResMoon)));

            c.EmitPop();

            c.EmitLdcI4(0);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Minor Inconvenience

    private void NoReloading(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(i => i.MatchRet());

            c.GotoPrev(MoveType.After,
                i => i.MatchCall(typeof(ConfigManager).FullName ?? "Terraria.ModLoader.Config.ConfigManager", nameof(ConfigManager.GetCustomAttributeFromMemberThenMemberType)),
                i => i.MatchLdnull(),
                i => i.MatchCgtUn());

            c.EmitLdarg0();
            c.EmitDelegate((bool reloadRequired, ConfigElement element) =>
            {
                if (element.MemberInfo.IsField &&
                    element.MemberInfo.fieldInfo.Name == nameof(NightConfig.UseHighResMoon) &&
                    element.MemberInfo.Type == NightConfig.Config.UseHighResMoon.GetType())
                    return false;

                return reloadRequired;
            });
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private void IgnoreReload(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? loopStartTarget = c.DefineLabel();

            int memberInfoIndex = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchBr(out loopStartTarget),
                i => i.MatchLdloc(out _),
                i => i.MatchCallvirt<IEnumerator<PropertyFieldWrapper>>($"get_{nameof(IEnumerator<>.Current)}"),
                i => i.MatchStloc(out memberInfoIndex));

            c.EmitLdloc(memberInfoIndex);
            c.EmitDelegate((PropertyFieldWrapper memberInfo) =>
            {
                if (memberInfo.IsField &&
                    memberInfo.fieldInfo.Name == nameof(NightConfig.UseHighResMoon) &&
                    memberInfo.Type == NightConfig.Config.UseHighResMoon.GetType())
                    return false;

                return true;
            });

            c.EmitBrfalse(loopStartTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Drawing

        // TODO: Include other non 'Special' star drawing.
    public static void StarsSpecialPostDraw(SpriteBatch spriteBatch, in SpriteBatchSnapshot snapshot, float alpha, Matrix transform)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, snapshot.RasterizerState, RealisticSkySystem.ApplyStarShader(), snapshot.TransformMatrix);

            // This isn't ideal but it doesn't matter.
        int i = 0;

        CountStars();
        drawStarPhase = 1;

        Main.SceneArea sceneArea = new()
        {
            bgTopY = Main.instance.bgTopY,
            totalHeight = Main.screenHeight,
            totalWidth = Main.screenWidth,
            SceneLocalScreenPositionOffset = Vector2.Zero
        };

        foreach (Star star in Main.star.Where(s => s is not null && !s.hidden && SpecialStarType(s) && CanDrawSpecialStar(s)))
        {
            i++;
            Main.instance.DrawStar(ref sceneArea, alpha, Main.ColorOfTheSkies, i, star, false, false);
        }

        spriteBatch.End();
    }

    private static void UseBigMoonTexture(ref Asset<Texture2D> moon, bool nonEventMoon)
    {
        if (IsEnabled &&
            UseBigMoon &&
            nonEventMoon)
            moon = SkyTextures.BetterNightSkyMoon;

        return;
    }

    #endregion

    #region Public Methods

    public static void ModifyMoonScale(ref float scale) => AdjustMoonScaleMethod(ref scale);

    #endregion
}
