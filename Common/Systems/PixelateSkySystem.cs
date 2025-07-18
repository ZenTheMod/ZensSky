using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Core.Utils;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class PixelateSkySystem : ModSystem
{
    #region Private Fields

    private static bool HasDrawn;

    private static RenderTarget2D? SkyTarget;

    private static RenderTargetBinding[]? PreviousTargets;

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() => 
        {
            IL_Main.DoDraw += InjectDoDraw;
            IL_Main.DrawSurfaceBG += InjectDrawSurfaceBG;
        });

        IL_Main.DrawCapture += InjectDrawCapture;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DoDraw -= InjectDoDraw;
            IL_Main.DrawSurfaceBG -= InjectDrawSurfaceBG;

            SkyTarget?.Dispose();
        });

        IL_Main.DrawCapture -= InjectDrawCapture;
    }

    #endregion

    #region DoDraw

    private void InjectDoDraw(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

                // Swap to our pixelation target.
            c.GotoNext(MoveType.After,
                i => i.MatchCall(typeof(TimeLogger).FullName ?? "Terraria.TimeLogger", nameof(TimeLogger.DetailedDrawTime)),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End)));

            c.EmitDelegate(PrepareTarget);

                // Fix the ugly background sampling.
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdcI4(0),
                i => i.MatchLdcI4(0),
                i => i.MatchCallvirt<OverlayManager>(nameof(OverlayManager.Draw)));

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<SamplerState>(nameof(SamplerState.LinearClamp)));

            c.EmitPop();

                // Lazy.
            c.EmitDelegate(() => SamplerState.PointClamp);

                // Now handle a backup case just to make sure that when drawing goes wrong nothing explodes.
            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(out _),
                i => i.MatchCall<Main>(nameof(Main.DrawBG)));

            c.EmitDelegate(DrawTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region DrawCapture

    private void InjectDrawCapture(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

                // Swap to our pixelation target,
            c.GotoNext(MoveType.After,
                i => i.MatchCall<Main>(nameof(Main.DrawSimpleSurfaceBackground)),
                i => i.MatchLdsfld<Main>(nameof(Main.tileBatch)),
                i => i.MatchCallvirt<TileBatch>(nameof(TileBatch.End)));

            c.EmitDelegate(PrepareTarget);

                // Draw our pixelation target.
            c.GotoNext(MoveType.After,
                i => i.MatchCall<Main>(nameof(Main.DrawSurfaceBG)),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End)));

            c.EmitDelegate(DrawTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region DrawSurfaceBG

    private void InjectDrawSurfaceBG(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel jumpDepthResetTarget = c.DefineLabel();

                // This is done to ensure that certain modded backgrounds that use CustomSky will still be pixelated correctly; assuming that they're checking for maxDepth >= float.MaxValue.
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.atmo)),
                i => i.MatchMul(),
                i => i.MatchStloc(out _));

            c.EmitDelegate(() =>
            {
                SkyManager.Instance.ResetDepthTracker();

                SkyManager.Instance.DrawToDepth(Main.spriteBatch, float.MaxValue * .5f);

                DrawTarget();
            });

                // Now jump over vanilla reseting it after clouds draw, just to avoid drawing backgrounds twice.
            c.GotoNext(MoveType.Before, 
                i => i.MatchLdsfld<SkyManager>(nameof(SkyManager.Instance)),
                i => i.MatchCallvirt<SkyManager>(nameof(SkyManager.ResetDepthTracker)));

            c.MoveAfterLabels();

            c.EmitBr(jumpDepthResetTarget);

            c.Index--;

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<SkyManager>(nameof(SkyManager.Instance)),
                i => i.MatchCallvirt<SkyManager>(nameof(SkyManager.ResetDepthTracker)));

            c.MarkLabel(jumpDepthResetTarget);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    private static void PrepareTarget()
    {
        Effect pixelate = Shaders.PixelateAndQuantize.Value;

        if (!SkyConfig.Instance.PixelatedSky || pixelate is null)
            return;

        HasDrawn = false;

        SpriteBatch spriteBatch = Main.spriteBatch;

        bool beginCalled = spriteBatch.beginCalled;

        SpriteBatchSnapshot snapshot = new();

        if (beginCalled)
            spriteBatch.End(out snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        PreviousTargets = device.GetRenderTargets();

            // Make sure that we can swap back to the previous targets without losing any information.
                // Lolxd might purge me for meddling with this during drawing.
        foreach (RenderTargetBinding oldTarg in PreviousTargets)
            if (oldTarg.RenderTarget is RenderTarget2D rt)
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

        Viewport viewport = device.Viewport;

        Utilities.ReintializeTarget(ref SkyTarget, device, viewport.Width, viewport.Height);

        device.SetRenderTarget(SkyTarget);
        device.Clear(Color.Transparent);

        if (beginCalled)
            spriteBatch.Begin(in snapshot);
    }

    private static void DrawTarget()
    {
        Effect pixelate = Shaders.PixelateAndQuantize.Value;

        if (!SkyConfig.Instance.PixelatedSky || 
            SkyTarget is null || 
            pixelate is null || 
            Main.mapFullscreen || 
            HasDrawn)
            return;

        HasDrawn = true;

        SpriteBatch spriteBatch = Main.spriteBatch;

        bool beginCalled = spriteBatch.beginCalled;

        SpriteBatchSnapshot snapshot = new();

        if (beginCalled)
            spriteBatch.End(out snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        device.SetRenderTargets(PreviousTargets);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, null, Matrix.Identity);

        Viewport viewport = device.Viewport;

        Vector2 screenSize = new(viewport.Width, viewport.Height);

        pixelate.Parameters["screenSize"]?.SetValue(screenSize);
        pixelate.Parameters["pixelSize"]?.SetValue(new Vector2(2));

        pixelate.Parameters["steps"]?.SetValue(SkyConfig.Instance.ColorSteps);

        int pass = (SkyConfig.Instance.ColorSteps == 255).ToInt();

        pixelate.CurrentTechnique.Passes[pass].Apply();

        spriteBatch.Draw(SkyTarget, viewport.Bounds, Color.White);

        if (beginCalled)
            spriteBatch.Restart(in snapshot);
        else
            spriteBatch.End();
    }
}
