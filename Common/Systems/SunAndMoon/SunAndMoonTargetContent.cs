using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Common.Utilities;
using static ZensSky.Common.Registries.Textures;
using static ZensSky.Common.Registries.Shaders;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.SunAndMoon;

public sealed class SunAndMoonTargetContent : ARenderTargetContentByRequest
{
    #region Private Fields

    private static readonly Color SkyColor = new(128, 168, 248);

    private const int SunTopBuffer = 50;

    private const float PrimaryFlareScaleLength = 6f;
    private const float PrimaryFlareScaleWidth = 0.02f;
    private const float PrimaryFlareScaleOpacity = 0.6f;
    private const float SecondaryFlareScaleLength = 3.3f;
    private const float SecondaryFlareScaleWidth = 0.09f;
    private const float SecondaryFlareScaleOpacity = 0.3f;
    private const float InnerFlareScaleLength = 2f;
    private const float InnerFlareScaleWidth = 0.06f;

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private const float SunOuterGlowScale = 0.35f;
    private const float SunOuterGlowOpacity = 0.2f;
    private const float SunInnerGlowScale = 0.23f;
    private const float SunInnerGlowOpacityMultiplier = 4f;

    private const float PrimaryEclipseScale = 0.4f;
    private const float SecondaryEclipseScale = 0.3f;
    private const float TertiaryEclipseScale = 0.25f;

    private const float EclipseTendrilsScale = 0.2f;
    private const float EclipseTendrilsMin = -0.7f;
    private const float EclipseTendrilsMax = 1f;

    private const float SunglassesScale = 0.3f;

    private const float MoonBrightness = 16f;

    private const float SingleMoonPhase = 0.125f; // = 1/8
    private const float StartingMoonPhase = 0.25f;

    private const int MoonScale = 50;
    private const float MoonFallOff = 0.95f;
    private const float MoonBloomScale = 1.1f;
    private const float MoonBloomFallOff = 0.8f;
    private const float MoonBloomOpacity = 0.2f;

    private static readonly Vector2 SmileyLeftEyePosition = new(-24, -32);
    private static readonly Vector2 SmileyRightEyePosition = new(13, -44);

    private static readonly Vector2 Moon2ExtraRingSize = new(0.28f, 0.07f);
    private const float Moon2ExtraRingRotation = 0.13f;
    private const float Moon2ExtraShadowExponent = 15f;
    private const float Moon2ExtraShadowSize = 5.1f;

    private static readonly Vector2 Moon8ExtraUpperPosition = new(-30, -26);
    private static readonly Vector2 Moon8ExtraLowerPosition = new(34);
    private const float Moon8ExtraUpperScale = 0.3f;
    private const float Moon8ExtraLowerScale = 0.45f;

    private const float ShootingStarTrailLength = 5f;
    private const float ShootingStarTrailWidth = 0.01f;
    private const float ShootingStarBrightnessDivisor = 20f;
    private const float ShootingStarLifetimeMultiplier = 0.5f;

    #endregion

    #region Requesting

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(device, nameof(device));
        ArgumentNullException.ThrowIfNull(spriteBatch, nameof(spriteBatch));

        bool isPixelated = SkyConfig.Instance.PixelatedSunAndMoon;
        Vector2 screenSize = MiscUtils.ScreenSize;
        Vector2 renderSize = isPixelated ? MiscUtils.HalfScreenSize : screenSize;

