using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.ModIcon;

internal sealed class WobblyModIcon : UIImage
{
    #region Private Fields

    private static readonly Color OutlineColor = new(153, 185, 255);

    private const float XFrequencyMultiplier = 1.25f;
    private const float YFrequencyMultiplier = 1.03f;
    private const float OffsetMultiplier = 4f;

    private readonly Asset<Texture2D> icon;
    private readonly Asset<Texture2D> iconOutline;

    #endregion

    public WobblyModIcon() : base(TextureAssets.MagicPixel)
    {
        icon = Textures.InnerModIcon;
        iconOutline = Textures.OuterModIcon;

        SetImage(icon);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        float time = Main.GlobalTimeWrappedHourly;

        Vector2 offset = new(MathF.Sin(time * XFrequencyMultiplier), MathF.Cos(time * YFrequencyMultiplier));
        offset *= OffsetMultiplier;

        CalculatedStyle dimensions = GetDimensions();

        spriteBatch.Draw(iconOutline.Value, dimensions.Position() + offset, OutlineColor);

        spriteBatch.Draw(icon.Value, dimensions.Position() + offset, Color.White);
    }
}
