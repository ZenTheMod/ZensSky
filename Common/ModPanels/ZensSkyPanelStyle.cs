using Daybreak.Common.Features.ModPanel;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Sky.Space;
using ZensSky.Core;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Particles;
using ZensSky.Core.Utils;
using Star = ZensSky.Common.DataStructures.Star;

namespace ZensSky.Common.ModPanels;

public sealed class ZensSkyPanelStyle : ModPanelStyleExt
{
    #region Private Fields

    private static RenderTarget2D? PanelTarget;

    private static readonly Color PanelOutlineColor = new(76, 76, 76, 76);
    private static readonly Color PanelHoverOutlineColor = new(100, 80, 90, 0);

    private static readonly Color BackgroundColor = new(78, 62, 130);
    private static readonly Color BackgroundGradientColor = new(64, 48, 22, 0);

    private static readonly Vector2 BranchPosition = new(-14, 10);
    private static readonly Vector2 BranchOrigin = new(-12, 47);

    private const float BranchRotationFrequency = 2.1f;
    private const float BranchRotationAmplitude = .06f;

    private static readonly Color ForegroundGradientColor = new(117, 81, 47, 0);

    private const int LeafCount = 55;
    private static readonly ParticleHandler<SakuraLeafParticle> Leaves = new(LeafCount);

    private const int LeafSpawnChance = 30;

    private const float LeafSpawnOffsetX = -15f;

    private const int WindCount = 45;
    private static readonly ParticleHandler<WindParticle> Winds = new(WindCount);

    private const int WindSpawnChance = 55;

    private const float WindSpawnOffsetXMin = -1000f;
    private const float WindSpawnOffsetXMax = -400f;

    private const int StarCount = 240;
    private static readonly Star[] Stars = new Star[StarCount];

    private static bool GeneratedStars = false;

    private const float StarRotationIncrement = .00045f;
    private static float StarRotation;

    #endregion

    #region Loading

    public override void Unload() => 
        MainThreadSystem.Enqueue(() => PanelTarget?.Dispose());

    #endregion

    #region Initialization

    public override void PostInitialize(UIModItem element)
    {
        element.OnUpdate += Update;

        GeneratedStars = false;
    }

    #endregion

    #region Color/Texture Changes

    public override bool PreSetHoverColors(UIModItem element, bool hovered)
    {
            // Use the default blue because it looks nicer.
        element.BackgroundColor = hovered ? UICommon.DefaultUIBlueMouseOver : UICommon.DefaultUIBlue;

        return false;
    }

        // Remove the panel behind the enable toggle.
    public override Dictionary<TextureKind, Asset<Texture2D>> TextureOverrides { get; } =
         new() { {TextureKind.InnerPanel, MiscTextures.Invis} };

    public override UIImage? ModifyModIcon(UIModItem element, UIImage modIcon, ref int modIconAdjust) => null;

    #endregion

    #region Updating

    private void Update(UIElement element)
    {
        Vector2 size = element.Dimensions.Size();

        UpdateLeafs(size);
        UpdateWinds(size);

        UpdateStars(size);
    }

    #region Particles

    private static void UpdateLeafs(Vector2 size)
    {
        Leaves.Update();

        if (!Main.rand.NextBool(LeafSpawnChance))
            return;

        Vector2 position = new(LeafSpawnOffsetX, Main.rand.NextFloat(-size.Y * .3f, size.Y));

        Leaves.Spawn(new(position));
    }

    private static void UpdateWinds(Vector2 size)
    {
        Winds.Update();

        if (!Main.rand.NextBool(WindSpawnChance))
            return;

        Vector2 position =
            new(Main.rand.NextFloat(WindSpawnOffsetXMin, WindSpawnOffsetXMax),
            Main.rand.NextFloat(-size.Y * .1f, size.Y * 1.1f));

        Winds.Spawn(new(position, .6f, false));
    }

    #endregion

    private static void UpdateStars(Vector2 size)
    {
        StarRotation += StarRotationIncrement;
        StarRotation %= MathHelper.TwoPi;

            // Regenerate stars if applicable.
        if (GeneratedStars)
            return;

        GeneratedStars = true;

        Vector2 center = new(size.X * .5f, size.Y);

        float radius = center.Length();

        for (int i = 0; i < StarCount; i++)
            Stars[i] = new(Main.rand, radius);
    }

    #endregion

    #region Drawing

