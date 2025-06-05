using Daybreak.Common.CIL;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.Systems.Background;

[Autoload(Side = ModSide.Client)]
public sealed class PixelateSkySystem : ModSystem
{
    private static RenderTarget2D? SkyTarget;

    #region Loading

    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            IL_Main.DoDraw += SwapToRenderTarget;
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            IL_Main.DoDraw -= SwapToRenderTarget;
        });
    }

    #endregion

    private void SwapToRenderTarget(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

                // Being extra safe incase some other mod mucks with rendering like this.
            VariableDefinition oldTargetsIndex = il.AddVariable<RenderTargetBinding[]>();

                // Bit risky; match to after the base sky is draw.
            c.GotoNext(MoveType.After,
                i => i.MatchCall(typeof(TimeLogger).FullName ?? "Terraria.TimeLogger", nameof(TimeLogger.DetailedDrawTime)),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End)));

            c.EmitLdloca(oldTargetsIndex);

            c.EmitDelegate((ref RenderTargetBinding[] oldTargets) =>
            {
                Effect pixelate = Shaders.PixelateAndQuantize.Value;

                if (!SkyConfig.Instance.PixelatedSky || pixelate is null)
                    return;

                GraphicsDevice device = Main.instance.GraphicsDevice;

                oldTargets = device.GetRenderTargets();

                    // Make sure that we can swap back to the previous targets without losing any information.
                        // Lolxd might purge me for meddling with this right before drawing occurs.
                foreach (RenderTargetBinding oldTarg in oldTargets)
                    if (oldTarg.RenderTarget is RenderTarget2D rt)
                        rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;

                PrepareTarget(device);

                device.SetRenderTarget(SkyTarget);
                device.Clear(Color.Transparent);
            });

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdcI4(0),
                i => i.MatchLdcI4(0),
                i => i.MatchCallvirt<OverlayManager>(nameof(OverlayManager.Draw)));

            c.EmitLdloca(oldTargetsIndex);

            c.EmitDelegate((ref RenderTargetBinding[] oldTargets) =>
            {
                Effect pixelate = Shaders.PixelateAndQuantize.Value;

                if (!SkyConfig.Instance.PixelatedSky || SkyTarget is null || pixelate is null || Main.mapFullscreen)
                    return;

                SpriteBatch spriteBatch = Main.spriteBatch;

                spriteBatch.End(out var snapshot);

                GraphicsDevice device = Main.instance.GraphicsDevice;

                device.SetRenderTargets(oldTargets);

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, snapshot.DepthStencilState, RasterizerState.CullCounterClockwise, null, Matrix.Identity);

                Viewport viewport = device.Viewport;

                Vector2 screenSize = new(viewport.Width, viewport.Height);

                pixelate.Parameters["screenSize"]?.SetValue(screenSize);
                pixelate.Parameters["pixelSize"]?.SetValue(new Vector2(2));

                pixelate.Parameters["steps"]?.SetValue(SkyConfig.Instance.ColorSteps);

                pixelate.CurrentTechnique.Passes[0].Apply();

                spriteBatch.Draw(SkyTarget, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);

                spriteBatch.Restart(in snapshot);
            });

                // Fix the ugly background sampling.
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<SamplerState>(nameof(SamplerState.LinearClamp)));

            c.EmitPop();

            c.EmitDelegate(() => SamplerState.PointClamp);
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error("Failed to patch \"Main.DoDraw\".");

            throw new ILPatchFailureException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    private static void PrepareTarget(GraphicsDevice device)
    {
        Viewport viewport = device.Viewport;

        if (SkyTarget is not null && 
            SkyTarget.Width == viewport.Width &&
            SkyTarget.Height == viewport.Height)
            return;

        SkyTarget?.Dispose();
        SkyTarget = new(device, viewport.Width, viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
    }
}
