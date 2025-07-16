using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core;
using ZensSky.Core.Exceptions;
using static ZensSky.Common.Systems.Stars.ShootingStarSystem;

namespace ZensSky.Common.Systems.Stars;

[Autoload(Side = ModSide.Client)]
public sealed class ShootingStarRenderingSystem : ModSystem
{
    #region Private Fields

    private const float WidthAmplitude = 1.3f;

    private const float StarRatio = .15f;

    private const float StarScale = .13f;

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() => IL_Main.DoDraw += DrawAfterSunAndMoon);

        IL_Main.DrawCapture += DrawAfterSunAndMoon;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() => IL_Main.DoDraw -= DrawAfterSunAndMoon);

        IL_Main.DrawCapture -= DrawAfterSunAndMoon;
    }

    #endregion

    #region Inject

    private void DrawAfterSunAndMoon(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchCall<Main>(nameof(Main.DrawSunAndMoon)));

            c.EmitDelegate(DrawShootingStars);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Drawing

    private static void DrawShootingStars()
    {
        if (!ZensSky.CanDrawSky || !ShowShootingStars)
        {
            ShowShootingStars = true;
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        float alpha = StarSystem.StarAlpha;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, RealisticSkySystem.ApplyStarShader(), snapshot.TransformMatrix);

        foreach (ShootingStar star in ShootingStars.Where(s => s.IsActive))
            DrawShootingStar(spriteBatch, device, star, alpha);

        spriteBatch.Restart(in snapshot);
    }

    private static void DrawShootingStar(SpriteBatch spriteBatch, GraphicsDevice device, ShootingStar star, float alpha)
    {
        Vector2[] positions = [.. star.OldPositions.Where(pos => pos != default && pos != star.Position)];

        if (positions.Length <= 2)
            return;

        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(positions.Length - 1) * 2];

        Color color = Color.LightGray * alpha * MathF.Sin(star.LifeTime * MathHelper.Pi);
        color.A = 0;

        for (int i = 0; i < positions.Length - 1; i++)
        {
            float progress = (float)i / positions.Length;
            float width = MathF.Sin(progress * MathHelper.Pi) * WidthAmplitude;

            Vector2 position = positions[i];

            float direction = (position - positions[i + 1]).ToRotation();
            Vector2 offset = new Vector2(width, 0).RotatedBy(direction + MathHelper.PiOver2);

            vertices[i * 2] = new(new(position - offset, 0), color, new(progress, 0f));
            vertices[i * 2 + 1] = new(new(position + offset, 0), color, new(progress, 1f));
        }

        device.Textures[0] = Textures.ShootingStar.Value;

        if (vertices.Length > 3)
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);

        Texture2D starTexture = Textures.Star.Value;
        Vector2 starOrigin = starTexture.Size() * .5f;

        Vector2 starPosition = positions[(int)(positions.Length * StarRatio)];

        spriteBatch.Draw(starTexture, starPosition, null, color, 0f, starOrigin, StarScale, SpriteEffects.None, 0f);
    }

    #endregion
}
