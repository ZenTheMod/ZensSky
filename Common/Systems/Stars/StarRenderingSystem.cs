using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Compat;
using ZensSky.Common.Utilities;
using Star = ZensSky.Common.DataStructures.Star;

namespace ZensSky.Common.Systems.Stars;

[Autoload(Side = ModSide.Client)]
public sealed class StarRenderingSystem : ModSystem
{
    #region Private Fields

    private const float VanillaStarsOpacity = 0.7f;

    private const float TwinkleFrequencyDivisor = 12f;
    private const float TwinkleAmplitude = 0.2f;
    private const float TwinkleBaseMultiplier = 1f;

    private const float StarScaleDivisor = 1.3f;
    private const float PrimaryFlareOpacity = 0.13f;
    private const float PrimaryFlareScaleDivisor = 4.15f;
    private const float SecondaryFlareOpacity = 0.6f;
    private const float SecondaryFlareScaleDivisor = 6f;

    private static readonly Vector4 ExplosionStart = new(1.5f, 2.5f, 4f, 1f);
    private static readonly Vector4 ExplosionEnd = new(1.4f, .25f, 2.2f, .7f);
    private static readonly Vector4 RingStart = new(3.5f, 2.9f, 1f, 1f);
    private static readonly Vector4 RingEnd = new(4.5f, 1.8f, .5f, .5f);

    private static readonly Vector4 Background = new(0, 0, 0, 0);

    private const float QuickTimeMultiplier = 20f;
    private const float ExpandTimeMultiplier = 13.3f;
    private const float RingTimeMultiplier = 6.6f;

    private const float MinimumSupernovaAlpha = 0.6f;

    private const float SupernovaScale = 0.27f;

    #endregion

    #region Loading

    public override void Load() => Main.QueueMainThreadAction(() => On_Main.DrawStarsInBackground += DrawStarsToSky);

    public override void Unload() => Main.QueueMainThreadAction(() => On_Main.DrawStarsInBackground -= DrawStarsToSky);

    #endregion

    #region Drawing

    #region Stars

    public static void DrawStars(SpriteBatch spriteBatch, Vector2 center, float alpha)
    {
        Texture2D flareTexture = Textures.Star.Value;
        Vector2 flareOrigin = flareTexture.Size() * 0.5f;

        float flareRotation = -StarSystem.StarRotation;

        Texture2D bloomTexture = Textures.SunBloom.Value;
        Vector2 bloomOrigin = bloomTexture.Size() * 0.5f;

        bool vanillaStyle = SkyConfig.Instance.VanillaStyleStars;

            // Feels way too bright without this.
        if (vanillaStyle)
            alpha *= VanillaStarsOpacity;

        foreach (Star star in StarSystem.Stars.Where(s => s.SupernovaProgress != SupernovaProgress.Exploding))
        {
            Vector2 position = center + star.Position;

            float twinklePhase = star.Twinkle + Main.GlobalTimeWrappedHourly / TwinkleFrequencyDivisor;
            float twinkle = (MathF.Sin(twinklePhase) * TwinkleAmplitude) + TwinkleBaseMultiplier;

            float scale = star.BaseSize * (1 - MathF.Pow(star.SupernovaTimer, 3)) * twinkle;

            Color color = star.GetColor() * star.BaseSize * alpha;

            Color primaryFlareColor = (color * PrimaryFlareOpacity) with { A = 0 };
            spriteBatch.Draw(flareTexture, position, null, primaryFlareColor, flareRotation, flareOrigin, scale / PrimaryFlareScaleDivisor, SpriteEffects.None, 0f);

            Color secondaryFlareColor = (color * SecondaryFlareOpacity) with { A = 0 };
            spriteBatch.Draw(flareTexture, position, null, secondaryFlareColor, flareRotation, flareOrigin, scale / SecondaryFlareScaleDivisor, SpriteEffects.None, 0f);

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
        supernova.Parameters["ringEndColor"]?.SetValue(RingEnd);

        supernova.Parameters["globalTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.SetAtmosphereParams(supernova);

        Texture2D texture = Textures.SupernovaNoise.Value;

        Vector2 origin = texture.Size() * 0.5f;

        foreach (Star star in StarSystem.Stars.Where(s => s.SupernovaProgress == SupernovaProgress.Exploding))
        {
            float time = star.SupernovaTimer / star.BaseSize;
            Vector2 position = center + star.Position;

                // Multiply the Vector4 and not the Color to give values past 1.
            supernova.Parameters["startColor"]?.SetValue(star.Color.ToVector4() * ExplosionStart);
            supernova.Parameters["endColor"]?.SetValue(star.Color.ToVector4() * ExplosionEnd);

            supernova.Parameters["quickTime"]?.SetValue(MathF.Min(time * QuickTimeMultiplier, 1f));
            supernova.Parameters["expandTime"]?.SetValue(MathF.Min(time * ExpandTimeMultiplier, 1f));
            supernova.Parameters["ringTime"]?.SetValue(MathF.Min(time * RingTimeMultiplier, 1f));
            supernova.Parameters["longTime"]?.SetValue(time);

            supernova.Parameters["offset"]?.SetValue(position / MiscUtils.ScreenSize);

            supernova.CurrentTechnique.Passes[0].Apply();

            float opacity = alpha + (MinimumSupernovaAlpha / star.BaseSize);

            float rotation = star.Rotation;

            spriteBatch.Draw(texture, position, null, Color.White * opacity, rotation, origin, SupernovaScale * star.BaseSize, SpriteEffects.None, 0f);
        }
    }

    #endregion

    private void DrawStarsToSky(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
    {
        if (!StarSystem.CanDrawStars)
        {
            orig(self, sceneArea, artificial);
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;

        float alpha = StarSystem.StarAlpha;

        Vector2 screenCenter = MiscUtils.HalfScreenSize;

        spriteBatch.End(out var snapshot);

        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.DrawStars();

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix * RotationMatrix());

        RealisticSkySystem.ApplyStarShader();

        if (alpha > 0)
            DrawStars(spriteBatch, screenCenter, alpha);

        if (StarSystem.Stars.Any(s => s.SupernovaProgress > SupernovaProgress.Shrinking))
            DrawSupernovae(spriteBatch, screenCenter, alpha);

            // The batches here are a bit fucked but idc.
        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.DrawGalaxy();

        if (BetterNightSkySystem.IsEnabled)
            BetterNightSkySystem.DrawSpecialStars(alpha);

        spriteBatch.Restart(in snapshot);
    }

    #endregion

    #region Public Methods

    public static Matrix RotationMatrix()
    {
        Matrix rotation = Matrix.CreateRotationZ(StarSystem.StarRotation);
        Matrix offset = Matrix.CreateTranslation(new(MiscUtils.HalfScreenSize, 0f));
        Matrix revoffset = Matrix.CreateTranslation(new(-MiscUtils.HalfScreenSize, 0f));

        return revoffset * rotation * offset * Main.BackgroundViewMatrix.EffectMatrix;
    }

    #endregion
}
