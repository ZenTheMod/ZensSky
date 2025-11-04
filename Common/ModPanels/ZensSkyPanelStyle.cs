using Daybreak.Common.Features.ModPanel;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
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
using static System.Reflection.BindingFlags;
using Star = ZensSky.Common.DataStructures.Star;

namespace ZensSky.Common.ModPanels;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="ReorderUIModList"/><br/>
///         Reorders <see cref="UIMods.modList"/> to be placed after all buttons.
///     </item>
/// </list>
/// </summary>
public sealed class ZensSkyPanelStyle : ModPanelStyleExt
{
    #region Private Fields

    private delegate void orig_OnInitialize(UIMods self);
    private static Hook? PatchOnInitialize;

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

    #region Particles

        // Leaves.
    private const int LeafCount = 55;
    private static readonly ParticleHandler<SakuraLeafParticle> Leaves = new(LeafCount);

    private const int LeafSpawnChance = 30;

    private const float LeafSpawnOffsetXMin = -100f;
    private const float LeafSpawnOffsetXMax = -13f;

        // Hover Leaves.
    private const int LeafHoverTime = 135;
    private static int LeafHoverTimer;

    private const int LeafHoverCountMin = 4;
    private const int LeafHoverCountMax = 12;

    private static readonly Vector2 LeafHoverVelocity = new(28, 0);

        // Wind.
    private const int WindCount = 45;
    private static readonly ParticleHandler<WindParticle> Winds = new(WindCount);

    private const int WindSpawnChance = 55;

    private const float WindSpawnOffsetXMin = -1000f;
    private const float WindSpawnOffsetXMax = -400f;

    #endregion

    #region Stars

    private const int StarCount = 240;
    private static readonly Star[] Stars = new Star[StarCount];

    private static bool GeneratedStars = false;

    private const float StarRotationIncrement = .00045f;
    private static float StarRotation;

    #endregion

    #region Bird

    private static BirdState BirdState;

    private static readonly Vector2 BirdBranchOffset = new(35, 44);
    private static readonly Vector2 BirdOrigin = new(15, 22);

    private static Vector2 BirdPosition;

    private const float BirdVelocityMultiplier = 1.07f;
    private const float BirdMaxVelocitySqr = 12f * 12f;
    private static readonly Vector2 BirdDirection = new(.9f, -1.5f);
    private static Vector2 BirdVelocity;

    private const int BirdFrames = 5;
    private const int BirdFlyingFrames = 4;
    private const int BirdFrameTime = 6;
    private static int BirdFrame;
    private static int BirdFrameTimer;

    #endregion

    #endregion

    #region Loading

    public override void Load()
    {
        MethodInfo? onInitialize = typeof(UIMods).GetMethod(nameof(UIMods.OnInitialize), Instance | Public);

        if (onInitialize is not null)
            PatchOnInitialize = new(onInitialize,
                ReorderUIModList);
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
            PanelTarget?.Dispose());

