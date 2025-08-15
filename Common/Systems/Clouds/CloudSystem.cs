using Daybreak.Common.CIL;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core.Utils;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;
using ZensSky.Core.Systems.ModCall;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Clouds;

[Autoload(Side = ModSide.Client)]
public sealed class CloudSystem : ModSystem
{
    #region Private Fields

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private const float NoonAlpha = .35f;

    private static readonly Color SunMultiplier = new(255, 245, 225);
    private static readonly Color MoonMultiplier = new(40, 40, 50);

    private static RenderTarget2D? OccludersTarget;
    private static RenderTarget2D? CloudsTarget;

    private static RenderTargetBinding[]? PreviousTargets;

    private static bool CanDrawClouds;

    #endregion

    #region Public Properties

    public static bool ShowCloudLighting
    {
        [ModCall(nameof(ShowCloudLighting), "GetShowCloudLighting")]
        get;
        [ModCall("SetShowCloudLighting")]
        set;
    }

    #endregion

    #region Loading

    public override void Load() 
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DrawSurfaceBG += CloudLighting;
        }); 
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DrawSurfaceBG -= CloudLighting;

            OccludersTarget?.Dispose();
            CloudsTarget?.Dispose();
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
                    canDrawClouds &&
                    Main.numClouds > 0 &&
                    Main.screenPosition.Y < (Main.worldSurface * 16) + 16 &&
                    SkyConfig.Instance.CloudsEnabled &&
                    (ShowCloudLighting = true) &&
                    SkyEffects.CloudLighting.IsReady;
            });

            #endregion

            #region Capturing Clouds

                // Begin capturing occluders.
            c.EmitDelegate(BeginCapturingOccluders);

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

                    // 0/false is used to tell EndCapturingClouds to swap back to PreviousTargets.
                    // 1/true is used to tell EndCapturingClouds to continue capturing to OccludersTarget.
                c.EmitLdcI4(i == 2 ? 0 : 1);
                c.EmitDelegate(EndCapturingClouds);
            }

            #endregion

            #region CloudBG

                // Match to before the first loop.
            c.GotoPrev(MoveType.Before,
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdsfld(typeof(TextureAssets).FullName ?? "Terraria.GameContent.TextureAssets", nameof(TextureAssets.Background)),
                i => i.MatchLdsfld<Main>(nameof(Main.cloudBG)),
                i => i.MatchLdcI4(out _),
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

            c.EmitLdcI4(1);
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

    private static void BeginCapturingOccluders()
    {
        if (!CanDrawClouds)
            return;

        SpriteBatch spriteBatch = Main.spriteBatch;

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        PreviousTargets = device.GetRenderTargets();

            // Set the default RenderTargetUsage to PreserveContents to prevent causing black screens when swaping targets.
        foreach (RenderTargetBinding oldTarg in PreviousTargets)
            if (oldTarg.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        Viewport viewport = device.Viewport;

        Utilities.ReintializeTarget(ref OccludersTarget, device, viewport.Width, viewport.Height);

        device.SetRenderTarget(OccludersTarget);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(in snapshot);
    }

    private static void BeginCapturingClouds()
    {
        if (!CanDrawClouds)
            return;

        SpriteBatch spriteBatch = Main.spriteBatch;

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        Viewport viewport = device.Viewport;

        Utilities.ReintializeTarget(ref CloudsTarget, device, viewport.Width, viewport.Height);

        device.SetRenderTarget(CloudsTarget);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(in snapshot);
    }

    private static void EndCapturingClouds(bool beginCapturing)
    {
        if (!CanDrawClouds)
            return;

        SpriteBatch spriteBatch = Main.spriteBatch;

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        Viewport viewport = device.Viewport;

        Utilities.ReintializeTarget(ref OccludersTarget, device, viewport.Width, viewport.Height);

            // Swap back to the screen target and draw the clouds with the lighting shader attached.
        device.SetRenderTargets(PreviousTargets);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            // Hi ebonf.ly !
            // [Apply shader here] [Should be under the 'SkyEffects.CloudLighting' namespace and located at 'Assets/Effects/Sky/CloudLighting.fx']

        spriteBatch.Draw(CloudsTarget, Utilities.ScreenDimensions, Color.White);

        if (beginCapturing)
        {
            spriteBatch.End();

                // Draw clouds as occluders, and begin capturing the background again.
            device.SetRenderTarget(OccludersTarget);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            spriteBatch.Draw(CloudsTarget, Utilities.ScreenDimensions, Color.White);
        }

        spriteBatch.Restart(in snapshot);
    }

    private static Color GetLightColor(bool day)
    {
            // This will behave a little buggy with Red Sun as the sun will take priority but I'm not implementing an array based light system as of now.
        Vector2 position = day ? Info.SunPosition : Info.MoonPosition;
        float centerX = Utilities.HalfScreenSize.X;

        float distanceFromCenter = MathF.Abs(centerX - position.X) / centerX;

        Color color = day ? Info.SunColor : Info.MoonColor;
        color = color.MultiplyRGB(day ? SunMultiplier : MoonMultiplier);

            // Add a fadeinout effect so the color doesnt just suddenly pop up.
        color *= Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);
            // And lessen it at the lower part of the screen.
        color *= 1 - (position.Y / Utilities.ScreenSize.Y);

            // Decrease the intensity at noon to make the clouds not just be pure white.
            // And alter the intensity depending on the moon phase, where a new moon casts no light.
        if (day)
            color *= MathHelper.Lerp(NoonAlpha, 1f, MathF.Pow(distanceFromCenter, 2));
        else
            color *= MathF.Abs(4 - Main.moonPhase) * .25f;

        return color;
    }
}