    public override bool PreDrawPanel(UIModItem element, SpriteBatch spriteBatch, ref bool drawDivider)
    {
        if (element._needsTextureLoading)
        {
            element._needsTextureLoading = false;
            element.LoadTextures();
        }

        if (!UIEffects.Panel.IsReady)
            return true;

        Rectangle dims = element.Dimensions;

            // Make sure the panel draws correctly on any scale.
        Vector2 size = Vector2.Transform(dims.Size(), Main.UIScaleMatrix);
        Vector2 position = Vector2.Transform(dims.Position(), Main.UIScaleMatrix);

        Rectangle source = new((int)position.X, (int)position.Y, 
            (int)size.X, (int)size.Y);

        spriteBatch.End(out var snapshot);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        using (new RenderTargetSwap(ref PanelTarget, (int)size.X, (int)size.Y))
        {
            device.Clear(Color.Transparent);

            DrawSkyPanel(spriteBatch, device, size);
        }

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, snapshot.RasterizerState, null, Main.UIScaleMatrix);

        UIEffects.Panel.Source = new(source.Width, source.Height, source.X, source.Y);

        UIEffects.Panel.Apply();

        device.Textures[1] = PanelTarget;
        device.SamplerStates[1] = SamplerState.PointClamp;

        element.DrawPanel(spriteBatch, element._backgroundTexture.Value, element.BackgroundColor);
        element.DrawPanel(spriteBatch, element._borderTexture.Value, element.BorderColor);

        spriteBatch.Restart(snapshot);

            // Additional border that stands out more.
        element.DrawPanel(spriteBatch, element._borderTexture.Value, element.IsMouseHovering ? PanelHoverOutlineColor : PanelOutlineColor);

            // Draw our faded divider.
        drawDivider = false;

        Rectangle innerDimensions = element.InnerDimensions;

        Rectangle dividerSize = new(innerDimensions.X + 5 + element._modIconAdjust, innerDimensions.Y + 30, innerDimensions.Width - 10 - element._modIconAdjust, 4);

        spriteBatch.Draw(PanelStyleTextures.Divider, dividerSize, Color.White);

        return false;
    }

    private static void DrawSkyPanel(SpriteBatch spriteBatch, GraphicsDevice device, Vector2 size)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

        Rectangle background = new(0, 0, (int)size.X, (int)size.Y);

            // Vauge sunset gradient.
        spriteBatch.Draw(MiscTextures.Pixel, background, BackgroundColor);
        spriteBatch.Draw(SkyTextures.SkyGradient, background, BackgroundGradientColor);

            // Draw background stars.
        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(snapshot with { TransformMatrix = RotationMatrix(size) });

        StarRendering.DrawStars(spriteBatch, .2f, -StarRotation, Stars, SkyConfig.Instance.StarStyle);

        spriteBatch.Restart(snapshot with { SamplerState = SamplerState.PointClamp });

            // Branch that rotates around an origin out of frame.
        Vector2 branchPosition = BranchPosition + (Vector2.UnitY * size.Y * .5f);

        float branchRotation = MathF.Sin(Main.GlobalTimeWrappedHourly * BranchRotationFrequency) * BranchRotationAmplitude;

        spriteBatch.Draw(PanelStyleTextures.Branch, branchPosition, null, Color.White, branchRotation, BranchOrigin, 1f, SpriteEffects.None, 0f);

            // Draw the falling leaves.
        Leaves.Draw(spriteBatch, device);

            // And faint wind particles.
        spriteBatch.End();

        device.Textures[0] = SkyTextures.SunBloom;
        device.SamplerStates[0] = SamplerState.LinearClamp;

            // TODO: Not this.
        Color oldSkyColor = Main.ColorOfTheSkies;
        Main.ColorOfTheSkies = Color.White;

        Winds.Draw(spriteBatch, device);

        Main.ColorOfTheSkies = oldSkyColor;

        spriteBatch.Begin(in snapshot);

            // Vauge foreground light.
        spriteBatch.Draw(SkyTextures.SkyGradient, background, ForegroundGradientColor);

        spriteBatch.End();
    }

    #endregion

    #region Private Methods

    private static Matrix RotationMatrix(Vector2 size)
    {
        Matrix rotation = Matrix.CreateRotationZ(StarRotation);
        Matrix offset = Matrix.CreateTranslation(new(size.X * .5f, size.Y, 0f));

        return Matrix.Identity * rotation * offset;
    }

    #endregion
}
