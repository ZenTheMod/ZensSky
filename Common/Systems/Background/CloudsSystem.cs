using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Exceptions;
using ZensSky.Core.ModCall;
using ZensSky.Core.Utils;
using static ZensSky.Common.Systems.Sky.Lighting.SkyLightingSystem;

namespace ZensSky.Common.Systems.Background;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="CloudLighting"/><br/>
///         Injects into vanilla sky drawing to apply a cloud lighting shader.
///     </item>
/// </list>
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class CloudsSystem : ModSystem
{
    #region Private Fields

    private static RenderTarget2D? BackgroundTarget;
    private static RenderTarget2D? OccludersTarget;
    private static RenderTarget2D? LightTarget;

    private const float LightTargetScale = .25f;

    private static RenderTargetBinding[]? PreviousTargets;

    private static bool CanDrawClouds;

    #endregion

    #region Public Properties

    public static bool ShowCloudLighting
    {
        [ModCall(nameof(ShowCloudLighting), $"Get{nameof(ShowCloudLighting)}")]
        get;
        [ModCall($"Set{nameof(ShowCloudLighting)}")]
        set;
    }

    #endregion

    #region Loading

    public override void Load() =>
        MainThreadSystem.Enqueue(() => IL_Main.DrawSurfaceBG += CloudLighting);

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DrawSurfaceBG -= CloudLighting;

            BackgroundTarget?.Dispose();
            OccludersTarget?.Dispose();
            LightTarget?.Dispose();
        });
    }

    #endregion

    #region Cloud Lighting

    private void CloudLighting(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            #region Setup

            int canDrawCloudsIndex = -1;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdcI4(out _),
                i => i.MatchStloc(out canDrawCloudsIndex));

            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.ColorOfSurfaceBackgroundsBase)));

            c.MoveAfterLabels();

                // See if we even need to capture the background.
            c.EmitLdloc(canDrawCloudsIndex);

            c.EmitDelegate((bool canDrawClouds) =>
            {
                CanDrawClouds =
                    ZensSky.CanDrawSky &&
                    canDrawClouds &&
                    (Main.numClouds > 0 || Main.cloudBGAlpha > 0) &&
                    Main.screenPosition.Y < Main.worldSurface * 16 + 16 &&
                    SkyConfig.Instance.UseCloudLighting &&
                    (ShowCloudLighting = true) &&
                    SkyEffects.CloudLighting.IsReady &&
                    SkyEffects.CloudGodrays.IsReady &&
                    SkyEffects.CloudOcclusion.IsReady;
            });

            c.GotoNext(MoveType.Before,
                    i => i.MatchBr(out _),
                    i => i.MatchLdsfld<Main>(nameof(Main.cloud)),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdelemRef(),
                    i => i.MatchLdfld<Cloud>(nameof(Cloud.active)));

            c.EmitDelegate(ClearTargets);

            #endregion

            #region Capturing Clouds

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

                    // Begin capturing clouds.
                c.EmitDelegate(BeginCapturingClouds);

                    // Match to after the loop ends.
                c.GotoNext(MoveType.After,
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdcI4(out _),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(out _),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdcI4(out _),
                    i => i.MatchBlt(out _));

                    // 0/false is used to tell EndCapturingClouds to continue capturing to OccludersTarget.
                    // 1/true is used to tell EndCapturingClouds to swap back to PreviousTargets.
                c.EmitLdcI4(i == 2 ? 1 : 0);
                c.EmitDelegate(EndCapturingClouds);
            }

            #endregion

            #region CloudBG

                // Match to before the first loop.
            c.GotoPrev(MoveType.Before,
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdsfld(typeof(TextureAssets).FullName!, nameof(TextureAssets.Background)),
                i => i.MatchLdsfld<Main>(nameof(Main.cloudBG)),
                i => i.MatchLdcI4(0),
                i => i.MatchLdelemI4());

                // Begin capturing clouds.
            c.EmitDelegate(BeginCapturingClouds);

                // Match to after the loop of the other drawn cloud background.
            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(22),
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdarg(out _),
                i => i.MatchLdfld<Main>(nameof(Main.bgLoops)),
                i => i.MatchBlt(out _));

            c.EmitLdcI4(0);
            c.EmitDelegate(EndCapturingClouds);

            #endregion

            #endregion
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Capturing

    private static void ClearTargets()
    {
        if (!CanDrawClouds)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        PreviousTargets = device.GetRenderTargets();

            // Set the default RenderTargetUsage to PreserveContents to prevent causing black screens when swaping targets.
        foreach (RenderTargetBinding oldTarg in PreviousTargets)
            if (oldTarg.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        BeginCapturingOccluders();

        device.Clear(Color.Transparent);
    }

    private static void BeginCapturingOccluders()
    {
        if (!CanDrawClouds)
            return;

        SpriteBatch spriteBatch = Main.spriteBatch;

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        Viewport viewport = device.Viewport;

        Utilities.ReintializeTarget(ref BackgroundTarget, device, viewport.Width, viewport.Height);
        Utilities.ReintializeTarget(ref OccludersTarget, device, viewport.Width, viewport.Height, preferredFormat: SurfaceFormat.Single);

        device.SetRenderTargets(BackgroundTarget, OccludersTarget);

            // Apply a shader that draws to both active targets.
        SkyEffects.CloudOcclusion.Apply();

        spriteBatch.Begin(snapshot.SortMode, snapshot.BlendState, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, SkyEffects.CloudOcclusion.Value, snapshot.TransformMatrix);
    }

    private static void BeginCapturingClouds()
    {
        if (!CanDrawClouds)
            return;

        SpriteBatch spriteBatch = Main.spriteBatch;

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        Viewport viewport = device.Viewport;

        int width = (int)(viewport.Width * LightTargetScale);
        int height = (int)(viewport.Height * LightTargetScale);

        using (new RenderTargetSwap(ref LightTarget, width, height))
        {
            device.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            DrawGodrays(spriteBatch);

            spriteBatch.End();
        }

            // Only draw clouds to the background as they should not be occluding light.
        device.SetRenderTarget(BackgroundTarget);

        Effect cloudLighting = ApplyCloudLighting();

        spriteBatch.Begin(snapshot.SortMode, snapshot.BlendState, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, cloudLighting, snapshot.TransformMatrix);

        device.Textures[1] = LightTarget;
        device.SamplerStates[1] = SamplerState.LinearClamp;
    }

    private static void EndCapturingClouds(bool endCapturing)
    {
        if (!CanDrawClouds)
            return;

        SpriteBatch spriteBatch = Main.spriteBatch;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        if (endCapturing)
        {
            spriteBatch.End(out var snapshot);

            device.SetRenderTargets(PreviousTargets);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            spriteBatch.Draw(BackgroundTarget, Utilities.ScreenDimensions, Color.White);

                // Restart with no effects applied.
            spriteBatch.End();
            spriteBatch.Begin(snapshot.SortMode, snapshot.BlendState, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

            return;
        }

        BeginCapturingOccluders();
    }

    #endregion

    #region Shaders

    private static void DrawGodrays(SpriteBatch spriteBatch)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;

        Viewport viewport = device.Viewport;

        Vector2 viewportSize = viewport.Bounds.Size();

        SkyEffects.CloudGodrays.ScreenSize = viewportSize;

        int sampleCount = SkyConfig.Instance.CloudGodraysSamples;

        SkyEffects.CloudGodrays.SampleCount = sampleCount;

        InvokeForActiveLights((light) =>
        {
            if (light.Color == Color.Black)
                return;

            SkyEffects.CloudGodrays.LightPosition = light.Position * LightTargetScale;
            SkyEffects.CloudGodrays.LightColor = light.Color.ToVector4();

            SkyEffects.CloudGodrays.LightSize = light.Size;

            SkyEffects.CloudGodrays.UseTexture = false;

            if (light.Texture is not null)
            {
                SkyEffects.CloudGodrays.UseTexture = true;
                device.Textures[1] = light.Texture.Value;
            }

                // TODO: Not this!
            if (SkyConfig.Instance.UseCloudGodrays)
                SkyEffects.CloudGodrays.ApplyGodrays();
            else
                SkyEffects.CloudGodrays.ApplyLight();

            spriteBatch.Draw(OccludersTarget, viewport.Bounds, Color.White);
        });
    }

    private static Effect ApplyCloudLighting()
    {
        Viewport viewport = Main.instance.GraphicsDevice.Viewport;

        Vector2 viewportSize = viewport.Bounds.Size();

        SkyEffects.CloudLighting.ScreenSize = viewportSize;

        SkyEffects.CloudLighting.Pixel = 1 / LightTargetScale;

        SkyEffects.CloudLighting.Apply();

        return SkyEffects.CloudLighting.Value;
    }

    #endregion
}
