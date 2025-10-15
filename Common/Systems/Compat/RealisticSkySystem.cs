using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RealisticSky;
using RealisticSky.Common.DataStructures;
using RealisticSky.Content;
using RealisticSky.Content.Atmosphere;
using RealisticSky.Content.Clouds;
using RealisticSky.Content.NightSky;
using RealisticSky.Content.Sun;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core.Utils;
using ZensSky.Core.Exceptions;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Sky.Space;
using ZensSky.Common.Systems.Sky.SunAndMoon;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.Sky.Space.StarHooks;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonHooks;

namespace ZensSky.Common.Systems.Compat;

/// <summary>
/// A poor taste fork attempting to implement the below can be found <a href="https://github.com/ZenTheMod/Realistic-Sky">here</a>.<br/><br/>
/// 
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="RemoveBias"/><br/>
///         Removes the restriction on sun/moon orbit, allowing once again for it to be dragged on the menu.
///     </item>
///     <item>
///         <see cref="StarRotation"/><br/>
///         Corrects Realistic Sky's star matrix to use <see cref="StarSystem.StarRotation"/>.
///     </item>
///     <item>
///         <see cref="GalaxyRotation"/><br/>
///         Corrects Realistic Sky's galaxy renderer to rotate around the center of the sky with <see cref="StarSystem.StarRotation"/>..
///     </item>
///     <item>
///         <see cref="CommonRequestsInvertedGravity"/>/<see cref="CommonShaderInvertedGravity"/> + <see cref="ModifySunPosition"/><br/>
///         Corrects various <see cref="ARenderTargetContentByRequest"/>s/renderers to correctly account for inverted gravity.
///     </item>
///     <item>
///         <see cref="DrawSky"/><br/>
///         Bulk patch to skip/modify/replace some rendering.
///     </item>
/// </list>
/// </summary>
[JITWhenModsEnabled("RealisticSky")]
[ExtendsFromMod("RealisticSky")]
[Autoload(Side = ModSide.Client)]
public sealed class RealisticSkySystem : ModSystem
{
    #region Private Fields

    private delegate void orig_VerticallyBiasSunAndMoon();
    private static Hook? RemoveBias;

    private static ILHook? PatchStarRotation;

    private static ILHook? PatchGalaxyRotation;

    private static ILHook? PatchAtmosphereTarget;
    private static ILHook? PatchCloudsTarget;
    private static ILHook? PatchCloudsShader;
    private static ILHook? PatchStarShader;

    private static ILHook? PatchDrawing;

    #endregion

    #region Public Properties

