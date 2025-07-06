using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;
using ZensSky.Core.DataStructures;

namespace ZensSky.Common.Systems.Ambience;

[Autoload(Side = ModSide.Client)]
public sealed class WindRenderingSystem : ModSystem
{
    #region Private Fields

    private const float WidthAmplitude = 2f;

    private static RenderTarget2D? WindTarget;

    #endregion

    #region Loading

    public override void Load() 
    {
        Main.QueueMainThreadAction(() => On_Main.DrawBackgroundBlackFill += MenuDraw);

        On_Main.DrawInfernoRings += InGameDraw;
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => {
            On_Main.DrawInfernoRings -= InGameDraw;
            On_Main.DrawBackgroundBlackFill -= MenuDraw;

            WindTarget?.Dispose();
        });

        On_Main.DrawInfernoRings -= InGameDraw;
    }

    private void MenuDraw(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
    {
        orig(self);

        if (!Main.gameMenu || !SkyConfig.Instance.WindParticles || SkyConfig.Instance.WindOpacity <= 0)
            return;

        if (SkyConfig.Instance.PixelatedSky)
            DrawPixelated();
        else
            DrawWinds();
    }

    private void InGameDraw(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        orig(self);

        if (Main.gameMenu || !SkyConfig.Instance.WindParticles || SkyConfig.Instance.WindOpacity <= 0)
            return;

        if (SkyConfig.Instance.PixelatedSky)
            DrawPixelated();
        else
            DrawWinds();
    }

    #endregion

    #region Drawing

        // Surely someone will kill me for this right ?
    private static void DrawPixelated()
    {
        Effect pixelate = Shaders.PixelateAndQuantize.Value;

        if (!SkyConfig.Instance.PixelatedSky || pixelate is null || Main.mapFullscreen)
            return;

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        Viewport viewport = device.Viewport;

        SpriteBatch spriteBatch = Main.spriteBatch;

        spriteBatch.End(out var snapshot);

        using (new RenderTargetSwap(ref WindTarget, viewport.Width, viewport.Height))
        {
            device.Clear(Color.Transparent);
            spriteBatch.Begin(in snapshot);

            DrawWinds();

            spriteBatch.End();
        }

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, null, Matrix.Identity);

        Vector2 screenSize = new(viewport.Width, viewport.Height);

        pixelate.Parameters["screenSize"]?.SetValue(screenSize);
        pixelate.Parameters["pixelSize"]?.SetValue(new Vector2(2));

        pixelate.Parameters["steps"]?.SetValue(SkyConfig.Instance.ColorSteps);

        pixelate.CurrentTechnique.Passes[0].Apply();

        spriteBatch.Draw(WindTarget, viewport.Bounds, Color.White);

        spriteBatch.Restart(in snapshot);
    }

    private static void DrawWinds()
    {
        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.Textures[0] = Textures.Bloom.Value;

        foreach (WindParticle wind in WindSystem.Winds.Where(w => w.IsActive))
            DrawWindTrail(device, wind);
    }

    private static void DrawWindTrail(GraphicsDevice device, WindParticle wind)
    {
        Vector2[] positions = [.. wind.OldPositions.Where(pos => pos != default)];

        if (positions.Length <= 2)
            return;

        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(positions.Length - 1) * 2];

        float brightness = MathF.Sin(wind.LifeTime * MathHelper.Pi) * Main.atmo * MathF.Abs(Main.WindForVisuals);

        float alpha = SkyConfig.Instance.WindOpacity;

        for (int i = 0; i < positions.Length - 1; i++)
        {
            float progress = (float)i / positions.Length;
            float width = MathF.Sin(progress * MathHelper.Pi) * brightness * WidthAmplitude;

            Vector2 position = positions[i] - Main.screenPosition;

            float direction = (positions[i] - positions[i + 1]).ToRotation();
            Vector2 offset = new Vector2(width, 0).RotatedBy(direction + MathHelper.PiOver2);

            Color color = Lighting.GetColor(positions[i].ToTileCoordinates()).MultiplyRGB(Main.ColorOfTheSkies) * brightness * alpha;
            color.A = 0;

            vertices[i * 2] = new(new(position - offset, 0), color, new(progress, 0f));
            vertices[i * 2 + 1] = new(new(position + offset, 0), color, new(progress, 1f));
        }

        if (vertices.Length > 3)
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
    }

    #endregion
}
