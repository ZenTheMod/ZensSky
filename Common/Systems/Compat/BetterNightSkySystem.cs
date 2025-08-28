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
using static ZensSky.Common.Systems.Space.StarHooks;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonHooks;
using BetterNightSystem = BetterNightSky.BetterNightSky.BetterNightSkySystem;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("BetterNightSky")]
[ExtendsFromMod("BetterNightSky")]
[Autoload(Side = ModSide.Client)]
public sealed class BetterNightSkySystem : ModSystem
{
    #region Private Fields

    private delegate void orig_On_Main_DrawStarsInBackground(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial);
    private static Hook? RemoveSecondaryOrig;

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

        // QueueMainThreadAction can be ignored as this mod is loaded first regardless.
    public override void Load()
    {
        IsEnabled = true;

        PostDrawStars += StarsSpecialPostDraw;

        ModifyMoonTexture += UseBigMoonTexture;

        MethodInfo? on_Main_DrawStarsInBackground = typeof(BetterNightSky.BetterNightSky).GetMethod(nameof(On_Main_DrawStarsInBackground), NonPublic | Static);

        if (on_Main_DrawStarsInBackground is not null)
            RemoveSecondaryOrig = new(on_Main_DrawStarsInBackground,
                DoubleDetour);

                // This is placed before the following check purely for a strange bugfix.
            // When using our moon rework the scale is derived from the texture for accuracy,
            // however this mods replaces every moon asset; This is also alarming as the resetting of these textures is derived from an unclamped asset replacement (https://github.com/IDGCaptainRussia94/BetterNightSky/blob/master/BetterNightSky.cs#L439).
            // Although I'm likely the only person in the world who cares about this.
        MethodInfo? doUnloads = typeof(BetterNightSystem).GetMethod(nameof(BetterNightSystem.DoUnloads), Public | Instance);

        if (doUnloads is not null)
            PatchUnload = new(doUnloads,
                JumpReset);

        if (!SkyConfig.Instance.SunAndMoonRework)
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
        RemoveSecondaryOrig?.Dispose();

        PatchLoad?.Dispose();
        PatchUnload?.Dispose();

        PatchConfigReloading?.Dispose();
        PatchNeedsReload?.Dispose();
    }

    #endregion

    #region Stars

    private void DoubleDetour(orig_On_Main_DrawStarsInBackground orig, On_Main.orig_DrawStarsInBackground origorig, Main self, Main.SceneArea sceneArea, bool artificial) =>
        origorig(self, sceneArea, artificial);

    #endregion

    #region Skip Moon Replacement

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

    private void JumpReset(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel skipLoopTarget = c.DefineLabel();

                // Unsure of the cause but occiasionally Main.DrawStar throws IndexOutOfRangeException, I'm hoping this fixes it.
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(5)))
            {
                c.EmitPop();

                c.EmitLdcI4(4);
            }

            if (!SkyConfig.Instance.SunAndMoonRework)
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
