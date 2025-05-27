using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Utilities;
using static ZensSky.Common.Registries.Textures;
using static ZensSky.Common.Registries.Shaders;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.SunAndMoon;

[Autoload(Side = ModSide.Client)]
public sealed class SunAndMoonRenderingSystem : ModSystem
{
    #region Private Fields

    private static readonly Color SkyColor = new(128, 168, 248);

    private const int SunTopBuffer = 50;

    private static readonly Vector2[] FlareScales = [new(6f, 0.02f), new(3.3f, 0.09f), new(2f, 0.06f)];
    private static readonly float[] FlareOpacities = [0.6f, 0.3f, 1f];

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private const float SunOuterGlowScale = 0.35f;
    private const float SunOuterGlowOpacity = 0.2f;
    private const float SunInnerGlowScale = 0.23f;
    private const float SunInnerGlowColorMultiplier = 3.4f;
    private const float SunHugeGlowScale = 0.7f;
    private const float SunHugeGlowOpacity = 0.25f;

    private static readonly float[] EclipseBloomScales = [0.36f, 0.27f, 0.2f];
    private static readonly float[] EclipseColorMultipliers = [0.2f, 1f, 1.6f];

    private const float EclipseTendrilsScale = 0.16f;

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

    #endregion

    #region Loading

    public override void Load() => Main.QueueMainThreadAction(() => On_Main.DrawSunAndMoon += DrawSunAndMoonToSky);

    public override void Unload() => Main.QueueMainThreadAction(() => On_Main.DrawSunAndMoon -= DrawSunAndMoonToSky);

    #endregion

    #region Drawing

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
            DrawSun(spriteBatch, position, color, rotation, scale, distanceFromCenter, distanceFromTop, device);
        else
            DrawMoon(spriteBatch, position, color, rotation, scale, moonColor, moonShadowColor, device);
    }

    #region Sun Drawing

    public static void DrawSun(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, float distanceFromCenter, float distanceFromTop, GraphicsDevice device)
    {
        if (SkyConfig.Instance.RealisticSun)
            return;

        if (Main.eclipse)
        {
            DrawEclipse(spriteBatch, position, color, rotation, scale, device);
            return;
        }

        color.A = 0;

        float offscreenMultiplier = Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);

        #region Bloom

        Texture2D bloom = SunBloom.Value;
        Vector2 bloomOrigin = bloom.Size() * 0.5f;

        Color outerGlowColor = color * SunOuterGlowOpacity;
        spriteBatch.Draw(bloom, position, null, outerGlowColor, 0, bloomOrigin, SunOuterGlowScale * scale, SpriteEffects.None, 0f);

        Color innerGlowColor = color * (1 + (distanceFromCenter * SunInnerGlowColorMultiplier));
        spriteBatch.Draw(bloom, position, null, innerGlowColor, 0, bloomOrigin, SunInnerGlowScale * scale, SpriteEffects.None, 0f);

        float hugeGlowMultiplier = Main.atmo * distanceFromCenter;
        Color hugeGlowColor = color * SunHugeGlowOpacity * offscreenMultiplier * hugeGlowMultiplier;
        spriteBatch.Draw(bloom, position, null, hugeGlowColor, 0, bloomOrigin, SunHugeGlowScale * hugeGlowMultiplier * scale, SpriteEffects.None, 0f);

        #endregion

        #region Flare

        // This draws a similar effect to that seen in 1.4.5 leaks.
        float flareWidth = distanceFromCenter * distanceFromTop * offscreenMultiplier;

        for (int i = 0; i < FlareScales.Length; i++)
        {
            Vector2 flareScale = new(FlareScales[i].X * flareWidth, FlareScales[i].Y);
            Color flareColor = color * FlareOpacities[i];
            spriteBatch.Draw(bloom, position, null, flareColor, 0, bloomOrigin, flareScale * scale, SpriteEffects.None, 0f);
        }

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

    private static void DrawEclipse(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, GraphicsDevice device)
    {
        Texture2D bloom = SunBloom.Value;
        Vector2 bloomOrigin = bloom.Size() * 0.5f;

        color.A = 0;

        for (int i = 0; i < EclipseBloomScales.Length; i++)
            spriteBatch.Draw(bloom, position, null, color * EclipseColorMultipliers[i], 0, bloomOrigin, scale * EclipseBloomScales[i], SpriteEffects.None, 0f);

        if (SkyConfig.Instance.EclipseMode)
        {
            Effect coronaries = Eclipse.Value;

            if (coronaries is null)
                return;

            coronaries.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            coronaries.CurrentTechnique.Passes[0].Apply();

            spriteBatch.Draw(bloom, position, null, Color.Black, 0, bloomOrigin, scale * EclipseTendrilsScale, SpriteEffects.None, 0f);
        }
        else
            DrawMoon(spriteBatch, position, Color.Black, rotation, scale, Color.Black, Color.Black, device);
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

        rings.Parameters["uAngle"]?.SetValue(Main.moonPhase * SingleMoonPhase * MathHelper.TwoPi);

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

    private void DrawSunAndMoonToSky(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
    {
        if (StarSystem.CanDrawStars)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            GraphicsDevice device = Main.instance.GraphicsDevice;

            spriteBatch.End(out var snapshot);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

            DrawSunAndMoon(spriteBatch, device);

            spriteBatch.Restart(in snapshot);
        }

        orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
    }

    #endregion
}