        PrepareARenderTarget_AndListenToEvents(ref _target, device, (int)renderSize.X, (int)renderSize.Y, (RenderTargetUsage)1);

        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);

        spriteBatch.BeginToggledHalfScale(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, isPixelated);

        DrawSunAndMoon(spriteBatch, device);

        spriteBatch.End();

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }

    #endregion

    public static void DrawSunAndMoon(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        Vector2 position = SunMoonPosition;
        Color color = SunMoonColor;
        float rotation = SunMoonRotation;
        float scale = SunMoonScale;

        float centerX = MiscUtils.HalfScreenSize.X;
        float distanceFromCenter = MathF.Abs(centerX - position.X) / centerX;

        float distanceFromTop = (position.Y + SunTopBuffer) / SceneAreaSize.Y;

        Color skyColor = Main.ColorOfTheSkies.MultiplyRGB(SkyColor);

        Color moonShadowColor = SkyConfig.Instance.TransparentMoonShadow ? Color.Transparent : skyColor;
        Color moonColor = color * MoonBrightness * scale;
        moonColor.A = 255;

        if (Main.dayTime)
            DrawSun(spriteBatch, position, color, scale, distanceFromCenter, distanceFromTop, device);
        else
            DrawMoon(spriteBatch, position, color, rotation, scale, moonColor, moonShadowColor, device);
    }

    #region Sun Drawing

    public static void DrawSun(SpriteBatch spriteBatch, Vector2 position, Color color, float scale, float distanceFromCenter, float distanceFromTop, GraphicsDevice device)
    {
        if (Main.eclipse)
        {
            DrawEclipse(spriteBatch, position, color, scale, device);
            return;
        }

        #region Bloom

        Texture2D bloom = SunBloom.Value;
        Vector2 bloomOrigin = bloom.Size() * 0.5f;

        Color outerGlowColor = (color * SunOuterGlowOpacity) with { A = 0 };
        spriteBatch.Draw(bloom, position, null, outerGlowColor, 0, bloomOrigin, SunOuterGlowScale * scale, SpriteEffects.None, 0f);

        Color innerColor = (color * (1 + (distanceFromCenter * SunInnerGlowOpacityMultiplier))) with { A = 0 };
        spriteBatch.Draw(bloom, position, null, innerColor, 0, bloomOrigin, SunInnerGlowScale * scale, SpriteEffects.None, 0f);

        #endregion

        #region Flare

            // This draws a similar effect to that seen in 1.4.5 leaks.
        float flareWidth = distanceFromCenter * distanceFromTop * Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);

        Vector2 primaryFlareScale = new(PrimaryFlareScaleLength * flareWidth, PrimaryFlareScaleWidth);
        Color primaryFlareColor = (color * PrimaryFlareScaleOpacity) with { A = 0 };
        spriteBatch.Draw(bloom, position, null, primaryFlareColor, 0, bloomOrigin, primaryFlareScale * scale, SpriteEffects.None, 0f);

        Vector2 secondaryFlareScale = new(SecondaryFlareScaleLength * flareWidth, SecondaryFlareScaleWidth);
        Color secondaryFlareColor = (color * SecondaryFlareScaleOpacity) with { A = 0 };
        spriteBatch.Draw(bloom, position, null, secondaryFlareColor, 0, bloomOrigin, secondaryFlareScale * scale, SpriteEffects.None, 0f);

        Vector2 innerFlareScale = new(InnerFlareScaleLength * flareWidth, InnerFlareScaleWidth);
        spriteBatch.Draw(bloom, position, null, color with { A = 0 }, 0, bloomOrigin, innerFlareScale * scale, SpriteEffects.None, 0f);

        #endregion

        #region Sungls

            // Not ideal to check every frame.
        if (!Main.gameMenu && Main.LocalPlayer.head == 12)
        {
            Texture2D sunglasses = Sunglasses.Value;
            spriteBatch.Draw(sunglasses, position, null, Color.White, 0, sunglasses.Size() * 0.5f, SunglassesScale * scale, SpriteEffects.None, 0f);
        }

        #endregion
    }

    #region Eclipse

    private static void DrawEclipse(SpriteBatch spriteBatch, Vector2 position, Color color, float scale, GraphicsDevice device)
    {
        Texture2D bloom = SunBloom.Value;
        Vector2 bloomOrigin = bloom.Size() * 0.5f;

        // TODO: For loop.
        spriteBatch.Draw(bloom, position, null, (color * 0.2f) with { A = 0 }, 0, bloomOrigin, scale * PrimaryEclipseScale, SpriteEffects.None, 0f);

        spriteBatch.Draw(bloom, position, null, color with { A = 0 }, 0, bloomOrigin, scale * SecondaryEclipseScale, SpriteEffects.None, 0f);

        spriteBatch.Draw(bloom, position, null, (color * 1.6f) with { A = 0 }, 0, bloomOrigin, scale * TertiaryEclipseScale, SpriteEffects.None, 0f);

        Effect coronaries = Eclipse.Value;

        if (coronaries is null)
            return;

        coronaries.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly * 1.2f);

        coronaries.CurrentTechnique.Passes[0].Apply();

        spriteBatch.Draw(bloom, position, null, Color.Black, 0, bloomOrigin, scale * EclipseTendrilsScale, SpriteEffects.None, 0f);
    }

    #endregion

    #endregion

    #region Moon Drawing

    public static void DrawMoon(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        if (WorldGen.drunkWorldGen)
        {
            DrawSmiley(spriteBatch, position, color, rotation, scale, moonColor, shadowColor, device);
            return;
        }

        Texture2D moon = Moon[Main.moonType].Value;

        Texture2D rings = Moon2Rings.Value;

        if (Main.pumpkinMoon)
            moon = PumpkinMoon.Value;
        else if (Main.snowMoon)
            moon = SnowMoon.Value;

        Effect planet = Planet.Value;

        if (planet is null)
            return;

            // This code is kinda soup. 😋
        if (Main.moonType == 2)
            DrawMoon2Extras(spriteBatch, rings, position, rings.Frame(1, 2, 0, 0), rotation - Moon2ExtraRingRotation, rings.Size() * 0.5f, scale, moonColor, shadowColor);

        PlanetSetup(planet, StartingMoonPhase, StartingMoonPhase + (Main.moonPhase * SingleMoonPhase), shadowColor, device);

        if (Main.moonType == 8)
            DrawMoon8Extras(spriteBatch, moon, position, rotation, scale, moonColor);

        Vector2 size = new Vector2(MoonScale * scale) / moon.Size();
        spriteBatch.Draw(moon, position, null, moonColor, rotation, moon.Size() * 0.5f, size, SpriteEffects.None, 0f);

        DrawBloom(spriteBatch, position, rotation, scale * MoonBloomScale, moonColor, planet);

        if (Main.moonType == 8)
            DrawMoon8ExtrasBloom(spriteBatch, position, rotation, scale, moonColor);

        if (Main.moonType == 2)
            DrawMoon2Extras(spriteBatch, rings, position, rings.Frame(1, 2, 0, 1), rotation - Moon2ExtraRingRotation, new(rings.Width * 0.5f, 0f), scale, moonColor, shadowColor);
    }

    #region GetFixedBoi Moon

    private static void DrawSmiley(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        Texture2D moon = Moon[0].Value;
        Texture2D star = Textures.Star.Value;

        Vector2 starLeftOffset = SmileyLeftEyePosition.RotatedBy(rotation) * scale;
        Vector2 starRightOffset = SmileyRightEyePosition.RotatedBy(rotation) * scale;

        spriteBatch.Draw(star, position + starLeftOffset, null, (moonColor * 0.4f) with { A = 0 }, 0, star.Size() * 0.5f, scale / 3f, SpriteEffects.None, 0f);
        spriteBatch.Draw(star, position + starRightOffset, null, (moonColor * 0.4f) with { A = 0 }, 0, star.Size() * 0.5f, scale / 3f, SpriteEffects.None, 0f);

        spriteBatch.Draw(star, position + starLeftOffset, null, color with { A = 0 }, MathHelper.PiOver4, star.Size() * 0.5f, scale / 5f, SpriteEffects.None, 0f);
        spriteBatch.Draw(star, position + starRightOffset, null, color with { A = 0 }, MathHelper.PiOver4, star.Size() * 0.5f, scale / 5f, SpriteEffects.None, 0f);

        Effect planet = Planet.Value;

        if (planet is null)
            return;

        PlanetSetup(planet, StartingMoonPhase, SingleMoonPhase * 5f, shadowColor, device);

        Vector2 size = new Vector2(MoonScale * scale) / moon.Size();
        spriteBatch.Draw(moon, position, null, moonColor, rotation - MathHelper.PiOver2, moon.Size() * 0.5f, size, SpriteEffects.None, 0f);

        DrawBloom(spriteBatch, position, rotation - MathHelper.PiOver2, scale * MoonBloomScale, moonColor, planet);
    }

    #endregion

    #region Rings

    private static void DrawMoon2Extras(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, float rotation, Vector2 origin, float scale, Color moonColor, Color shadowColor)
    {
        Effect rings = Rings.Value;

        if (rings is null)
            return;

        rings.Parameters["uAngle"]?.SetValue(-Main.moonPhase * SingleMoonPhase * MathHelper.TwoPi);

        rings.Parameters["ShadowColor"]?.SetValue(shadowColor.ToVector4());
        rings.Parameters["ShadowExponent"]?.SetValue(Moon2ExtraShadowExponent);
        rings.Parameters["ShadowSize"]?.SetValue(Moon2ExtraShadowSize);

        rings.CurrentTechnique.Passes[0].Apply();

        spriteBatch.Draw(texture, position, frame, moonColor, rotation, origin, Moon2ExtraRingSize * scale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Moon8 Extras

    private static void DrawMoon8Extras(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float rotation, float scale, Color moonColor)
    {
        Vector2 upperMoonOffset = Moon8ExtraUpperPosition.RotatedBy(rotation) * scale;
        Vector2 lowerMoonOffset = Moon8ExtraLowerPosition.RotatedBy(rotation) * scale;

        Vector2 origin = texture.Size() * 0.5f;
        Vector2 size = new Vector2(MoonScale * scale) / texture.Size();

        spriteBatch.Draw(texture, position + upperMoonOffset, null, moonColor, rotation, origin, size * Moon8ExtraUpperScale, SpriteEffects.None, 0f);
        spriteBatch.Draw(texture, position + lowerMoonOffset, null, moonColor, rotation, origin, size * Moon8ExtraLowerScale, SpriteEffects.None, 0f);
    }

    private static void DrawMoon8ExtrasBloom(SpriteBatch spriteBatch, Vector2 position, float rotation, float scale, Color moonColor)
    {
        Vector2 upperMoonOffset = Moon8ExtraUpperPosition.RotatedBy(rotation) * scale;
        Vector2 lowerMoonOffset = Moon8ExtraLowerPosition.RotatedBy(rotation) * scale;

        Texture2D texture = Pixel.Value;

        Vector2 origin = texture.Size() * 0.5f;
        Vector2 size = new(MoonScale * scale * MoonBloomScale);

        Color bloomColor = (moonColor * MoonBloomOpacity) with { A = 0 };

        spriteBatch.Draw(texture, position + upperMoonOffset, null, bloomColor, rotation, origin, size * Moon8ExtraUpperScale, SpriteEffects.None, 0f);
        spriteBatch.Draw(texture, position + lowerMoonOffset, null, bloomColor, rotation, origin, size * Moon8ExtraLowerScale, SpriteEffects.None, 0f);
    }

    #endregion

    private static void PlanetSetup(Effect planet, float baseAngle, float shadowAngle, Color shadowColor, GraphicsDevice device)
    {
        planet.Parameters["shadowColor"]?.SetValue(shadowColor.ToVector4());

        planet.Parameters["planetRotation"]?.SetValue(baseAngle);
        planet.Parameters["shadowRotation"]?.SetValue(shadowAngle);

        planet.Parameters["falloffStart"]?.SetValue(MoonFallOff);

        planet.CurrentTechnique.Passes[0].Apply();

        device.SamplerStates[0] = SamplerState.LinearWrap;
    }

    private static void DrawBloom(SpriteBatch spriteBatch, Vector2 position, float rotation, float scale, Color moonColor, Effect planet)
    {
        planet.Parameters["shadowColor"]?.SetValue(Color.Transparent.ToVector4());
        planet.Parameters["falloffStart"]?.SetValue(MoonBloomFallOff);

        planet.CurrentTechnique.Passes[0].Apply();

        Texture2D texture = Pixel.Value;

        Vector2 size = new(MoonScale * scale);

        spriteBatch.Draw(texture, position, null, (moonColor * MoonBloomOpacity) with { A = 0 }, rotation, texture.Size() * 0.5f, size, SpriteEffects.None, 0f);
    }

    #endregion
}
