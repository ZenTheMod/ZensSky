﻿using Daybreak.Common.CIL;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Compat;
using ZensSky.Common.Systems.SunAndMoon;
using ZensSky.Common.Utilities;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Clouds;

[Autoload(Side = ModSide.Client)]
public sealed class CloudSystem : ModSystem
{
    #region Private Fields

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private static readonly Color SunMultiplier = new(255, 245, 225);
    private static readonly Color MoonMultiplier = new(40, 40, 50);

    #endregion

    #region Loading

    public override void Load() => Main.QueueMainThreadAction(() => IL_Main.DrawSurfaceBG += ApplyCloudLighting);

    public override void Unload() => Main.QueueMainThreadAction(() => IL_Main.DrawSurfaceBG -= ApplyCloudLighting);

    #endregion

    private void ApplyCloudLighting(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            VariableDefinition iHaveTrustIssues = c.AddVariable<SpriteBatchSnapshot>();
            VariableDefinition lighting = c.AddVariable<Effect>();

            #region Shader Parameters

            // Setup the shaders parameters.
            c.EmitLdloca(lighting);
            c.EmitDelegate((ref Effect lighting) =>
            {
                lighting = Shaders.Cloud.Value;

                if (!SkyConfig.Instance.CloudsEnabled || lighting is null)
                    return;

                Viewport viewport = Main.instance.GraphicsDevice.Viewport;

                Vector2 viewportSize = viewport.Bounds.Size();
                lighting.Parameters["ScreenSize"]?.SetValue(viewportSize);

                Vector2 sunPosition = SunPosition;
                Vector2 moonPosition = MoonPosition;

                if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
                {
                    sunPosition.Y = viewportSize.Y - sunPosition.Y;
                    moonPosition.Y = viewportSize.Y - moonPosition.Y;
                }

                lighting.Parameters["SunPosition"]?.SetValue(sunPosition);
                lighting.Parameters["MoonPosition"]?.SetValue(moonPosition);

                Color sunColor = GetColor(true);
                lighting.Parameters["SunColor"]?.SetValue(sunColor.ToVector4());

                Color moonColor = GetColor(false);
                lighting.Parameters["MoonColor"]?.SetValue(moonColor.ToVector4());

                lighting.Parameters["DrawSun"]?.SetValue(Main.dayTime);
                lighting.Parameters["DrawMoon"]?.SetValue(RedSunSystem.IsEnabled || !Main.dayTime);
            });

            #endregion

            #region Various Clouds

            for (int i = 0; i < 3; i++)
            {
                    // Match to before the loop.
                c.GotoNext(MoveType.Before, 
                    i => i.MatchBr(out _),
                    i => i.MatchLdsfld<Main>(nameof(Main.cloud)),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdelemRef(),
                    i => i.MatchLdfld<Cloud>(nameof(Cloud.active)));

                    // Apply our shader.
                c.EmitLdloca(iHaveTrustIssues);
                c.EmitLdloc(lighting);
                c.EmitDelegate(ApplyShader);

                    // Match to after the loop.
                c.GotoNext(MoveType.After, 
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdcI4(1),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(out _),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdcI4(200),
                    i => i.MatchBlt(out _));

                c.EmitLdloc(iHaveTrustIssues);
                c.EmitDelegate(ResetSpritebatch);
            }

            #endregion

            #region CloudBG

                // Match to before the loop.
            c.GotoPrev(MoveType.Before,
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdsfld(typeof(TextureAssets).FullName ?? "Terraria.GameContent.TextureAssets", nameof(TextureAssets.Background)),
                i => i.MatchLdsfld<Main>(nameof(Main.cloudBG)),
                i => i.MatchLdcI4(0),
                i => i.MatchLdelemI4());

                // Apply our shader.
            c.EmitLdloca(iHaveTrustIssues);
            c.EmitLdloc(lighting);
            c.EmitDelegate(ApplyShader);

                // Match to after the loop of the other drawn cloud background.
            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(22), // I dislike this.
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdarg(out _),
                i => i.MatchLdfld<Main>(nameof(Main.bgLoops)),
                i => i.MatchBlt(out _));

            c.EmitLdloc(iHaveTrustIssues);
            c.EmitDelegate(ResetSpritebatch);

            #endregion
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"Main.DrawSurfaceBG\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    private void ApplyShader(ref SpriteBatchSnapshot snapshot, Effect lighting)
    {
        if (!SkyConfig.Instance.CloudsEnabled || lighting is null)
            return;

        lighting.CurrentTechnique.Passes[0].Apply();

        Main.spriteBatch.End(out snapshot);
        Main.spriteBatch.Begin(snapshot.SortMode, snapshot.BlendState, SamplerState.PointClamp, snapshot.DepthStencilState, snapshot.RasterizerState, lighting, snapshot.TransformMatrix);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        device.Textures[1] = SunAndMoonRenderingSystem.MoonTexture();
        device.SamplerStates[1] = SamplerState.PointWrap;
    }

    private void ResetSpritebatch(SpriteBatchSnapshot snapshot)
    {
        if (!SkyConfig.Instance.CloudsEnabled)
            return;

        Main.spriteBatch.Restart(in snapshot);
    }

    private static Color GetColor(bool day)
    {
            // This will behave a little buggy with Red Sun as the sun will take priority but I'm not implementing an array based light system as of now.
        Vector2 position = day ? SunPosition : MoonPosition;
        float centerX = MiscUtils.HalfScreenSize.X;

        float distanceFromCenter = MathF.Abs(centerX - position.X) / centerX;

        Color color = day ? SunColor : MoonColor;
        color = color.MultiplyRGB(day ? SunMultiplier : MoonMultiplier);

            // Add a fadeinout effect so the color doesnt just suddenly pop up.
        color *= Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);
            // And lessen it at the lower part of the screen.
        color *= 1 - (position.Y / MiscUtils.ScreenSize.Y);

        return color;
    }
}
