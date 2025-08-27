using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Systems;
using ZensSky.Core.Systems.ModCall;
using ZensSky.Core.Utils;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonHooks;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.SunAndMoon;

public static class SunAndMoonRendering
{
    #region Private Fields

    private static readonly Color SkyColor = new(128, 168, 248);

    private const int SunTopBuffer = 50;

    private static readonly Vector2[] FlareScales = [new(3f, .02f), new(1.15f, .09f), new(1f, .06f)];
    private static readonly float[] FlareOpacities = [.6f, .3f, 1f];

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private const float SunOuterGlowScale = .35f;
    private const float SunOuterGlowOpacity = .2f;
    private const float SunInnerGlowScale = .23f;
    private const float SunInnerGlowColorMultiplier = 3.4f;
    private const float SunHugeGlowScale = .7f;
    private const float SunHugeGlowOpacity = .25f;

    private static readonly float[] EclipseBloomScales = [.36f, .27f, .2f];
    private static readonly float[] EclipseColorMultipliers = [.2f, 1f, 1.6f];

    private const float SunglassesScale = .3f;

    private const float SingleMoonPhase = .125f;

    private const float MoonRadius = .88f;

        // I've just started using Vector4s over colors for shaders, I'm far too lazy to convert it.
    private static readonly Vector4 AtmosphereColor = Vector4.Zero;
    private static readonly Vector4 AtmosphereShadowColor = new(.2f, .04f, .12f, 1f);

    private static readonly Vector2 SmileyLeftEyePosition = new(-24, -32);
    private static readonly Vector2 SmileyRightEyePosition = new(13, -44);

    private const float SmileyPhase = .3125f;

    private static readonly Vector2 Moon2ExtraRingSize = new(.28f, .07f);
    private const float Moon2ExtraRingRotation = .13f;
    private const float Moon2ExtraShadowExponent = 15f;
    private const float Moon2ExtraShadowSize = 4.6f;

    private const float Moon8Scale = .74f;

    private static readonly Vector2 Moon8ExtraUpperPosition = new(-22, -19);
    private static readonly Vector2 Moon8ExtraLowerPosition = new(25);
    private const float Moon8ExtraUpperScale = .22f;
    private const float Moon8ExtraLowerScale = .41f;

    private const int BetterNightSkyMoonSize = 202;

    #endregion

    #region Public Properties

    public static int MoonSize
    {
        get
        {
                // TODO: Move to BetterNightSkySystem.UseBigMoonTexture
            if (NonEventMoon &&
                BetterNightSkySystem.IsEnabled &&
                BetterNightSkySystem.UseBigMoon)
                return BetterNightSkyMoonSize;

            return TextureAssets.Moon[NonEventMoon ? Main.moonType : 0].Value.Width + 12;
        }
    }

    #endregion

    #region Loading

    [OnLoad(Side = ModSide.Client)]
    public static void Load()
    {
        MainThreadSystem.Enqueue(() =>
            On_Main.DrawSunAndMoon += DrawSunAndMoonToSky);

        PreDrawSun += SunEclipsePreDraw;
        PostDrawSun += SunSunglassesPostDraw;

        PreDrawMoon += Moon2PreDraw;
        PostDrawMoon += Moon2PostDraw;

        PreDrawMoon += Moon8PreDraw;

        PreDrawMoon += MoonGetFixedBoiPreDraw;
    }

    [OnUnload(Side = ModSide.Client)]
    public static void Unload() =>
        MainThreadSystem.Enqueue(() => On_Main.DrawSunAndMoon -= DrawSunAndMoonToSky);

    #endregion

    #region Drawing

    [ModCall]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DrawSunAndMoon(SpriteBatch spriteBatch, GraphicsDevice device, bool showSun, bool showMoon)
    {
        ForceInfo = false;

        if (showSun)
            DrawSun(spriteBatch, device);
        if (showMoon)
            DrawMoon(spriteBatch, device);
    }

    #region Sun Drawing

    [ModCall]
    public static void DrawSun(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        Viewport viewport = device.Viewport;

        float centerX = viewport.Width * .5f;
        float distanceFromCenter = MathF.Abs(centerX - Info.SunPosition.X) / centerX;

        float distanceFromTop = (Info.SunPosition.Y + SunTopBuffer) / viewport.Height;

        DrawSun(spriteBatch, Info.SunPosition, Info.SunColor, Info.SunRotation, Info.SunScale, distanceFromCenter, distanceFromTop, device);
    }

    [ModCall]
    public static void DrawSun(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, float distanceFromCenter, float distanceFromTop, GraphicsDevice device)
    {
        color.A = 0;

        if (InvokePreDrawSun(spriteBatch, ref position, ref color, ref rotation, ref scale, device))
        {
            float offscreenMultiplier = Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);

            #region Bloom

            Texture2D bloom = SkyTextures.SunBloom.Value;
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
        }

