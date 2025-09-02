using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Systems;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonRendering;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonHooks;

namespace ZensSky.Common.Systems.Compat;

[Autoload(Side = ModSide.Client)]
public sealed class CalamityFablesSystem : ModSystem
{
    #region Private Fields

    private const float SingleMoonPhase = .125f;

    private static readonly Color DarkAtmosphere = new(13, 69, 96);

        // private const float MoonRadius = .9f;

    private const float ShatterScale = 1.35f;

    private static readonly Vector4 AtmosphereColor = Vector4.Zero;
    private static readonly Vector4 AtmosphereShadowColor = new(.1f, .02f, .06f, 1f);

    private static readonly Vector2 ShatterTargetSize = new(200);

    private static RenderTarget2D? ShatterTarget;

    #endregion

    #region Public Properties

    public static int PriorMoonStyles { get; private set; }

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // CalamityFables is a Both-Sided mod, meaning we cannot deliberatly load before or after it with build.txt sorting.
    public override void PostSetupContent() 
    {
        PriorMoonStyles = TextureAssets.Moon.Length;

        if (!ModLoader.TryGetMod("CalamityFables", out Mod fables))
            return;

        IsEnabled = true;

        Assembly fablessAsm = fables.Code;

        Type? moddedMoons = fablessAsm.GetType("CalamityFables.Core.ModdedMoons");
        ArgumentNullException.ThrowIfNull(moddedMoons);

        FieldInfo? vanillaMoonCount = moddedMoons?.GetField("VanillaMoonCount", Public | Static);
        ArgumentNullException.ThrowIfNull(vanillaMoonCount);

        int? count = (int?)vanillaMoonCount.GetValue(null);
        ArgumentNullException.ThrowIfNull(count);

        PriorMoonStyles = (int)count;

        for (int i = 0; i < FablesTextures.Moon.Length; i++)
            AddMoonStyle(PriorMoonStyles + i, FablesTextures.Moon[i]);

        PreDrawMoon += MoonsFablesPreDraw;
    }

    public override void Unload() =>
        MainThreadSystem.Enqueue(() => ShatterTarget?.Dispose());

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
    private static bool MoonsFablesPreDraw(
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
        if (!nonEventMoon || !IsEdgeCase())
            return true;

        switch (Main.moonType - PriorMoonStyles)
        {
            case 1:
                DrawDark(spriteBatch, moon.Value, position, rotation, scale);
                return false;
            case 8:
                DrawShatter(spriteBatch, moon.Value, position, color, rotation, scale, moonColor, shadowColor, device);
                return false;
            case 9:
                DrawCyst(spriteBatch, moon.Value, position, rotation, scale, moonColor, shadowColor);
                return false;
        }

        return true;
    }

        // To maintain consistency with Fables I've used the light atmosphere color to act as Dark's outline and decided to not show the shadow atmosphere color.
    private static void DrawDark(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, float rotation, float scale)
    {
        ApplyPlanetShader(Main.moonPhase * SingleMoonPhase, Color.Black, DarkAtmosphere, Color.Transparent);

        Vector2 size = new(MoonSize * scale);
        spriteBatch.Draw(moon, position, null, Color.White, rotation, moon.Size() * .5f, size, SpriteEffects.None, 0f);
    }

        // To maintain consistency with Fables I have implemented a .obj filetype reader to import 3D models into Terraria.
    private static void DrawShatter(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        if (!CompatEffects.Shatter.IsReady)
            return;

        spriteBatch.End(out var snapshot);

        using (new RenderTargetSwap(ref ShatterTarget, (int)ShatterTargetSize.X, (int)ShatterTargetSize.Y, preferredDepthFormat: DepthFormat.Depth16))
        {
            device.Clear(Color.Transparent);

                // The texture of the broken chunks.
            device.Textures[0] = moon;

            device.RasterizerState = RasterizerState.CullNone;
            
            device.DepthStencilState = DepthStencilState.Default;

            Viewport viewport = device.Viewport;
            Vector2 screenSize = new(viewport.Width, viewport.Height);
            CompatEffects.Shatter.ScreenSize = screenSize;

            Matrix projection = CalculateShatterMatrix();
            CompatEffects.Shatter.Projection = projection;

            CompatEffects.Shatter.Color = color.ToVector4();
            CompatEffects.Shatter.ShadowColor = shadowColor.ToVector4();

            CompatEffects.Shatter.InnerColor = Color.Red.ToVector4();

            float shadowAngle = Main.moonPhase * SingleMoonPhase;
            CompatEffects.Shatter.ShadowRotation = -shadowAngle * MathHelper.TwoPi;

            CompatEffects.Shatter.Apply();

            Models.Shatter.DrawMoon(device);

            Models.Shatter.DrawRocks(device);

                // The "Black Hole" in the center.
            device.Textures[0] = MiscTextures.Pixel;

            Models.Shatter.DrawShaderPlane(device);
        }

        spriteBatch.Begin(in snapshot);

        Vector2 size = new Vector2(MoonSize * scale * ShatterScale) / ShatterTargetSize;
        spriteBatch.Draw(ShatterTarget, position, null, Color.White, rotation, ShatterTarget.Size() * .5f, size, SpriteEffects.None, 0f);
    }

    private static Matrix CalculateShatterMatrix() => 
        Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY) *
        Matrix.CreateOrthographicOffCenter(-1, 1, 1, -1, -1, 1);

        // TODO: Allow ApplyPlanetShader to take an Effect arg or create a seperate ApplyPlanetShaderParameters method. (Scrapped in favour of Sourcegen implementaion(?))
    private static void DrawCyst(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, float rotation, float scale, Color moonColor, Color shadowColor)
    {
        if (!CompatEffects.Cyst.IsReady)
            return;

        float shadowAngle = Main.moonPhase * SingleMoonPhase;
        CompatEffects.Cyst.ShadowRotation = -shadowAngle * MathHelper.TwoPi;

        CompatEffects.Cyst.ShadowColor = shadowColor.ToVector4();
        CompatEffects.Cyst.AtmosphereColor = AtmosphereColor;

        Vector4 atmoShadowColor = SkyConfig.Instance.TransparentMoonShadow ? Color.Transparent.ToVector4() : AtmosphereShadowColor;
        CompatEffects.Cyst.AtmosphereShadowColor = atmoShadowColor;

        CompatEffects.Cyst.Apply();

        Vector2 size = new Vector2(MoonSize * scale) / moon.Size();
        spriteBatch.Draw(moon, position, null, moonColor, rotation, moon.Size() * .5f, size, SpriteEffects.None, 0f);
    }

    #endregion
}
