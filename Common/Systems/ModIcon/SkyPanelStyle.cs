using Daybreak.Common.Features.ModPanel;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using ZensSky.Common.Registries;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.Systems.ModIcon;

public sealed class SkyPanelStyle : ModPanelStyleExt
{
    #region Private Fields

    private static readonly Color OutlineColor = new(21, 30, 39);

    private static readonly Color DisabledColor = new(133, 103, 74);
    private static readonly Color EnabledColor = new(199, 174, 144);

    private static SkyPanelTargetContent? PanelRenderTarget;

    #endregion

    #region Loading

    public override void Load()
    {
        PanelRenderTarget = new();
        Main.ContentThatNeedsRenderTargets.Add(PanelRenderTarget);
    }

    public override void Unload()
    {
        if (PanelRenderTarget is not null)
        {
            Main.ContentThatNeedsRenderTargets.Remove(PanelRenderTarget);
            PanelRenderTarget = null;
        }
    }

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

    public override bool PreDrawPanel(UIModItem element, SpriteBatch spriteBatch)
    {
        if (element._needsTextureLoading)
        {
            element._needsTextureLoading = false;
            element.LoadTextures();
        }

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, snapshot.RasterizerState, null, Main.UIScaleMatrix);

        DrawPanel(element, spriteBatch);

        spriteBatch.Restart(in snapshot);

        element.DrawPanel(spriteBatch, element._borderTexture.Value, element.BorderColor);

        return false;
    }

    private static void DrawPanel(UIModItem element, SpriteBatch spriteBatch)
    {
        Effect panel = Shaders.Panel.Value;

        if (panel is null || PanelRenderTarget is null)
            return;

        CalculatedStyle dims = element.GetDimensions();

        Vector2 size = Vector2.Transform(new(dims.Width, dims.Height), Main.UIScaleMatrix);
        Vector2 position = Vector2.Transform(new(dims.X, dims.Y), Main.UIScaleMatrix);

        CalculatedStyle innerDims = element.GetInnerDimensions();

        PanelRenderTarget.Size = new(dims.Width, dims.Height);
        PanelRenderTarget.InnerDimensions = new((int)innerDims.X - (int)dims.X, (int)innerDims.Y - (int)dims.Y,
            (int)innerDims.Width, (int)innerDims.Height);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        panel.Parameters["Source"]?.SetValue(new Vector4(size.X, size.Y, 
            position.X, position.Y));

        panel.CurrentTechnique.Passes[0].Apply();

        PanelRenderTarget.Request();
        device.Textures[1] = PanelRenderTarget.IsReady ? PanelRenderTarget.GetTarget() : TextureAssets.MagicPixel.Value;

        device.Textures[2] = Textures.PanelGradient.Value;
        device.SamplerStates[2] = SamplerState.PointClamp;

        element.DrawPanel(spriteBatch, element._backgroundTexture.Value, element.BackgroundColor);
    }
}
