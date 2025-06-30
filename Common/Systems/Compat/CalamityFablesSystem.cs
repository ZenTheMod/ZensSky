using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Registries;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonRenderingSystem;

namespace ZensSky.Common.Systems.Compat;

[Autoload(Side = ModSide.Client)]
public sealed class CalamityFablesSystem : ModSystem
{
    #region Private Fields

    private const float SingleMoonPhase = .125f;

        // private const float MoonRadius = .9f;
        // private const float MoonAtmosphere = .1f;

    private const float CystAtmosphere = .175f;

    private static readonly Vector4 AtmosphereColor = new(.3f, .35f, .35f, 1f);
    private static readonly Vector4 AtmosphereShadowColor = new(.1f, .02f, .06f, 1f);

    private static readonly Color DarkAtmosphere = new(13, 69, 96);

    #endregion

    #region Public Properties

    public static int PriorMoonStyles { get; private set; }

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load() 
    {
        PriorMoonStyles = TextureAssets.Moon.Length;

        if (!ModLoader.HasMod("CalamityFables"))
            return;

        IsEnabled = true;

            // I don't feel like adding a project reference for a massive mod just for moon styles of all things.
        Assembly fablessAsm = ModLoader.GetMod("CalamityFables").Code;

        Type? moddedMoons = fablessAsm.GetType("CalamityFables.Core.ModdedMoons");

        FieldInfo? vanillaMoonCount = moddedMoons?.GetField("VanillaMoonCount", Public | Static);

        PriorMoonStyles = (int?)vanillaMoonCount?.GetValue(null) ?? PriorMoonStyles;
    }

    #endregion

    public static bool IsEdgeCase()
    {
        return (Main.moonType - PriorMoonStyles) switch
        {
            1 => true,
            2 => true,
            8 => true,
            9 => true,
            10 => true,
            13 => true,
            14 => true,
            _ => false
        };
    }

    #region Drawing

        // Handle a bunch of edge cases for moons with non standard visuals.
    public static void DrawMoon(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        switch (Main.moonType - PriorMoonStyles)
        {
            case 1:
                DrawDark(spriteBatch, moon, position, rotation, scale);
                break;
            case 8:
                DrawShatter(spriteBatch, moon, position, color, rotation, scale, moonColor, shadowColor, device);
                break;
            case 9:
                DrawCyst(spriteBatch, moon, position, rotation, scale, moonColor, shadowColor);
                break;
        }
    }

        // To maintain consistency with Fables I've used the light atmosphere color to act as Dark's outline and decided to not show the shadow atmosphere color.
    private static void DrawDark(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, float rotation, float scale)
    {
        ApplyPlanetShader(Main.moonPhase * SingleMoonPhase, Color.Black, DarkAtmosphere, Color.Transparent);

        Vector2 size = new(MoonSize * scale);
        spriteBatch.Draw(moon, position, null, Color.White, rotation, moon.Size() * .5f, size, SpriteEffects.None, 0f);
    }

    private static void DrawShatter(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        Effect shatter = Shaders.Shatter.Value;

        if (shatter is null)
            return;

        spriteBatch.End(out var snapshot);

        device.Textures[0] = Textures.Pixel.Value;
        device.RasterizerState = RasterizerState.CullNone;

        Viewport viewport = device.Viewport;
        Vector2 screenSize = new(viewport.Width, viewport.Height);
        shatter.Parameters["screenSize"]?.SetValue(screenSize);

        Matrix projection = CalculateMoonMatrix(position, screenSize, rotation, scale);
        shatter.Parameters["projection"]?.SetValue(projection);

        shatter.CurrentTechnique.Passes[0]?.Apply();

        Models.Shatter.Value?.Draw(device);

        spriteBatch.Begin(in snapshot);
    }

    private static Matrix CalculateMoonMatrix(Vector2 position, Vector2 screenSize, float rotation, float scale)
    {
        bool flip = Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically);

        Vector3 size = new(new Vector2(MoonSize * scale * 10) / screenSize, 1);

        Vector2 translation = position - (screenSize * .5f);
        translation /= screenSize * .5f;

        return
            Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up) *
            Matrix.CreateOrthographicOffCenter(-1, 1, 1, -1, -1, 0) *
            Matrix.CreateRotationZ(rotation) *
            Matrix.CreateScale(size) *
            Matrix.CreateTranslation(translation.X, translation.Y, 0f) *
            Matrix.CreateScale(1f, flip ? 1f : -1f, 1f);
    }

        // TODO: Allow ApplyPlanetShader to take an Effect arg or create a seperate ApplyPlanetShaderParameters method.
    private static void DrawCyst(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, float rotation, float scale, Color moonColor, Color shadowColor)
    {
        Effect planet = Shaders.Cyst.Value;

        if (planet is null)
            return;

        planet.Parameters["atmosphereRange"]?.SetValue(CystAtmosphere);

        float shadowAngle = Main.moonPhase * SingleMoonPhase;
        planet.Parameters["shadowRotation"]?.SetValue(-shadowAngle * MathHelper.TwoPi);

        planet.Parameters["shadowColor"]?.SetValue(shadowColor.ToVector4());
        planet.Parameters["atmosphereColor"]?.SetValue(AtmosphereColor);

        Vector4 atmoShadowColor = SkyConfig.Instance.TransparentMoonShadow ? Color.Transparent.ToVector4() : AtmosphereShadowColor;
        planet.Parameters["atmosphereShadowColor"]?.SetValue(atmoShadowColor);

        planet.CurrentTechnique.Passes[0].Apply();

        Vector2 size = new Vector2(MoonSize * scale) / moon.Size();
        spriteBatch.Draw(moon, position, null, moonColor, rotation, moon.Size() * .5f, size, SpriteEffects.None, 0f);
    }

    #endregion
}
