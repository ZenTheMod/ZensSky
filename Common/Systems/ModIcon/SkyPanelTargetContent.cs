using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using Terraria;
using Terraria.GameContent;
using Terraria.Utilities;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.ModIcon;

public sealed class SkyPanelTargetContent : ARenderTargetContentByRequest
{
    #region Private Fields

    private static readonly Color Clear = new(135, 135, 135);

    private const float PlanetHorizontalOffset = 15f;
    private const float PlanetScale = 150f;
    private const float PlanetRadius = 0.95f;
    private const float PlanetAtmosphere = 0.05f;
    private const float PlanetTimeMultiplier = 0.85f;

    private const int StarCount = 300;
    private const float StarTimeMultiplier = 0.4f;
    private const float MaxPhase = MathHelper.Pi * 8f;
    private const float StarScale = 0.25f;

    private const int CreaseCount = 10;
    private static readonly Vector2 CreaseScale = new(0.01f, 0.6f);
    private const float CreaseOpacity = 0.15f;
    private const float CreaseRotation = 0.22f;

    #endregion

    public Vector2 Size { get; set; }
    public Rectangle InnerDimensions { get; set; }

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(device, nameof(device));
        ArgumentNullException.ThrowIfNull(spriteBatch, nameof(spriteBatch));

        PrepareARenderTarget_AndListenToEvents(ref _target, device, (int)Size.X, (int)Size.Y, (RenderTargetUsage)1);

        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            // I don't trust clear to work on all devices.
        spriteBatch.Draw(Textures.Pixel.Value, new Rectangle(0, 0, (int)Size.X, (int)Size.Y), Clear);

        DrawStars(spriteBatch);

        DrawPlanet(spriteBatch);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        DrawCreases(spriteBatch);

        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    #region Stars

    private void DrawStars(SpriteBatch spriteBatch)
    {
        UnifiedRandom rand = new("guh".GetHashCode());

        int starCount = StarCount;

        float time = Main.GlobalTimeWrappedHourly * StarTimeMultiplier;

        Texture2D star = Textures.Star.Value;
        Vector2 starOrigin = star.Size() * 0.5f;

        for (int i = 0; i < starCount; i++)
        {
            Vector2 starPosition = new(rand.NextFloat(Size.X), rand.NextFloat(Size.Y));

            float lifeTime = time + rand.NextFloat(MaxPhase);
            lifeTime %= MaxPhase;

            if (lifeTime < MathHelper.TwoPi)
            {
                float sinValue = MathF.Sin(lifeTime);

                float scale = MathF.Pow(2, 10 * (sinValue - 1));

                Color color = Color.White * sinValue;
                color.A = 0;
                spriteBatch.Draw(star, starPosition, null, color, 0, starOrigin, scale * StarScale, SpriteEffects.None, 0f);
            }
        }
    }

    #endregion

    #region Planet

    private void DrawPlanet(SpriteBatch spriteBatch)
    {
        Effect planet = Shaders.Planet.Value;

        if (planet is null)
            return;

        planet.Parameters["radius"]?.SetValue(PlanetRadius);
        planet.Parameters["atmosphereRange"]?.SetValue(PlanetAtmosphere);

        planet.Parameters["shadowRotation"]?.SetValue(Main.GlobalTimeWrappedHourly * PlanetTimeMultiplier);

            // Remember this is inverted.
        planet.Parameters["shadowColor"]?.SetValue(Color.Black.ToVector4());

            // Don't bother with an atmosphere.
        planet.Parameters["atmosphereColor"]?.SetValue(Color.Transparent.ToVector4());
        planet.Parameters["atmosphereShadowColor"]?.SetValue(Color.Transparent.ToVector4());

        planet.CurrentTechnique.Passes[0].Apply();

        Texture2D texture = Textures.Pixel.Value;

        Vector2 origin = texture.Size() * 0.5f;

        Vector2 position = new(PlanetHorizontalOffset, InnerDimensions.Center.Y);

        spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, PlanetScale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Creases

    private void DrawCreases(SpriteBatch spriteBatch)
    {
        UnifiedRandom rand = new("Ebon will never steal my code.".GetHashCode());

        Texture2D crease = Textures.SunBloom.Value;
        Vector2 creaseOrigin = crease.Size() * 0.5f;

        Color color = (Color.White * CreaseOpacity) with { A = 0 };

        for (int i = 0; i < CreaseCount; i++)
        {
            Vector2 creasePosition = new(rand.NextFloat(Size.X), rand.NextFloat(Size.Y));

            spriteBatch.Draw(crease, creasePosition, null, color, CreaseRotation, creaseOrigin, CreaseScale, SpriteEffects.None, 0f);
        }
    }

    #endregion
}
