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

    private static readonly char[] AllowedHexChars =
        [.. '0'.Range('9'), .. 'A'.Range('F'), .. 'a'.Range('f')];

    private readonly InputField HexInput;

    private static readonly char[] AllowedRGBChars =
        [.. '0'.Range('9')];

    private static readonly string[] RGBLabels =
        ["R", "G", "B"];

    private const int RGBInputWidth = 70;

    private readonly InputField[] RGBInputs;

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

        RGBInputs = new InputField[3];

        for (int i = 0; i < RGBInputs.Length; i++)
            CreateRGBInput(i);

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

        RGBInputs[0]?.Hint = Color.R.ToString();
        RGBInputs[1]?.Hint = Color.G.ToString();
        RGBInputs[2]?.Hint = Color.B.ToString();
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
        Color newColor = Utilities.FromHex3(field.Text);

        if (newColor != Color.Transparent)
            Color = newColor;

        field.Text = string.Empty;

        OnAcceptInput?.Invoke(this);
    }

    private void CreateRGBInput(int i)
    {
        UIText label = new(RGBLabels[i]);

        label.Top.Set(-12f, 1f);
        label.Left.Set(4 - (RGBInputWidth * (3 - i)), 1f);

        Append(label);

        RGBInputs[i] = new(string.Empty, 3);

        RGBInputs[i].Width.Set(50f, 0f);
        RGBInputs[i].Top.Set(-16f, 1f);

        RGBInputs[i].Left.Set(-50f - (RGBInputWidth * (2 - i)), 1f);

        RGBInputs[i].WhitelistedChars = AllowedRGBChars;

        RGBInputs[i].OnEnter +=
            (f) => AcceptRGB(f, i);

        Append(RGBInputs[i]);
    }

    private void AcceptRGB(InputField field, int component)
    {
        if (int.TryParse(field.Text, out int value))
        {
            Color = component switch
            {
                0 => new(value, Color.G, Color.B),
                1 => new(Color.R, value, Color.B),
                2 => new(Color.R, Color.G, value),
                _ => Color
            };
        }

        field.Text = string.Empty;

        OnAcceptInput?.Invoke(this);
    }

    #endregion
}
