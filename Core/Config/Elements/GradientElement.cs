using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.Config.UI;
using ZensSky.Core.DataStructures;
using ZensSky.Core.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Config.Elements;

public class GradientElement : DropDownConfigElement<Gradient>
{
    #region Private Fields

    private const string SliderHoverKey = "Mods.ZensSky.Configs.GradientSliderHover";

    #endregion

    #region Public Fields

    public GradientSlider? Slider;

    public ColorPicker? Picker;

    #endregion

    #region Public Properties

    public override float ExpandedHeight => BaseHeight + 36 + 300f + 52f + 20;

    #endregion

    #region Drop Down

    protected override void OnExpand()
    {
        Slider = new(Value);

        Slider.Top.Set(BaseHeight + 5, 0f);

        Slider.HAlign = .5f;

        Slider.Width.Set(-20f, 1f);

            // For whatever reason most -- maybe all(?) -- ConfigElements don't make any sound for hovering, nor clicking.
        Slider.Mute = true;

        Slider.OnSegmentSelected +=
            (s) => Picker?.Color = s.TargetSegment.Color;

        Append(Slider);

        Picker = new();

        Picker.Top.Set(BaseHeight + 36, 0f);

        Picker.Left.Set(10, 0f);

        Picker.Width.Set(1f, 0f);
        Picker.MinWidth.Set(300f, 0f);

        Picker.Mute = true;

        Append(Picker);

        Picker.Color = Slider.TargetSegment.Color;

        UIPanel test = new();

        test.Top.Set(BaseHeight + 36, 0f);

        test.Left.Set(315f, 0f);

        test.Width.Set(-325f, 1f);

        test.Height.Set(-BaseHeight - 46, 1f);

        Append(test);
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Slider is null ||
            Picker is null)
            return;

        Slider.TargetSegment.Color = Picker.Color;
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (!MenuOpen)
            DrawDisplaySlider(spriteBatch);

        if (Slider is null ||
            Picker is null)
            return;

        if (Slider.IsHeld)
        {
            string tooltip = Utilities.GetReadableTime(Slider.TargetSegment.Position * 24f);

            UIModConfig.Tooltip = tooltip;
        }
        else if (Slider.IsMouseHovering)
        {
            string tooltip = Utilities.GetTextValueWithGlyphs(SliderHoverKey);

            UIModConfig.Tooltip = tooltip;
        }
    }

    protected void DrawDisplaySlider(SpriteBatch spriteBatch)
    {
        Rectangle dims = this.Dimensions;

        IngameOptions.valuePosition = new(dims.X + dims.Width - 10f, dims.Y + 16f);

        Texture2D colorBar = TextureAssets.ColorBar.Value;

        IngameOptions.valuePosition.X -= colorBar.Width;

        Utilities.DrawVanillaSlider(spriteBatch, Color.White, false, out _, out _, out Rectangle inner);

        for (int i = 0; i < inner.Width; i++)
        {
            Rectangle segement = new(inner.X + i, inner.Y, 1, inner.Height);

            Color color = Value.GetColor(i / (float)inner.Width);

            spriteBatch.Draw(MiscTextures.Pixel, segement, color);
        }
    }

    #endregion
}
