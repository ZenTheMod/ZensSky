using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.UI;

public sealed class ColorSquare : UIElement
{
    #region Private Fields

    private static readonly Color Outline = new(215, 215, 215);

    #endregion

    #region Public Fields

    public float Hue;

    public Vector2 PickerPosition;

    public bool IsHeld;

    public bool Mute;

    #endregion

    #region Public Properties

    public Color Color
    {
        get => Utilities.HSVToColor(new(Hue, PickerPosition.X, 1 - PickerPosition.Y));
        set 
        {
            Vector3 hsl = Utilities.ColorToHSV(value);

            Hue = hsl.X;

            PickerPosition = new(hsl.Y, 1 - hsl.Z);
        }
    }

    #endregion

    #region Public Constructors

        // Make the square by fill its container by default.
    public ColorSquare() =>
        Width.Set(0, 1f);

    #endregion

    #region Updating

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (Main.alreadyGrabbingSunOrMoon)
            return;

        if (evt.Target == this)
            IsHeld = true;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        IsHeld = false;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        IsMouseHovering = !Main.alreadyGrabbingSunOrMoon;

        if (!IsMouseHovering || IsHeld)
            return;

        if (!Mute)
            SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsHeld)
            return;

        Rectangle dims = this.Dimensions;

        dims.Inflate(-4, -4);

        Vector2 position = Vector2.Clamp(Utilities.UIMousePosition, dims.Position(), dims.Position() + dims.Size());

        PickerPosition = (position - dims.Position()) / dims.Size();
    }

    public override void Recalculate()
    {
        base.Recalculate();

            // Force the square to be... well, a square.
        float width = GetDimensions().Width;

        Height.Set(width, 0);
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle dims = this.Dimensions;

        spriteBatch.Draw(MiscTextures.Pixel, dims, Color.Black);

        dims.Inflate(-2, -2);

        bool hovering =
            ContainsPoint(Utilities.UIMousePosition) &&
            !Main.alreadyGrabbingSunOrMoon &&
            Parent.IsMouseHovering;

        Color outline = hovering || IsHeld ?
            Main.OurFavoriteColor : Outline;

        spriteBatch.Draw(MiscTextures.Pixel, dims, outline);

        dims.Inflate(-2, -2);

        spriteBatch.Draw(ButtonTextures.ColorSelector[0], dims, Color.White);
        spriteBatch.Draw(ButtonTextures.ColorSelector[1], dims, Utilities.HSVToColor(Hue));

        Texture2D picker = UITextures.Dot;

        Vector2 pickerOrigin = picker.Size() * .5f;

        Vector2 position = PickerPosition * dims.Size();
        position += dims.Position();

            // Round the position to have it be unable to lie inbetween pixels.
        position = Terraria.Utils.Round(position);

        spriteBatch.Draw(picker, position, null, Color.White, 0f, pickerOrigin, 1f, SpriteEffects.None, 0f);
    }

    #endregion
}
