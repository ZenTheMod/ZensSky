using Microsoft.Xna.Framework;
using Terraria.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.UI;

public sealed class ColorPicker : UIElement
{
    #region Private Fields

    private readonly ColorSquare Picker;

    private readonly UISlider HueSlider;

    #endregion

    #region Public Properties

    public Color Color
    {
        get => Picker.Color; 
        set 
        { 
            Picker.Color = value;
            HueSlider.Ratio = Utilities.ColorToHSV(value).X;
        }
    }

    public bool IsHeld => Picker.IsHeld || HueSlider.IsHeld;

    #endregion

    #region Constructor

    public ColorPicker() : base()
    {
        Width.Set(0f, 1f);

        HueSlider = new();

        HueSlider.Top.Set(-16f, 1f);

        HueSlider.InnerTexture = MiscTextures.HueGradient;
        HueSlider.InnerColor = Color.White;

        Append(HueSlider);

        Picker = new();

        Append(Picker);
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Picker.Hue = HueSlider.Ratio;
    }

    public override void Recalculate()
    {
        base.Recalculate();

        float width = GetDimensions().Width;

        Height.Set(width + 28f, 0f);
    }

    #endregion
}
