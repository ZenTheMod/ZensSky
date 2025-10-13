using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;
using ZensSky.Core.Utils;

namespace ZensSky.Core.UI;

public class InputField : UIPanel
{
    #region Private Fields

    private int MousePosition;

    #endregion

    #region Public Fields

    public string OldText = string.Empty;

    public string Text;

    public string Hint;

    public int MaxChars;

    public char[] BlacklistedChars = [];

    public char[] WhitelistedChars = [];

    public bool Centered;

    #endregion

    #region Public Events

        // TODO: Generic impl of UIElementAction.
    public event Action<InputField>? OnEnter;

    public event Action<InputField>? OnEscape;

    #endregion

    #region Public Properties

    public bool IsWriting
    {
        get => field;

        set
        {
            Input.WritingText = value;

            field = value;
        }
    }

    #endregion

    #region Public Constructors

    public InputField(string hint, int maxChars)
    {
        Text = string.Empty;
        Hint = hint;
        MaxChars = maxChars;

        _backgroundTexture = UITextures.TextPanel;
        _borderTexture = MiscTextures.Invis;

        Height.Set(26f, 0f);
        Width.Set(0f, 1f);
    }

    #endregion

    #region Interactions

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (evt.Target != this ||
            Main.alreadyGrabbingSunOrMoon)
        {
            IsWriting = false;
            return;
        }

        IsWriting = true;
        OldText = Text;

        Input.CursorPositon = 0;

        if (Text.Length <= 0)
            return;

        Input.CursorPositon = MousePosition;
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

            // Don't freeze the game dumbass.
        if (Text.Length > MaxChars)
            Text = Text[.. MaxChars];

        bool clickedOff =
            !Main.hasFocus ||
            (Main.mouseLeft &&
            !IsMouseHovering);

        if (clickedOff)
        {
            if (IsWriting)
                OnEnter?.Invoke(this);
            IsWriting = false;
        }
    }

    #endregion

    #region Input

    private void HandleInput()
    {
        Input.WritingText = IsWriting;

        switch (Input.GetInput(Text, out string newText, false, BlacklistedChars, WhitelistedChars))
        {
            case InputCancellationType.Confirmed:
                Text = newText;
                OnEnter?.Invoke(this);
                IsWriting = false;
                break;

            case InputCancellationType.Escaped:
                Text = OldText;
                OnEscape?.Invoke(this);
                IsWriting = false;
                break;

            default:
                Text = newText;
                break;
        }
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        DynamicSpriteFont font = FontAssets.MouseText.Value;

        Rectangle dims = this.Dimensions;

        Vector2 position = new(Centered ? dims.Y + (dims.Height * .5f) : (dims.X + 6), dims.Y + (dims.Height * .5f) + 4);

        Vector2 textSize = font.MeasureString(Text == string.Empty ? Hint : Text);
        Vector2 origin = new(Centered ? textSize.X * .5f : 0, textSize.Y * .5f);

        bool drawBlinker = IsWriting && Main.GlobalTimeWrappedHourly % .666f > .333f;

        if (Text == string.Empty)
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, Hint, position, Color.Gray, 0f, origin, Vector2.One);

        spriteBatch.SlowDrawStringWithShadow(font, Text, position, Color.White, origin, Vector2.One, out MousePosition, drawBlinker, Input.CursorPositon);

        if (!IsWriting)
            return;

        HandleInput();
    }

    #endregion
}
