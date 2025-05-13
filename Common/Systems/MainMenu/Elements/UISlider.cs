using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.UI;
using Terraria.ID;
using ZensSky.Common.Registries;
using Terraria;
using Terraria.GameContent;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class UISlider : UIElement
{
    public UISlider() 
    {
        Width.Set(0, 1f);
        Height.Set(16, 0f);
    }

    public bool IsHeld;

    public float Ratio;

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
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
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();

        if (IsHeld)
        {
            float num = UserInterface.ActiveInstance.MousePosition.X - dims.X;
            Ratio = MathHelper.Clamp(num / dims.Width, 0f, 1);
        }

        Texture2D slider = Textures.Slider.Value;
        Texture2D sliderOutline = Textures.SliderHighlight.Value;

        Rectangle size = dims.ToRectangle();

        DrawBar(spriteBatch, slider, size, Color.White);
        if (IsHeld || IsMouseHovering)
            DrawBar(spriteBatch, sliderOutline, size, Main.OurFavoriteColor);

        size.Inflate(-4, -4);
        spriteBatch.Draw(Textures.Pixel.Value, size, Color.Gray);

        Texture2D blip = TextureAssets.ColorSlider.Value;

        Vector2 blipOrigin = blip.Size() * 0.5f;
        Vector2 blipPosition = new(size.X + (Ratio * size.Width), size.Center.Y);

        spriteBatch.Draw(blip, blipPosition, null, Color.White, 0f, blipOrigin, 1f, 0, 0f);
    }

    public static void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color color)
    {
        spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y, 6, dimensions.Height), new Rectangle(0, 0, 6, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + 6, dimensions.Y, dimensions.Width - 12, dimensions.Height), new Rectangle(6, 0, 2, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + dimensions.Width - 6, dimensions.Y, 6, dimensions.Height), new Rectangle(8, 0, 6, texture.Height), color);
    }
}
