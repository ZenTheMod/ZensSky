using Macrocosm.Common.Utils;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using ZensSky.Common.Config;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Config.Elements;

public class GradientElement : ConfigElement<Gradient>
{
    #region Private Fields

    private const float SliderWidth = 167f;

    #endregion

    #region Public Properties

    public GradientSegment? TargetSegment { get; private set; }

    #endregion

    #region Binding

    public override void OnBind()
    {
        base.OnBind();


    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        CalculatedStyle dimensions = GetDimensions();

            // Not sure the purpose of this.
        IngameOptions.valuePosition = new(dimensions.X + dimensions.Width - 10f, dimensions.Y + 16f);

        DrawSlider(spriteBatch);
    }

    public void DrawSlider(SpriteBatch spriteBatch)
    {
        Texture2D colorBar = TextureAssets.ColorBar.Value;
        Texture2D colorSlider = TextureAssets.ColorSlider.Value;

        IngameOptions.valuePosition.X -= colorBar.Width;

        Rectangle rectangle = new(
            (int)IngameOptions.valuePosition.X,
            (int)IngameOptions.valuePosition.Y - (int)(colorBar.Height * .5f),
            colorBar.Width,
            colorBar.Height);

        bool isHovering = rectangle.Contains(Utilities.MousePosition);

        Utilities.DrawVanillaSlider(spriteBatch, Color.White, isHovering, out _, out _, out Rectangle inner);

        IngameOptions.inBar = isHovering;

        SkyConfig.Instance.SkyGradient =
            [new(0f, Color.Green, EasingStyle.InExpo),
            new(.31f, Color.Black, EasingStyle.OutExpo),
            new(.75f, Color.White, EasingStyle.OutExpo),
            new(.88f, Color.Yellow, EasingStyle.InExpo),
            new(.9f, Color.White, EasingStyle.OutExpo),
            new(.97f, Color.Yellow, EasingStyle.InExpo),
            new(.975f, Color.Brown),
            new(1f, Color.Brown)];

        for (int i = 0; i < inner.Width; i++)
        {
            Rectangle segement = new(inner.X + i, inner.Y, 1, inner.Height);

            Color color = Value.GetColor(i / (float)inner.Width);

            spriteBatch.Draw(MiscTextures.Pixel, segement, color);
        }
    }

    #endregion
}