        InvokePostDrawSun(spriteBatch, position, color, rotation, scale, device);
    }

    #region Eclipse

    private static bool SunEclipsePreDraw(SpriteBatch spriteBatch, ref Vector2 position, ref Color color, ref float rotation, ref float scale, GraphicsDevice device)
    {
        if (!Main.eclipse)
            return true;

        Texture2D bloom = SkyTextures.SunBloom.Value;
        Vector2 bloomOrigin = bloom.Size() * .5f;

        color.A = 0;

        for (int i = 0; i < EclipseBloomScales.Length; i++)
            spriteBatch.Draw(bloom, position, null, color * EclipseColorMultipliers[i], 0, bloomOrigin, scale * EclipseBloomScales[i], SpriteEffects.None, 0f);

        DrawMoon(spriteBatch, position, Color.Black, rotation, scale, Color.Black, Color.Black, device);

        return false;
    }

    #endregion

    #region Sunglasses

    public static void SunSunglassesPostDraw(
        SpriteBatch spriteBatch,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        GraphicsDevice device)
    {
        if (Main.gameMenu || Main.LocalPlayer.head == 12)
            return;

        Texture2D sunglasses = SkyTextures.Sunglasses.Value;
        spriteBatch.Draw(sunglasses, position, null, Color.White, rotation, sunglasses.Size() * .5f, SunglassesScale * scale, SpriteEffects.None, 0f);
    }

    #endregion

    #endregion

    #region Moon Drawing

    [ModCall]
    public static void DrawMoon(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        Color skyColor = Main.ColorOfTheSkies.MultiplyRGB(SkyColor);

        Color moonShadowColor = SkyConfig.Instance.TransparentMoonShadow ? Color.Transparent : skyColor;
        Color moonColor = Info.MoonColor * Info.MoonScale;
        moonColor.A = 255;

        DrawMoon(spriteBatch, Info.MoonPosition, Info.MoonColor, Info.MoonRotation, Info.MoonScale, moonColor, moonShadowColor, device);
    }

    [ModCall]
    public static void DrawMoon(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        Asset<Texture2D> moon = MoonTexture;

        device.Textures[1] = MiscTextures.Pixel;

        bool canDrawEdgeCases = NonEventMoon;

        if (InvokePreDrawMoon(spriteBatch, ref moon, ref position, ref color, ref rotation, ref scale, ref moonColor, ref shadowColor, device, canDrawEdgeCases))
        {
            Vector2 size = new Vector2(MoonSize * scale) / moon.Value.Size();

            ApplyPlanetShader(Main.moonPhase * SingleMoonPhase, shadowColor);
            spriteBatch.Draw(moon.Value, position, null, moonColor, rotation, moon.Value.Size() * .5f, size, SpriteEffects.None, 0f);
        }

        InvokePostDrawMoon(spriteBatch, moon, position, color, rotation, scale, moonColor, shadowColor, device, canDrawEdgeCases);
    }

    public static void ApplyPlanetShader(float shadowAngle, Color shadowColor, Color? atmosphereColor = null, Color? atmosphereShadowColor = null)
    {
        if (!SkyEffects.Planet.IsReady)
            return;

        SkyEffects.Planet.Radius = MoonRadius;

        SkyEffects.Planet.ShadowRotation = -shadowAngle * MathHelper.TwoPi;

        SkyEffects.Planet.ShadowColor = shadowColor.ToVector4();
        SkyEffects.Planet.AtmosphereColor = atmosphereColor?.ToVector4() ?? AtmosphereColor;

        Vector4 atmoShadowColor = SkyConfig.Instance.TransparentMoonShadow ? 
            Color.Transparent.ToVector4() : 
            (atmosphereShadowColor?.ToVector4() ?? AtmosphereShadowColor);
        SkyEffects.Planet.AtmosphereShadowColor = atmoShadowColor;

        SkyEffects.Planet.Apply();
    }

    #region Vanilla Extras

    #region Moon-GetFixedBoi Extras

    private static bool MoonGetFixedBoiPreDraw(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        GraphicsDevice device,
        bool nonEventMoon)
    {
            // Unsure if the GFB moon overrides event moons.
        if (!WorldGen.drunkWorldGen || !nonEventMoon)
            return true;

        Texture2D texture = SkyTextures.Moon[0];

        Texture2D star = StarTextures.FourPointedStar.Value;

        Vector2 starLeftOffset = SmileyLeftEyePosition.RotatedBy(rotation) * scale;
        Vector2 starRightOffset = SmileyRightEyePosition.RotatedBy(rotation) * scale;

        spriteBatch.Draw(star, position + starLeftOffset, null, (moonColor * .4f) with { A = 0 }, 0, star.Size() * .5f, scale * .33f, SpriteEffects.None, 0f);
        spriteBatch.Draw(star, position + starRightOffset, null, (moonColor * .4f) with { A = 0 }, 0, star.Size() * .5f, scale * .33f, SpriteEffects.None, 0f);

        spriteBatch.Draw(star, position + starLeftOffset, null, color with { A = 0 }, MathHelper.PiOver4, star.Size() * .5f, scale * .2f, SpriteEffects.None, 0f);
        spriteBatch.Draw(star, position + starRightOffset, null, color with { A = 0 }, MathHelper.PiOver4, star.Size() * .5f, scale * .2f, SpriteEffects.None, 0f);

        ApplyPlanetShader(SmileyPhase, shadowColor);

        Vector2 size = new Vector2(MoonSize * scale) / texture.Size();
        spriteBatch.Draw(texture, position, null, moonColor, rotation - MathHelper.PiOver2, texture.Size() * .5f, size, SpriteEffects.None, 0f);

        return false;
    }

    #endregion

    #region Moon-2 Extras

    private static bool Moon2PreDraw(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        GraphicsDevice device,
        bool nonEventMoon)
    {
        if (Main.moonType != 2 || !nonEventMoon)
            return true;

        Texture2D rings = SkyTextures.Rings.Value;

        DrawMoon2Rings(spriteBatch, rings, position, rings.Frame(1, 2, 0, 0), rotation - Moon2ExtraRingRotation, rings.Size() * .5f, scale, moonColor, shadowColor);

        return true;
    }

    private static void Moon2PostDraw(
        SpriteBatch spriteBatch,
        Asset<Texture2D> moon,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        Color moonColor,
        Color shadowColor,
        GraphicsDevice device,
        bool nonEventMoon)
    {
        if (Main.moonType != 2 || !nonEventMoon)
            return;

        Texture2D rings = SkyTextures.Rings.Value;

        DrawMoon2Rings(spriteBatch, rings, position, rings.Frame(1, 2, 0, 1), rotation - Moon2ExtraRingRotation, new(rings.Width * .5f, 0f), scale, moonColor, shadowColor);
    }

    private static void DrawMoon2Rings(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Vector2 position,
        Rectangle frame,
        float rotation,
        Vector2 origin,
        float scale,
        Color moonColor,
        Color shadowColor)
    {
        if (!SkyEffects.Rings.IsReady)
            return;

        SkyEffects.Rings.Angle = Main.moonPhase * SingleMoonPhase * MathHelper.TwoPi;

        SkyEffects.Rings.ShadowColor = shadowColor.ToVector4();
        SkyEffects.Rings.ShadowExponent = Moon2ExtraShadowExponent;
        SkyEffects.Rings.ShadowSize = Moon2ExtraShadowSize;

        SkyEffects.Rings.Apply();

        spriteBatch.Draw(texture, position, frame, moonColor, rotation, origin, Moon2ExtraRingSize * scale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Moon-8 Extras

    private static bool Moon8PreDraw(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        GraphicsDevice device,
        bool nonEventMoon)
    {
        if (Main.moonType != 8 || !nonEventMoon)
            return true;

        ApplyPlanetShader(Main.moonPhase * SingleMoonPhase, shadowColor);

        Texture2D texture = moon.Value;

        Vector2 upperMoonOffset = Moon8ExtraUpperPosition.RotatedBy(rotation) * scale;
        Vector2 lowerMoonOffset = Moon8ExtraLowerPosition.RotatedBy(rotation) * scale;

        Vector2 origin = moon.Size() * .5f;
        Vector2 size = new Vector2(MoonSize * scale) / texture.Size();

        spriteBatch.Draw(texture, position + upperMoonOffset, null, moonColor, rotation, origin, size * Moon8ExtraUpperScale, SpriteEffects.None, 0f);
        spriteBatch.Draw(texture, position + lowerMoonOffset, null, moonColor, rotation, origin, size * Moon8ExtraLowerScale, SpriteEffects.None, 0f);

        scale *= Moon8Scale;

        return true;
    }

    #endregion

    #endregion

    #endregion

    private static void DrawSunAndMoonToSky(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
    {
        if (!ZensSky.CanDrawSky || 
            RedSunSystem.IsEnabled || 
            !SkyConfig.Instance.SunAndMoonRework || 
            (!ShowSun && !ShowMoon))
        {
            if (!RedSunSystem.IsEnabled)
            {
                ShowSun = true;
                ShowMoon = true;
            }

            if (RealisticSkySystem.IsEnabled && Main.dayTime)
                RealisticSkySystem.DrawSun();

            orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

        DrawSunAndMoon(spriteBatch, device, Main.dayTime && ShowSun, !Main.dayTime && ShowMoon);

        spriteBatch.Restart(in snapshot);

        orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);

        ShowSun = true;
        ShowMoon = true;
    }

    #endregion
}
