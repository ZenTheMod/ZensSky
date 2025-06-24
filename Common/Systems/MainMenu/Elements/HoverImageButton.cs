using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class HoverImageButton : UIElement
{
    #region Public Fields

    public Asset<Texture2D> InnerTexture;
    public Asset<Texture2D> OuterTexture;

    public Color InnerColor;
    public Color OuterColor;
    public Color OuterHoverColor;

    public string HoverText;

    #endregion

    #region Contructor

    public HoverImageButton(Asset<Texture2D> innerTexture, Color innerColor, Asset<Texture2D>? outerTexture = null, Color? outerColor = null, Color? outerHoverColor = null, string hoverText = "")
    {
        InnerTexture = innerTexture;
        InnerColor = innerColor;

        OuterTexture = outerTexture ?? Textures.Invis;
        OuterColor = outerColor ?? Color.White;
        OuterHoverColor = outerHoverColor ?? Main.OurFavoriteColor;

        HoverText = hoverText;
    }

    #endregion

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dims = GetDimensions();

        spriteBatch.Draw(InnerTexture.Value, dims.ToRectangle(), InnerColor);
        spriteBatch.Draw(OuterTexture.Value, dims.ToRectangle(), IsMouseHovering ? OuterHoverColor : OuterColor);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        IsMouseHovering = !Main.alreadyGrabbingSunOrMoon;

        if (!IsMouseHovering)
            return;

        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsMouseHovering || HoverText == string.Empty)
            return;

        string tooltip = Language.GetTextValue(HoverText);
        Main.instance.MouseText(tooltip);
    }
}
