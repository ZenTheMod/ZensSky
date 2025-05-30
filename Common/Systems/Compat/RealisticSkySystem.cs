﻿using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RealisticSky.Common.DataStructures;
using RealisticSky.Content;
using RealisticSky.Content.Atmosphere;
using RealisticSky.Content.Clouds;
using RealisticSky.Content.NightSky;
using RealisticSky.Content.Sun;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Systems.SunAndMoon;
using ZensSky.Common.Utilities;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("RealisticSky")]
[ExtendsFromMod("RealisticSky")]
public sealed class RealisticSkySystem : ModSystem
{
    #region Private Fields

    private delegate void orig_VerticallyBiasSunAndMoon();
    private static Hook? RemoveBias;

    private static ILHook? StarRotationPatch;

    private static ILHook? PatchAtmosphereTarget;
        // private static ILHook? PatchAtmosphereShader;
    private static ILHook? PatchCloudsTarget;
    private static ILHook? PatchCloudsShader;
    private static ILHook? PatchStarShader;

    private static ILHook? PatchDrawing;

    private static ILHook? StaticGalaxy;

    private static FieldInfo? AtmosphereTargetInfo;

    private static MethodInfo? SetSunPosition;
    private static MethodInfo? SetMoonPosition;

    private static AtmosphereTargetContent? AtmosphereTarget => (AtmosphereTargetContent?)AtmosphereTargetInfo?.GetValue(null);

    #endregion

    public static bool IsEnabled { get; private set; }

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        AtmosphereTargetInfo = typeof(AtmosphereRenderer).GetField("AtmosphereTarget", NonPublic | Static);

            // This is a stupid hack.
        SetSunPosition = typeof(SunPositionSaver).GetMethod($"set_{nameof(SunPositionSaver.SunPosition)}", NonPublic | Static);
        SetMoonPosition = typeof(SunPositionSaver).GetMethod($"set_{nameof(SunPositionSaver.MoonPosition)}", NonPublic | Static);

        MethodInfo? verticallyBiasSunAndMoon = typeof(SunPositionSaver).GetMethod(nameof(SunPositionSaver.VerticallyBiasSunAndMoon));

        if (verticallyBiasSunAndMoon is not null)
            RemoveBias = new(verticallyBiasSunAndMoon, 
                (orig_VerticallyBiasSunAndMoon orig) => { });

        MethodInfo? calculatePerspectiveMatrix = typeof(StarsRenderer).GetMethod("CalculatePerspectiveMatrix", NonPublic | Static);

        if (calculatePerspectiveMatrix is not null)
            StarRotationPatch = new(calculatePerspectiveMatrix,
                StarRotation);

        #region Inverted Gravity Patches

        MethodInfo? handleAtmosphereTargetReqest = typeof(AtmosphereTargetContent).GetMethod("HandleUseReqest", NonPublic | Instance);
        if (handleAtmosphereTargetReqest is not null)
            PatchAtmosphereTarget = new(handleAtmosphereTargetReqest,
                CommonRequestsInvertedGravity);

            // Removed due to no longer using a RenderTarget based system.

            // MethodInfo? drawToAtmosphereTarget = typeof(AtmosphereRenderer).GetMethod("RenderToTarget", Public | Static);
            // if (drawToAtmosphereTarget is not null)
            //     PatchAtmosphereShader = new(drawToAtmosphereTarget,
            //         CommonShaderInvertedGravity);

        MethodInfo? handleCloudsTargetReqest = typeof(CloudsTargetContent).GetMethod("HandleUseReqest", NonPublic | Instance);
        if (handleCloudsTargetReqest is not null)
            PatchCloudsTarget = new(handleCloudsTargetReqest,
                CommonRequestsInvertedGravity);

        MethodInfo? drawToCloudsTarget = typeof(CloudsRenderer).GetMethod("RenderToTarget", Public | Static);
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

