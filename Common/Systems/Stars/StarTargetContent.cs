using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using ZensSky.Common.Config;
using ZensSky.Common.Utilities;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.Stars;

public sealed class StarTargetContent : ARenderTargetContentByRequest
{
    #region Private Fields

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

            // if (RealisticSkyCompatSystem.RealisticSkyEnabled)
            //    DrawRealisticSky(spriteBatch, device, screenSize, renderSize, isPixelated, alpha);
            // else
        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

        // private static void DrawRealisticSky(SpriteBatch spriteBatch, GraphicsDevice device, Vector2 screenSize, Vector2 renderSize, bool isPixelated, float alpha)
        // {
        //     DrawRealisticGalaxy(screenSize * GalaxyScaleMultiplier, screenSize.X);
        // 
        //     Vector2 sunPosition = SunAndMoonSystem.SunMoonPosition;
        //     float falloff = Main.eclipse ? EclipseFalloff : NormalFalloff;
        //
        //     if (isPixelated)
        //     {
        //         sunPosition *= 0.5f;
        //         falloff *= 0.5f;
        //     }
        //
        //     spriteBatch.End();
        // 
        //     DrawRealisticStars(device, alpha * RealisticStarAlphaMultiplier, renderSize, sunPosition, Matrix.Identity, Main.GlobalTimeWrappedHourly, falloff, true);
        // }

    public static void DrawStars(SpriteBatch spriteBatch, Vector2 center, float alpha)
    {
        Texture2D flareTexture = Textures.Star.Value;
        Vector2 flareOrigin = flareTexture.Size() * 0.5f;

        ReadOnlySpan<InteractableStar> starSpan = StarSystem.Stars.AsSpan();

        bool vanillaStyle = SkyConfig.Instance.VanillaStyleStars;

        for (int i = 0; i < starSpan.Length; i++)
        {
            InteractableStar star = starSpan[i];

            if (star.SupernovaProgress > SupernovaProgress.Shrinking)
                continue;

            Vector2 position = center + star.GetRotatedPosition();

            float twinklePhase = star.Twinkle + Main.GlobalTimeWrappedHourly / TwinkleFrequencyDivisor;
            float twinkle = (MathF.Sin(twinklePhase) * TwinkleAmplitude) + TwinkleBaseMultiplier;

            float scale = star.BaseSize * (1 - star.SupernovaTimer) * twinkle;

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
}
