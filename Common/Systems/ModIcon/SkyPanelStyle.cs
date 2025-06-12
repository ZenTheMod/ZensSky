using Daybreak.Common.Features.ModPanel;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.Utilities;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.ModIcon;

public sealed class SkyPanelStyle : ModPanelStyleExt
{
    #region Private Fields

    private static readonly Color OutlineColor = new(21, 30, 39);

    private static readonly Color DisabledColor = new(133, 103, 74);
    private static readonly Color EnabledColor = new(199, 174, 144);

    private static readonly Color ClearColor = new(135, 135, 135);

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

    private static RenderTarget2D? PanelTarget;

    #endregion

    #region Loading

    public override void Unload() =>
        PanelTarget?.Dispose();

    #endregion

    #region Color/Texture Changes

    public override bool PreSetHoverColors(UIModItem element, bool hovered)
    {
        element.BorderColor = OutlineColor;
        element.BackgroundColor = Color.White;

        return false;
    }

    public override UIText ModifyModName(UIModItem element, UIText modName)
    {
        modName.TextColor = EnabledColor;
        modName.ShadowColor = OutlineColor;

        return modName;
    }

    public override UIImage? ModifyModIcon(UIModItem element, UIImage modIcon, ref int modIconAdjust) => null;

    public override Dictionary<TextureKind, Asset<Texture2D>> TextureOverrides { get; } = new()
    {
        { TextureKind.ModInfo, Textures.ModInfo },
        { TextureKind.ModConfig, Textures.ModConfig },
        { TextureKind.Deps, Textures.ModDeps },
        { TextureKind.InnerPanel, Textures.Invis }
    };

    public override Color ModifyEnabledTextColor(bool enabled, Color color) => enabled ? EnabledColor : DisabledColor;

    #endregion

    #region Drawing

    public override bool PreDrawPanel(UIModItem element, SpriteBatch spriteBatch)
    {
        if (element._needsTextureLoading)
        {
            element._needsTextureLoading = false;
            element.LoadTextures();
        }

        CalculatedStyle dims = element.GetDimensions();

        Vector2 size = Vector2.Transform(new(dims.Width, dims.Height), Main.UIScaleMatrix);
        Vector2 position = Vector2.Transform(new(dims.X, dims.Y), Main.UIScaleMatrix);

        Rectangle source = new((int)position.X, (int)position.Y, 
            (int)size.X, (int)size.Y);

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        using (new RenderTargetSwap(ref PanelTarget, (int)size.X, (int)size.Y))
        {
            device.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

                // I don't trust clear to work on all devices.
            spriteBatch.Draw(Textures.Pixel.Value, new Rectangle(0, 0, (int)size.X, (int)size.Y), ClearColor);

            DrawStars(spriteBatch, source);

            DrawPlanet(spriteBatch, source);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            DrawCreases(spriteBatch, source);

            spriteBatch.End();
        }

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, snapshot.RasterizerState, null, Main.UIScaleMatrix);

        DrawPanel(element, spriteBatch, device, source);

        spriteBatch.Restart(in snapshot);

        element.DrawPanel(spriteBatch, element._borderTexture.Value, element.BorderColor);

        return false;
    }

    private static void DrawPanel(UIModItem element, SpriteBatch spriteBatch, GraphicsDevice device, Rectangle source)
    {
        Effect panel = Shaders.Panel.Value;

        if (panel is null)
            return;

        panel.Parameters["Source"]?.SetValue(new Vector4(source.Width, source.Height, source.X, source.Y));

        panel.CurrentTechnique.Passes[0].Apply();

        device.Textures[1] = PanelTarget ?? TextureAssets.MagicPixel.Value;

        device.Textures[2] = Textures.PanelGradient.Value;
        device.SamplerStates[2] = SamplerState.PointClamp;

        element.DrawPanel(spriteBatch, element._backgroundTexture.Value, element.BackgroundColor);
    }

    #region Stars

    private static void DrawStars(SpriteBatch spriteBatch, Rectangle source)
    {
        UnifiedRandom rand = new("guh".GetHashCode());

        int starCount = StarCount;

        float time = Main.GlobalTimeWrappedHourly * StarTimeMultiplier;

        Texture2D star = Textures.Star.Value;
        Vector2 starOrigin = star.Size() * 0.5f;

        for (int i = 0; i < starCount; i++)
        {
            Vector2 starPosition = new(rand.NextFloat(source.Width), rand.NextFloat(source.Height));

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

    private static void DrawPlanet(SpriteBatch spriteBatch, Rectangle source)
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

        Vector2 position = new(PlanetHorizontalOffset, source.Height * .5f);

        spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, PlanetScale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Creases

    private void DrawCreases(SpriteBatch spriteBatch, Rectangle source)
    {
        UnifiedRandom rand = new("Ebon will never steal my code.".GetHashCode());

        Texture2D crease = Textures.SunBloom.Value;
        Vector2 creaseOrigin = crease.Size() * 0.5f;

        Color color = (Color.White * CreaseOpacity) with { A = 0 };

        for (int i = 0; i < CreaseCount; i++)
        {
            Vector2 creasePosition = new(rand.NextFloat(source.Width), rand.NextFloat(source.Height));

            spriteBatch.Draw(crease, creasePosition, null, color, CreaseRotation, creaseOrigin, CreaseScale, SpriteEffects.None, 0f);
        }
    }

    #endregion

    #endregion
}