            // This is done so that when drawing the galaxy manually it'll rotate around the center of the screen, rather than the center of its sprite.
        MethodInfo? renderGalaxy = typeof(GalaxyRenderer).GetMethod(nameof(GalaxyRenderer.Render), Public | Static);
        if (renderGalaxy is not null)
            StaticGalaxy = new(renderGalaxy,
                 ChangeGalaxyRotation);
    }

    public override void Unload()
    {
        RemoveBias?.Dispose();

        StarRotationPatch?.Dispose();

        PatchAtmosphereTarget?.Dispose();
            // PatchAtmosphereShader?.Dispose();
        PatchCloudsTarget?.Dispose();
        PatchCloudsShader?.Dispose();
        PatchStarShader?.Dispose();

        PatchDrawing?.Dispose();

        StaticGalaxy?.Dispose();
    }

    #endregion

    #region Inverted Gravity Patches

    private void CommonRequestsInvertedGravity(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.Rasterizer))))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitPop();
        c.EmitDelegate(() => RasterizerState.CullNone);
    }

    private void CommonShaderInvertedGravity(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdloca(2),
            i => i.MatchCall<SkyPlayerSnapshot>($"get_{nameof(SkyPlayerSnapshot.InvertedGravity)}")))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitPop();
        c.EmitLdcI4(0);
    }

    private void ModifySunPosition(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchBr(out _),
            i => i.MatchCall<SunPositionSaver>($"get_{nameof(SunPositionSaver.SunPosition)}")))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitDelegate((Vector2 sunPosition) =>
        {
            if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
                sunPosition.Y = MiscUtils.ScreenSize.Y - sunPosition.Y;

            return sunPosition;
        });
    }

    #endregion

    private void StarRotation(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchCall(typeof(RealisticSkyManager).FullName ?? "RealisticSky.Content.RealisticSkyManager", "get_StarViewRotation")))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitPop();
        c.EmitDelegate(() => StarSystem.StarRotation);

        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdloc(out _),
            i => i.MatchLdloc(out _),
            i => i.MatchCall<Matrix>("op_Multiply"),
            i => i.MatchLdloc(out _),
            i => i.MatchCall<Matrix>("op_Multiply")))
            throw new ILPatchFailureException(Mod, il, null);

        c.EmitDelegate((Matrix mat) =>
        {
            bool flip = Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically);

            Vector3 scale = new(1f, flip ? -1f : 1f, 1f);

            return mat * Matrix.CreateScale(scale);
        });
    }

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

            c.EmitDelegate(() => SkyConfig.Instance.SunAndMoonRework && !SkyConfig.Instance.RealisticSun);
            c.EmitBrtrue(jumpSunRendering);

            c.GotoNext(MoveType.After,
                i => i.MatchLdnull(),
                i => i.MatchCall<Matrix>("get_Identity"));

            c.EmitPop();
            c.EmitDelegate(() => Main.BackgroundViewMatrix.EffectMatrix);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \'RealisticSkyManager.Draw\'.");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    #endregion

    private void ChangeGalaxyRotation(ILContext il)
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
            Mod.Logger.Error("Failed to patch \'GalaxyRenderer.Render\'.");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    #region Stars

    public static void ApplyStarShader()
    {
        Effect star = Shaders.StarAtmosphere.Value;

        if (star is null)
            return;

        SetAtmosphereParams(star);

        star.CurrentTechnique.Passes[0].Apply();
    }

    public static void SetAtmosphereParams(Effect shader)
    {
        shader.Parameters["usesAtmosphere"]?.SetValue(true);

        shader.Parameters["screenSize"]?.SetValue(MiscUtils.ScreenSize);
        shader.Parameters["distanceFadeoff"]?.SetValue(Main.eclipse ? 0.11f : 1f);

        Vector2 sunPosition = SunAndMoonSystem.SunPosition;

        if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            sunPosition.Y = MiscUtils.ScreenSize.Y - sunPosition.Y;

        shader.Parameters["sunPosition"]?.SetValue(Main.dayTime ? sunPosition : (Vector2.One * 50000f));

        if (AtmosphereTarget?.IsReady is true)
            Main.instance.GraphicsDevice.Textures[1] = AtmosphereTarget.GetTarget() ?? Textures.Invis.Value;
    }

    public static void DrawStars() 
    {
        if (!SkyConfig.Instance.DrawRealisticStars)
            return;

        StarsRenderer.Render(StarSystem.StarAlpha, Matrix.Identity); 
    }

    private static Matrix GalaxyMatrix()
    {
        Matrix rotation = Matrix.CreateRotationZ(StarSystem.StarRotation);
        Matrix offset = Matrix.CreateTranslation(new(MiscUtils.HalfScreenSize, 0f));
        Matrix revoffset = Matrix.CreateTranslation(new(-MiscUtils.HalfScreenSize, 0f));

        return revoffset * rotation * offset * Main.BackgroundViewMatrix.EffectMatrix;
    }

    public static void DrawGalaxy() 
    {
        if (!SkyConfig.Instance.DrawRealisticStars)
            return;

        Main.spriteBatch.End(out var snapshot);
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, snapshot.RasterizerState, null, GalaxyMatrix());

        ApplyStarShader();

        GalaxyRenderer.Render();

        Main.spriteBatch.Restart(in snapshot);
    }

    #endregion

    public static void UpdateSunAndMoonPosition(Vector2 position)
    {
        SetSunPosition?.Invoke(null, [position]);
        SetMoonPosition?.Invoke(null, [position]);
    }
}
