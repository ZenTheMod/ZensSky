using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.Wind;

[Autoload(Side = ModSide.Client)]
public sealed class WindRenderingSystem : ModSystem
{
    #region Private Fields

    private const float WidthAmplitude = 2f;
    private const float Alpha = 0.8f;

    #endregion

    #region Loading

    public override void Load() 
    {
        Main.QueueMainThreadAction(() => {
            On_Main.DrawInfernoRings += InGameDraw;
            On_Main.DrawBackgroundBlackFill += MenuDraw; 
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => {
            On_Main.DrawInfernoRings -= InGameDraw;
            On_Main.DrawBackgroundBlackFill -= MenuDraw;
        });
    }

    private void MenuDraw(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
    {
        orig(self);

        if (!Main.gameMenu)
            return;

        DrawWinds();
    }

    private void InGameDraw(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        orig(self);

        DrawWinds();
    }

    #endregion

    #region Drawing

    private static void DrawWinds()
    {
        if (!SkyConfig.Instance.WindParticles)
            return;

        GraphicsDevice device = Main.graphics.GraphicsDevice;

        device.Textures[0] = Textures.SunBloom.Value;

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

        for (int i = 0; i < positions.Length - 1; i++)
        {
            float progress = (float)i / positions.Length;
            float width = MathF.Sin(progress * MathHelper.Pi) * brightness * WidthAmplitude;

            Vector2 position = positions[i] - Main.screenPosition;

            float direction = (positions[i] - positions[i + 1]).ToRotation();
            Vector2 offset = new Vector2(width, 0).RotatedBy(direction + MathHelper.PiOver2);

            Color color = Lighting.GetColor(positions[i].ToTileCoordinates()) * brightness * Alpha;
            color.A = 0;

            vertices[i * 2] = new(new(position - offset, 0), color, new(progress, 0f));
            vertices[i * 2 + 1] = new(new(position + offset, 0), color, new(progress, 1f));
        }

        if (vertices.Length > 3)
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
    }

    #endregion
}
