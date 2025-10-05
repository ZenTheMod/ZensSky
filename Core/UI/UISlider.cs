using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.UI;

public class UISlider : UIElement
{
    #region Public Fields

    public Asset<Texture2D> InnerTexture;

    public Asset<Texture2D> BlipTexture;

    public Color InnerColor;

    public bool IsHeld;

    public float Ratio;

    public bool Mute;

    #endregion

    #region Public Constructors

    public UISlider()
    {
        Width.Set(0, 1f);
        Height.Set(16, 0f);

        InnerColor = Color.Gray;

        InnerTexture = MiscTextures.Gradient;
        BlipTexture = TextureAssets.ColorSlider;
    }

    #endregion

    #region Interactions

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

    #endregion

    #region Updating

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        base.Update(gameTime);

        Rectangle dims = this.Dimensions;

        if (IsHeld)
        {
            float num = Utilities.UIMousePosition.X - dims.X;
            Ratio = Utilities.Saturate(num / dims.Width);
        }
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        Rectangle dims = this.Dimensions;

        DrawBars(spriteBatch, dims);

        dims.Inflate(-4, -4);
        spriteBatch.Draw(InnerTexture.Value, dims, InnerColor);

        DrawBlip(spriteBatch, dims, Ratio, Color.White);
    }

    #region Private Methods

    protected void DrawBars(SpriteBatch spriteBatch, Rectangle dims)
    {
        Texture2D slider = UITextures.Slider;
        Texture2D sliderOutline = UITextures.SliderHighlight;

        DrawBar(spriteBatch, slider, dims, Color.White);

        if (IsHeld || IsMouseHovering)
            DrawBar(spriteBatch, sliderOutline, dims, Main.OurFavoriteColor);
    }

    protected void DrawBlip(SpriteBatch spriteBatch, Rectangle dims, float ratio, Color color)
    {
        Texture2D blip = BlipTexture.Value;

        Vector2 blipOrigin = blip.Size() * .5f;
        Vector2 blipPosition = new(dims.X + ratio * dims.Width, dims.Center.Y);

        spriteBatch.Draw(blip, blipPosition, null, color, 0f, blipOrigin, 1f, 0, 0f);
    }

    #endregion

    #region Public Methods

    public static void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dims, Color color)
    {
        spriteBatch.Draw(texture, new Rectangle(dims.X, dims.Y, 6, dims.Height), new(0, 0, 6, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dims.X + 6, dims.Y, dims.Width - 12, dims.Height), new(6, 0, 2, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dims.X + dims.Width - 6, dims.Y, 6, dims.Height), new(8, 0, 6, texture.Height), color);
    }

    #endregion

    #endregion
}