        PatchOnInitialize?.Dispose();
    }

    private void ReorderUIModList(orig_OnInitialize orig, UIMods self)
    {
        orig(self);

            // Move the mod list to the front to have it drawn after certain buttons.

        self.uIPanel.RemoveChild(self.modList);
        self.uIPanel.Append(self.modList);
    }

    #endregion

    #region Initialization

    public override void PostInitialize(UIModItem element)
    {
        element.OnUpdate += Update;
        element.OnMouseOver += OnHover;

        ResetBird();

        GeneratedStars = false;
    }

    private static void ResetBird()
    {
        BirdState = Main.rand.NextBool() ? BirdState.None : BirdState.Idle;
        BirdVelocity = Vector2.Zero;
        BirdFrame = BirdFrames - 1;
        BirdFrameTimer = 0;
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

    private static void Update(UIElement element)
    {
        Vector2 size = element.Dimensions.Size();

        UpdateLeafs(size);
        UpdateWinds(size);

        UpdateStars(size);

        UpdateBird(element);
    }

    #region Particles

    private static void UpdateLeafs(Vector2 size)
    {
        Leaves.Update();

        if (LeafHoverTimer > 0)
            LeafHoverTimer--;

        if (!Main.rand.NextBool(LeafSpawnChance))
            return;

        SpawnLeaf(size);
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

    #region Stars

    private static void UpdateStars(Vector2 size)
    {
        StarRotation += StarRotationIncrement;
        StarRotation %= MathHelper.TwoPi;

            // Regenerate stars if applicable.
        if (GeneratedStars)
            return;

        GeneratedStars = true;

        Vector2 center = new(size.X * .5f, size.Y);

        float radius = center.Length() * Main.UIScale;

        for (int i = 0; i < StarCount; i++)
            Stars[i] = new(Main.rand, radius);
    }

    #endregion

    #region Bird

    private static void UpdateBird(UIElement element)
    {
        BirdState = BirdState switch
        {
            BirdState.Idle => UpdateIdle(element),
            BirdState.Flying => UpdatingFlying(),
            _ => BirdState.None
        };
    }

    private static BirdState UpdateIdle(UIElement element)
    {
            // Update the base position.
        Vector2 position = element.Dimensions.Position() * Main.UIScale;
        Vector2 size = element.Dimensions.Size() * Main.UIScale;

        Vector2 branchPosition =
            position +
            BranchPosition +
            (Vector2.UnitY * size.Y * .5f);

        float branchRotation = MathF.Sin(Main.GlobalTimeWrappedHourly * BranchRotationFrequency) * BranchRotationAmplitude;

        BirdPosition = BirdBranchOffset - BranchOrigin;
        BirdPosition = BirdPosition.RotatedBy(branchRotation);

        BirdPosition += branchPosition;

            // Only allow transitioning to flying if fully on screen.
        if (!BirdOnScreen(element) ||
            !element.IsMouseHovering)
            return BirdState.Idle;

        BirdFrame = 0;

        BirdVelocity = BirdDirection;

        return BirdState.Flying;
    }

    private static bool BirdOnScreen(UIElement element)
    {
        Texture2D texture = PanelStyleTextures.Bird;

        Rectangle rectangle = texture.Frame(1, BirdFrames, 0, 0);

        Vector2 position = BirdPosition - BirdOrigin;
        rectangle.X += (int)position.X;
        rectangle.Y += (int)position.Y;

        UIElement? innerList = element.Parent?.Parent;

        if (innerList is null)
            return false;

        Rectangle parentRectangle = innerList.DimensionsFromParent.Multiply(Main.UIScale);

        return parentRectangle.Contains(rectangle);
    }

    private static BirdState UpdatingFlying()
    {
        if (++BirdFrameTimer >= BirdFrameTime)
        {
            BirdFrameTimer = 0;

            if (++BirdFrame >= BirdFlyingFrames)
                BirdFrame = 0;
        }

            // Make the bird only move so fast
        if (BirdVelocity.LengthSquared() <= BirdMaxVelocitySqr)
            BirdVelocity *= BirdVelocityMultiplier;

        BirdPosition += BirdVelocity;

        if (BirdPosition.Y <= 0)
            BirdState = BirdState.None;

        return BirdState.Flying;
    }

    #endregion

    #endregion

    #region Interactions

    private static void OnHover(UIMouseEvent evt, UIElement element)
    {
        Vector2 size = element.Dimensions.Size();

        SpawnLeavesHover(size);
    }

    #region Particles

    private static void SpawnLeavesHover(Vector2 size)
    {
        if (LeafHoverTimer > 0)
            return;

        LeafHoverTimer = LeafHoverTime;

        int count = Main.rand.Next(LeafHoverCountMin, LeafHoverCountMax);

        for (int i = 0; i < count; i++)
            SpawnLeaf(size, LeafHoverVelocity);
    }

    #endregion

    #endregion

    #region Drawing

    #region Panel

    public override bool PreDrawPanel(UIModItem element, SpriteBatch spriteBatch, ref bool drawDivider)
    {
        if (element._needsTextureLoading)
        {
            element._needsTextureLoading = false;
            element.LoadTextures();
        }

        if (!UIEffects.Panel.IsReady)
            return true;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        Rectangle dims = element.Dimensions;

            // Make sure the panel draws correctly on any scale.
        Vector2 size = Vector2.Transform(dims.Size(), Main.UIScaleMatrix);
        Vector2 position = Vector2.Transform(dims.Position(), Main.UIScaleMatrix);

        Rectangle source = new((int)position.X, (int)position.Y, 
            (int)size.X, (int)size.Y);

        spriteBatch.End(out var snapshot);

            // Panel background (sky, branch.)
        using (new RenderTargetSwap(ref PanelTarget, (int)size.X, (int)size.Y))
        {
            device.Clear(Color.Transparent);

            DrawPanelBackground(spriteBatch, size);
        }

        DrawAsPanel(spriteBatch, snapshot, device, PanelTarget, source, element);

            // That fucking bird that I hate.
        DrawBird(spriteBatch, snapshot, device, element);

            // Panel foreground (particles, glow.)
        using (new RenderTargetSwap(ref PanelTarget, (int)size.X, (int)size.Y))
        {
            device.Clear(Color.Transparent);

            DrawPanelForeground(spriteBatch, device, size);
        }

            // Use transparent as the panel color to only mask PanelTarget.
        DrawAsPanel(spriteBatch, snapshot, device, PanelTarget, source, element, Color.Transparent);

            // Return to the base spriteBatch context.
        spriteBatch.Begin(in snapshot);

            // Additional border that stands out more.
        Color borderColor =
            element.IsMouseHovering ?
            PanelHoverOutlineColor :
            PanelOutlineColor;

        element.DrawPanel(spriteBatch, element._borderTexture.Value, borderColor);

            // Faded divider.
        drawDivider = false;

        Rectangle innerDimensions = element.InnerDimensions;

        Rectangle dividerSize = new(
            innerDimensions.X + 5 + element._modIconAdjust, innerDimensions.Y + 30,
            innerDimensions.Width - 10 - element._modIconAdjust, 4);

        spriteBatch.Draw(PanelStyleTextures.Divider, dividerSize, Color.White);

        return false;
    }

    private static void DrawAsPanel(SpriteBatch spriteBatch, SpriteBatchSnapshot snapshot, GraphicsDevice device, Texture2D texture, Rectangle frame, UIPanel element, Color? color = null)
    {
        spriteBatch.Begin(snapshot with { SortMode = SpriteSortMode.Immediate });

        UIEffects.Panel.Source = new(frame.Width, frame.Height, frame.X, frame.Y);

        UIEffects.Panel.Apply();

        device.Textures[1] = texture;
        device.SamplerStates[1] = SamplerState.PointClamp;

        element.DrawPanel(spriteBatch, element._backgroundTexture.Value, color ?? element.BackgroundColor);
        element.DrawPanel(spriteBatch, element._borderTexture.Value, color ?? element.BorderColor);

        spriteBatch.End();
    }

    #endregion

    #region Background

    private static void DrawPanelBackground(SpriteBatch spriteBatch, Vector2 size)
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

        spriteBatch.End();
    }

    #endregion

    #region Foreground

    private static void DrawPanelForeground(SpriteBatch spriteBatch, GraphicsDevice device, Vector2 size)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            // Draw the falling leaves.
        Leaves.Draw(spriteBatch, device);

            // And faint wind particles.
        spriteBatch.End(out var snapshot);

        device.Textures[0] = SkyTextures.SunBloom;
        device.SamplerStates[0] = SamplerState.LinearClamp;

            // TODO: Not this.
        Color oldSkyColor = Main.ColorOfTheSkies;
        Main.ColorOfTheSkies = Color.White;

        Winds.Draw(spriteBatch, device);

        Main.ColorOfTheSkies = oldSkyColor;

        spriteBatch.Begin(snapshot with { SamplerState = SamplerState.LinearClamp });

        Rectangle background = new(0, 0, (int)size.X, (int)size.Y);

            // Vauge foreground light.
        spriteBatch.Draw(SkyTextures.SkyGradient, background, ForegroundGradientColor);

        spriteBatch.End();
    }

    #endregion

    #region Bird

    private static void DrawBird(SpriteBatch spriteBatch, SpriteBatchSnapshot snapshot, GraphicsDevice device, UIPanel panel)
    {
        if (BirdState == BirdState.None)
            return;

        Rectangle scissor = device.ScissorRectangle;

        if (BirdState == BirdState.Flying)
            device.ScissorRectangle = device.Viewport.Bounds;

        spriteBatch.Begin(snapshot with
        {
            BlendState = BlendState.AlphaBlend,
            SamplerState = SamplerState.PointClamp,
            TransformMatrix = Matrix.Identity
        });

        Texture2D texture = PanelStyleTextures.Bird;

        Rectangle frame = texture.Frame(1, BirdFrames, 0, BirdFrame);

        spriteBatch.Draw(texture, BirdPosition, frame, Color.White, 0f, BirdOrigin, 1f, SpriteEffects.None, 0f);

        spriteBatch.End();

        device.ScissorRectangle = scissor;
    }

    #endregion

    #endregion

    #region Private Methods

    private static void SpawnLeaf(Vector2 size, Vector2? velocity = null)
    {
        Vector2 position =
            new(Main.rand.NextFloat(LeafSpawnOffsetXMin, LeafSpawnOffsetXMax),
            Main.rand.NextFloat(-size.Y * .3f, size.Y));

        Leaves.Spawn(new(position, velocity));
    }

    private static Matrix RotationMatrix(Vector2 size)
    {
        Matrix rotation = Matrix.CreateRotationZ(StarRotation);
        Matrix offset = Matrix.CreateTranslation(new(size.X * .5f, size.Y, 0f));

        return Matrix.Identity * rotation * offset;
    }

    #endregion
}
