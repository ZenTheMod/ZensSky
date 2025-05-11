using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Utilities;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.ModIcon;

public sealed class SkyPanelTargetContent : ARenderTargetContentByRequest
{
    private static readonly Color Clear = new(185, 185, 185);

    private const float PlanetHorizontalOffset = 15f;
    private const float PlanetScale = 150f;

    private const int StarCount = 300;

    private const float TimeMultiplier = 0.4f;

    private const float MaxPhase = MathHelper.Pi * 8f;

    private const float StarScale = 0.25f;

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
        spriteBatch.Draw(Textures.Pixel.Value, new Rectangle(0, 0, (int)Size.X, (int)Size.Y), new(135, 135, 135));

        DrawStars(spriteBatch);

        DrawPlanet(spriteBatch);

        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    private void DrawStars(SpriteBatch spriteBatch)
    {
        UnifiedRandom rand = new("guh".GetHashCode());

        int starCount = StarCount;

        float time = Main.GlobalTimeWrappedHourly * TimeMultiplier;

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

                Color innerColor = Color.White * sinValue;
                innerColor.A = 0;
                spriteBatch.Draw(star, starPosition, null, innerColor, 0, starOrigin, scale * StarScale, SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawPlanet(SpriteBatch spriteBatch)
    {
        Effect planet = Shaders.Planet.Value;

        if (planet is null)
            return;

        planet.Parameters["shadowColor"]?.SetValue(Color.Black.ToVector4());

        planet.Parameters["planetRotation"]?.SetValue(0f);
        planet.Parameters["shadowRotation"]?.SetValue(Main.GlobalTimeWrappedHourly * TimeMultiplier);

        planet.Parameters["falloffStart"]?.SetValue(0.97f);
        planet.CurrentTechnique.Passes[0].Apply();

        Texture2D texture = Textures.Pixel.Value;

        Vector2 origin = texture.Size() * 0.5f;

        Vector2 position = new(PlanetHorizontalOffset, InnerDimensions.Center.Y);

        spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, PlanetScale, SpriteEffects.None, 0f);
    }
}
