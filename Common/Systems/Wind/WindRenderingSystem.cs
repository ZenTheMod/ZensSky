using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.Wind;

public sealed class WindRenderingSystem : ModSystem
{
    #region Private Fields

    private const float WidthAmplitude = 2f;

    #endregion

    #region Loading

    public override void Load() => On_Main.DrawInfernoRings += DrawWind;

    public override void Unload() => On_Main.DrawInfernoRings -= DrawWind;

    #endregion

    #region Drawing

    private static void DrawWind(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        orig(self);

        if (!SkyConfig.Instance.WindParticles)
            return;

        GraphicsDevice device = self.GraphicsDevice;

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

        float brightness = MathF.Sin(wind.LifeTime * MathHelper.Pi) * Main.atmo;

        for (int i = 0; i < positions.Length - 1; i++)
        {
            float progress = (float)i / positions.Length;
            float width = MathF.Sin(progress * MathHelper.Pi) * brightness * WidthAmplitude;

            Vector2 position = positions[i] - Main.screenPosition;

            float direction = (positions[i] - positions[i + 1]).ToRotation();
            Vector2 offset = new Vector2(width, 0).RotatedBy(direction + MathHelper.PiOver2);

            Color color = Lighting.GetColor(positions[i].ToTileCoordinates()) * brightness;
            color.A = 0;

            vertices[i * 2] = new(new(position + offset, 0), color, new(progress, 0f));

            vertices[i * 2 + 1] = new(new(position - offset, 0), color, new(progress, 1f));
        }

        if (vertices.Length > 3)
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices.ToArray(), 0, vertices.Length - 2);
    }

    #endregion
}
