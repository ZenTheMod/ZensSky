using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.UI;

public sealed class ColorPicker : UIElement
{
    #region Private Fields

    private readonly ColorSquare Picker;

    private readonly UISlider HueSlider;

    private static readonly char[] AllowedHexChars = [.. '0'.Range('9'), .. 'A'.Range('F'), .. 'a'.Range('f')];

    private readonly InputField HexInput;

    private static readonly char[] AllowedRGBChars = [.. '0'.Range('9')];

    private readonly InputField RInput;
    private readonly InputField GInput;
    private readonly InputField BInput;

    #endregion

    #region Public Fields

    public bool Mute;

    #endregion

    #region Public Events

        // TODO: Generic impl of UIElementAction.
    public event Action<ColorPicker>? OnAcceptInput;

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

        HueSlider.Top.Set(-40f, 1f);

        HueSlider.InnerTexture = MiscTextures.HueGradient;
        HueSlider.InnerColor = Color.White;

        Append(HueSlider);

        Picker = new();

        Append(Picker);

        #region Hex

        UIText hashtag = new("#");

        hashtag.Top.Set(-12f, 1f);
        hashtag.Left.Set(4f, 0f);

        Append(hashtag);

        HexInput = new(string.Empty, 6);

        HexInput.Width.Set(76f, 0f);
        HexInput.Top.Set(-16f, 1f);

        HexInput.Left.Set(16f, 0f);

        HexInput.WhitelistedChars = AllowedHexChars;

        HexInput.OnEnter += AcceptHex;

        Append(HexInput);

        #endregion

        #region RGB

        UIText b = new("B");

        b.Top.Set(-12f, 1f);
        b.Left.Set(-62f, 0f);

            // Append(b);

        BInput = new(string.Empty, 3);

        BInput.Width.Set(50f, 0f);
        BInput.Top.Set(-16f, 1f);

        BInput.Left.Set(0f, 1f);

        BInput.WhitelistedChars = AllowedRGBChars;

            // BInput.OnEnter += AcceptHex;

            // Append(BInput);

        #endregion
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Picker.Hue = HueSlider.Ratio;

        Picker.Mute = Mute;
        HueSlider.Mute = Mute;

        HexInput.Hint = Terraria.Utils.Hex3(Color);
    }

    public override void Recalculate()
    {
        base.Recalculate();

        float width = GetDimensions().Width;

        Height.Set(width + 52f, 0f);
    }

    #endregion

    #region Inputs

    private void AcceptHex(InputField field)
    {
        Color = Utilities.FromHex3(field.Text);

        field.Text = string.Empty;

        OnAcceptInput?.Invoke(this);
    }

    #endregion
}