    public static bool CanDraw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => 
            RealisticSkyManager.CanRender &&
            !RealisticSkyManager.TemporarilyDisabled &&
            !(!RealisticSkyConfig.Instance.ShowInMainMenu &&
            Main.gameMenu);
    }

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // MainThreadSystem.Enqueue can be ignored as this mod is loaded first regardless.
    public override void Load()
    {
        IsEnabled = true;

        PreDrawStars += StarsRealisticPreDraw;
        PostDrawStars += StarsGalaxyPostDraw;

        PreDrawSun += SunRealisticPreDraw;

        OnUpdateSunAndMoonInfo += UpdateSunPositionSaver;

        MethodInfo? verticallyBiasSunAndMoon = typeof(SunPositionSaver).GetMethod(nameof(SunPositionSaver.VerticallyBiasSunAndMoon));

        if (verticallyBiasSunAndMoon is not null)
            RemoveBias = new(verticallyBiasSunAndMoon, 
                (orig_VerticallyBiasSunAndMoon orig) => { });

        MethodInfo? calculatePerspectiveMatrix = typeof(StarsRenderer).GetMethod(nameof(StarsRenderer.CalculatePerspectiveMatrix), NonPublic | Static);

        if (calculatePerspectiveMatrix is not null)
            PatchStarRotation = new(calculatePerspectiveMatrix,
                StarRotation);

        MethodInfo? renderGalaxy = typeof(GalaxyRenderer).GetMethod(nameof(GalaxyRenderer.Render), Public | Static);
        if (renderGalaxy is not null)
            PatchGalaxyRotation = new(renderGalaxy,
                 GalaxyRotation);

        #region Inverted Gravity Patches

        MethodInfo? handleAtmosphereTargetReqest = typeof(AtmosphereTargetContent).GetMethod(nameof(AtmosphereTargetContent.HandleUseReqest), NonPublic | Instance);
        if (handleAtmosphereTargetReqest is not null)
            PatchAtmosphereTarget = new(handleAtmosphereTargetReqest,
                CommonRequestsInvertedGravity);

        MethodInfo? handleCloudsTargetReqest = typeof(CloudsTargetContent).GetMethod(nameof(CloudsTargetContent.HandleUseReqest), NonPublic | Instance);
        if (handleCloudsTargetReqest is not null)
            PatchCloudsTarget = new(handleCloudsTargetReqest,
                CommonRequestsInvertedGravity);

        MethodInfo? drawToCloudsTarget = typeof(CloudsRenderer).GetMethod(nameof(CloudsRenderer.RenderToTarget), Public | Static);
        if (drawToCloudsTarget is not null)
            PatchCloudsShader = new(drawToCloudsTarget,
                CommonShaderInvertedGravity);

        MethodInfo? renderStars = typeof(StarsRenderer).GetMethod(nameof(StarsRenderer.Render), Public | Static);
        if (renderStars is not null)
            PatchStarShader = new(renderStars,
                ModifySunPosition);

        #endregion

        MethodInfo? draw = typeof(RealisticSkyManager).GetMethod(nameof(RealisticSkyManager.Draw), Public | Instance);
        if (draw is not null)
            PatchDrawing = new(draw,
                DrawSky);
    }

    public override void Unload()
    {
        RemoveBias?.Dispose();

        PatchStarRotation?.Dispose();
        PatchGalaxyRotation?.Dispose();

        PatchAtmosphereTarget?.Dispose();
        PatchCloudsTarget?.Dispose();
        PatchCloudsShader?.Dispose();
        PatchStarShader?.Dispose();

        PatchDrawing?.Dispose();
    }

    #endregion

    #region Inverted Gravity Patches

    private void CommonRequestsInvertedGravity(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.Rasterizer))))
            throw new ILEditException(Mod, il, null);

        c.EmitPop();
        c.EmitDelegate(() => RasterizerState.CullNone);
    }

    private void CommonShaderInvertedGravity(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdloca(2),
            i => i.MatchCall<SkyPlayerSnapshot>($"get_{nameof(SkyPlayerSnapshot.InvertedGravity)}")))
            throw new ILEditException(Mod, il, null);

        c.EmitPop();
        c.EmitLdcI4(0);
    }

    private void ModifySunPosition(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchBr(out _),
            i => i.MatchCall<SunPositionSaver>($"get_{nameof(SunPositionSaver.SunPosition)}")))
            throw new ILEditException(Mod, il, null);

        c.EmitDelegate((Vector2 sunPosition) =>
        {
            if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
                sunPosition.Y = Utilities.ScreenSize.Y - sunPosition.Y;

            return sunPosition;
        });
    }

    #endregion

    #region Rotation Patches

    private void StarRotation(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchCall(typeof(RealisticSkyManager).FullName ?? "RealisticSky.Content.RealisticSkyManager", "get_StarViewRotation")))
            throw new ILEditException(Mod, il, null);

        c.EmitPop();
        c.EmitDelegate(() => StarSystem.StarRotation);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdloc(out _),
            i => i.MatchLdloc(out _),
            i => i.MatchCall<Matrix>("op_Multiply"),
            i => i.MatchLdloc(out _),
            i => i.MatchCall<Matrix>("op_Multiply")))
            throw new ILEditException(Mod, il, null);

        c.EmitDelegate((Matrix mat) =>
        {
            bool flip = Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically);

            Vector3 scale = new(1f, flip ? -1f : 1f, 1f);

            return mat * Matrix.CreateScale(scale);
        });
    }

    private void GalaxyRotation(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdcR4(.84f),
                i => i.MatchMul(),
                i => i.MatchLdcR4(.23f),
                i => i.MatchAdd());

            c.EmitPop();

            c.EmitLdcR4(0f);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Patch Draw

    private void DrawSky(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? galaxySkipTarget = c.DefineLabel();
            ILLabel? starSkipTarget = c.DefineLabel();
            ILLabel? jumpSunRendering = c.DefineLabel();

                // Fix various matricies.
            for (int i = 0; i < 3; i++)
            {
                c.GotoNext(MoveType.After,
                    i => i.MatchLdnull(),
                    i => i.MatchLdloc0());

                c.EmitPop();

                    // This is a hack but its the only way I've found to correctly draw the atmosphere target.
                if (i == 0)
                    c.EmitDelegate(() => Matrix.Identity);
                else
                    c.EmitDelegate(() => Main.BackgroundViewMatrix.EffectMatrix);
            }

                // Bring us back to the top.
            c.Index = 0;

                // Branch over the galaxy drawing.
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End)));

            c.EmitBr(galaxySkipTarget);

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End)));

            c.MarkLabel(galaxySkipTarget);

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.Rasterizer)));

            c.EmitPop();
            c.EmitDelegate(() => RasterizerState.CullNone);

                // Branch over the stars drawing.
            c.GotoNext(MoveType.Before, 
                i => i.MatchNop(),
                i => i.MatchLdsfld(typeof(RealisticSkyManager).FullName ?? "RealisticSky.Content.RealisticSkyManager", nameof(RealisticSkyManager.Opacity)),
                i => i.MatchLdloc0(),
                i => i.MatchCall<StarsRenderer>(nameof(StarsRenderer.Render)));
            c.EmitBr(starSkipTarget);

            c.GotoNext(MoveType.After,
                i => i.MatchNop(),
                i => i.MatchLdsfld(typeof(RealisticSkyManager).FullName ?? "RealisticSky.Content.RealisticSkyManager", nameof(RealisticSkyManager.Opacity)),
                i => i.MatchLdloc0(),
                i => i.MatchCall<StarsRenderer>(nameof(StarsRenderer.Render)));
            c.MarkLabel(starSkipTarget);
            
                // Branch over sun rendering.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdloc(out _),
                i => i.MatchBrfalse(out jumpSunRendering),
                i => i.MatchNop(),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End)));

            c.EmitBr(jumpSunRendering);

            c.GotoNext(MoveType.After,
                i => i.MatchLdnull(),
                i => i.MatchCall<Matrix>($"get_{nameof(Matrix.Identity)}"));

            c.EmitPop();
            c.EmitDelegate(() => Main.BackgroundViewMatrix.EffectMatrix);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Public Methods

    #region Shaders

    /// <summary>
    /// Apply a star masking shader if <see cref="RealisticSky"/> is enabled and is active.
    /// </summary>
    public static Effect? ApplyStarShader()
    {
        if (!IsEnabled)
            return null;

        if (!CanDraw)
            return null;

        Effect star = CompatEffects.StarAtmosphere.Value;

        if (!CompatEffects.StarAtmosphere.IsReady)
            return null;

        SetAtmosphereParams(star);

        star.CurrentTechnique.Passes[0].Apply();

        return star;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetAtmosphereParams(Effect shader)
    {
        shader.Parameters["usesAtmosphere"]?.SetValue(true);

        shader.Parameters["screenSize"]?.SetValue(Utilities.ScreenSize);
        shader.Parameters["distanceFadeoff"]?.SetValue(Main.eclipse ? 0.11f : 1f);

        Vector2 sunPosition = SunAndMoonSystem.Info.SunPosition;

        if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            sunPosition.Y = Utilities.ScreenSize.Y - sunPosition.Y;

        shader.Parameters["sunPosition"]?.SetValue(Main.dayTime ? sunPosition : (Vector2.One * 50000f));

        if (AtmosphereRenderer.AtmosphereTarget?.IsReady ?? false)
            Main.instance.GraphicsDevice.Textures[1] = AtmosphereRenderer.AtmosphereTarget.GetTarget();
        else
            Main.instance.GraphicsDevice.Textures[1] = MiscTextures.Invis;

    }

    #endregion

    #region Stars

    public static bool StarsRealisticPreDraw(SpriteBatch spriteBatch, in SpriteBatchSnapshot snapshot, ref float alpha, ref Matrix transform)
    {
        if (!SkyConfig.Instance.DrawRealisticStars || !CanDraw)
            return true;

        StarsRenderer.Render(StarSystem.StarAlpha, Matrix.Identity);

        return true;
    }

    public static void StarsGalaxyPostDraw(SpriteBatch spriteBatch, in SpriteBatchSnapshot snapshot, float alpha, Matrix transform)
    {
        if (!SkyConfig.Instance.DrawRealisticStars || !CanDraw)
            return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, snapshot.RasterizerState, ApplyStarShader(), transform);

        GalaxyRenderer.Render();

        spriteBatch.End();
    }

    #endregion

    #region Sun

    public static bool SunRealisticPreDraw(
        SpriteBatch spriteBatch,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        GraphicsDevice device) =>
        DrawSun();

    public static bool DrawSun()
    {
        if (!CanDraw || !SkyConfig.Instance.RealisticSun)
            return true;

        SunRenderer.Render(1f - RealisticSkyManager.SunlightIntensityByTime);

        return false;
    }

    public static void UpdateSunPositionSaver(SunAndMoonInfo info)
    {
        SunPositionSaver.SunPosition = info.SunPosition;
        SunPositionSaver.MoonPosition = info.MoonPosition;
    }

    #endregion

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Color GetRainColor(Color color, Rain rain) => 
        RainReplacementManager.CalculateRainColor(color, rain);

    #endregion
}
