using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.Systems.Stars;

public sealed class StarTargetContent : ARenderTargetContentByRequest
{
    #region Private Fields

    private const float VanillaStarsOpacity = 0.85f;

    private const float TwinkleFrequencyDivisor = 12f;
    private const float TwinkleAmplitude = 0.2f;
    private const float TwinkleBaseMultiplier = 1f;

    private const float StarScaleDivisor = 1.3f;
    private const float PrimaryFlareOpacity = 0.13f;
    private const float PrimaryFlareScaleDivisor = 4.15f;
    private const float SecondaryFlareOpacity = 0.6f;
    private const float SecondaryFlareScaleDivisor = 6f;

        // private const float GalaxyScaleMultiplier = 0.6f;
        // private const float RealisticStarAlphaMultiplier = 0.6f;
        // private const float EclipseFalloff = 0.05f;
        // private const float NormalFalloff = 0.3f;

        // Use Vector4s rather than colors to allow us to go over the byte limit of 255.
    private static readonly Vector4 ExplosionStart = new(1.5f, 2.5f, 4f, 1f);
    private static readonly Vector4 ExplosionEnd = new(2.4f, 1.25f, 3.2f, .7f);
    private static readonly Vector4 RingStart = new(3.5f, 2.9f, 1f, 1f);
    private static readonly Vector4 RingEnd = new(7.5f, 1.8f, .5f, .5f);

    private static readonly Vector4 Background = new(0, 0, 0, 0);

    private const float QuickTimeMultiplier = 7f;
    private const float ExpandTimeMultiplier = 6f;
    private const float RingTimeMultiplier = 2.3f;

    #endregion

    #region Drawing

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(device, nameof(device));
        ArgumentNullException.ThrowIfNull(spriteBatch, nameof(spriteBatch));

        bool isPixelated = SkyConfig.Instance.PixelatedStars;
        Vector2 screenSize = MiscUtils.ScreenSize;
        Vector2 renderSize = isPixelated ? MiscUtils.HalfScreenSize : screenSize;
        Vector2 screenCenter = MiscUtils.HalfScreenSize;

        PrepareARenderTarget_AndListenToEvents(ref _target, device, (int)renderSize.X, (int)renderSize.Y, (RenderTargetUsage)1);

        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);

        spriteBatch.BeginToggledHalfScale(SpriteSortMode.Deferred, BlendState.AlphaBlend, isPixelated);

        float alpha = StarSystem.StarAlpha;
        if (alpha > 0)
            DrawStars(spriteBatch, screenCenter, alpha);

            // Only draw supernovae if theres any as to prevent sb restarts.
        if (StarSystem.Stars.Any(s => s.SupernovaProgress > SupernovaProgress.Shrinking))
        {
            spriteBatch.End();
            spriteBatch.BeginToggledHalfScale(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, isPixelated);

            DrawSupernovae(spriteBatch, screenCenter, alpha);
        }

        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    #region Stars

    public static void DrawStars(SpriteBatch spriteBatch, Vector2 center, float alpha)
    {
        Texture2D flareTexture = Textures.Star.Value;
        Vector2 flareOrigin = flareTexture.Size() * 0.5f;

        Texture2D bloomTexture = Textures.SunBloom.Value;
        Vector2 bloomOrigin = bloomTexture.Size() * 0.5f;

        bool vanillaStyle = SkyConfig.Instance.VanillaStyleStars;

            // Feels way too bright without this.
        if (vanillaStyle)
            alpha *= 0.7f;

        foreach (InteractableStar star in StarSystem.Stars.Where(s => s.SupernovaProgress <= SupernovaProgress.Shrinking))
        {
            Vector2 position = center + star.GetRotatedPosition();

            float twinklePhase = star.Twinkle + Main.GlobalTimeWrappedHourly / TwinkleFrequencyDivisor;
            float twinkle = (MathF.Sin(twinklePhase) * TwinkleAmplitude) + TwinkleBaseMultiplier;

            float scale = star.BaseSize * (1 - MathF.Pow(star.SupernovaTimer, 3)) * twinkle;

            Color color = star.GetColor() * star.BaseSize * alpha;

            Color primaryFlareColor = (color * PrimaryFlareOpacity) with { A = 0 };
            spriteBatch.Draw(flareTexture, position, null, primaryFlareColor, 0, flareOrigin, scale / PrimaryFlareScaleDivisor, SpriteEffects.None, 0f);

            Color secondaryFlareColor = (color * SecondaryFlareOpacity) with { A = 0 };
            spriteBatch.Draw(flareTexture, position, null, secondaryFlareColor, 0, flareOrigin, scale / SecondaryFlareScaleDivisor, SpriteEffects.None, 0f);

            if (vanillaStyle)
            {
                Texture2D starTexture = TextureAssets.Star[star.StarType].Value;
                Vector2 starOrigin = starTexture.Size() * 0.5f;

                float rotation = star.Rotation;

                spriteBatch.Draw(starTexture, position, null, color, rotation, starOrigin, scale / StarScaleDivisor, SpriteEffects.None, 0f);
            }
        }
    }

    #endregion

    #region Supernovae

    public static void DrawSupernovae(SpriteBatch spriteBatch, Vector2 center, float alpha)
    {
        Effect supernova = Shaders.Supernova.Value;

        if (supernova is null)
            return;

            // Set all of the generic color info.
        supernova.Parameters["background"]?.SetValue(Background);

        supernova.Parameters["ringStartColor"]?.SetValue(RingStart);
        supernova.Parameters["ringEndColor"]?.SetValue(new Vector4(5.5f, 1.8f, .5f, .5f));

        Texture2D texture = Textures.SupernovaNoise.Value;

        Vector2 origin = texture.Size() * 0.5f;

        foreach (InteractableStar star in StarSystem.Stars.Where(s => s.SupernovaProgress == SupernovaProgress.Exploding))
        {
            float time = star.SupernovaTimer / star.BaseSize;
            Vector2 position = center + star.GetRotatedPosition();

            supernova.Parameters["noisePosition"]?.SetValue(position / MiscUtils.ScreenSize);

                // Multiply the Vector4 and not the Color to give values past 1.

            supernova.Parameters["startColor"]?.SetValue(star.Color.ToVector4() * ExplosionStart * 0.1f);
            supernova.Parameters["endColor"]?.SetValue(star.Color.ToVector4() * ExplosionEnd);

                // Where is my saturate method.
            supernova.Parameters["quickTime"]?.SetValue(MathF.Min(time * QuickTimeMultiplier, 1f));
            supernova.Parameters["expandTime"]?.SetValue(MathF.Min(time * ExpandTimeMultiplier, 1f));
            supernova.Parameters["ringTime"]?.SetValue(MathF.Min(time * RingTimeMultiplier, 1f));
            supernova.Parameters["longTime"]?.SetValue(time);

            supernova.CurrentTechnique.Passes[0].Apply();

            float opacity = alpha + (0.6f / star.BaseSize);

            float rotation = star.Rotation;

            spriteBatch.Draw(texture, position, null, Color.White * opacity, rotation, origin, 0.26f * star.BaseSize, SpriteEffects.None, 0f);
        }
    }

    #endregion

    #endregion
}
