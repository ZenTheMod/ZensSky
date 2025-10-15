using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Core;
using ZensSky.Core.DataStructures;

namespace ZensSky.Common.Systems.Weather;

public static class WindRendering
{
    #region Private Fields

    private static RenderTarget2D? WindTarget;

    #endregion

    #region Loading

    [OnLoad(Side = ModSide.Client)]
    public static void Load() 
    {
        MainThreadSystem.Enqueue(() =>
            On_Main.DrawBackgroundBlackFill += MenuDraw);

        On_Main.DrawInfernoRings += InGameDraw;
    }

    [OnUnload(Side = ModSide.Client)]
    public static void Unload()
    {
        MainThreadSystem.Enqueue(() => 
        {
            On_Main.DrawBackgroundBlackFill -= MenuDraw;

            WindTarget?.Dispose();
        });

        On_Main.DrawInfernoRings -= InGameDraw;
    }

    private static void MenuDraw(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
    {
        orig(self);

        if (!Main.gameMenu || !SkyConfig.Instance.UseWindParticles || SkyConfig.Instance.WindOpacity <= 0)
            return;

        if (SkyConfig.Instance.UsePixelatedSky)
            DrawPixelated();
        else
            DrawWind();
    }

    private static void InGameDraw(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        orig(self);

        if (Main.gameMenu || !SkyConfig.Instance.UseWindParticles || SkyConfig.Instance.WindOpacity <= 0)
            return;

        if (SkyConfig.Instance.UsePixelatedSky)
            DrawPixelated();
        else
            DrawWind();
    }

    #endregion

    #region Drawing

    private static void DrawPixelated()
    {
        if (!SkyConfig.Instance.UsePixelatedSky || 
            !SkyEffects.PixelateAndQuantize.IsReady || 
            Main.mapFullscreen)
            return;

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        Viewport viewport = device.Viewport;

        SpriteBatch spriteBatch = Main.spriteBatch;

        spriteBatch.End(out var snapshot);

        using (new RenderTargetSwap(ref WindTarget, viewport.Width, viewport.Height))
        {
            device.Clear(Color.Transparent);
            spriteBatch.Begin(in snapshot);

            DrawWind();

            spriteBatch.End();
        }

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, null, Matrix.Identity);

        Vector2 screenSize = new(viewport.Width, viewport.Height);

        SkyEffects.PixelateAndQuantize.ScreenSize = screenSize;
        SkyEffects.PixelateAndQuantize.PixelSize = new(2);

        SkyEffects.PixelateAndQuantize.Steps = SkyConfig.Instance.ColorSteps;

        int pass = (SkyConfig.Instance.ColorSteps == 255).ToInt();

        SkyEffects.PixelateAndQuantize.Apply(pass);

        spriteBatch.Draw(WindTarget, viewport.Bounds, Color.White);

        spriteBatch.Restart(in snapshot);
    }

    private static void DrawWind()
    {
        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.Textures[0] = SkyTextures.SunBloom;

        ReadOnlySpan<WindParticle> activeWind = [.. WindSystem.Winds.Where(w => w.IsActive)];

        for (int i = 0; i < activeWind.Length; i++)
            activeWind[i].Draw(device);
    }

    #endregion
}
