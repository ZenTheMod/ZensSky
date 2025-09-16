using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;
using ZensSky.Common.Config;
using ZensSky.Core.DataStructures;
using ZensSky.Core.Utils;
using static Terraria.ModLoader.Config.UI.RangeElement;

namespace ZensSky.Core.Config.Elements;

public class GradientElement : ConfigElement<Gradient>
{
    #region Private Fields

    private const float SliderWidth = 167f;

        // Dummy element to set when hovering to prevent issues with normal RangeElements.
    private readonly RangeElement DummyRangeElement =
        new ByteElement();

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

        if (SkyConfig.Instance.SkyGradient is null)
            return;

        if (Value is null)
            return;

        rightHover = null;

        if (!Main.mouseLeft)
            rightLock = null;

        CalculatedStyle dimensions = GetDimensions();

            // Not sure the purpose of this.
        IngameOptions.valuePosition = new(dimensions.X + dimensions.Width - 10f, dimensions.Y + 16f);

        DrawSlider(spriteBatch, out float ratio);

        if (IngameOptions.inBar || rightLock == DummyRangeElement)
        {
            rightHover = DummyRangeElement;

            if (PlayerInput.Triggers.JustPressed.MouseLeft &&
                rightLock == DummyRangeElement)
            {
                    // Only drag segements if they are close enough to the cursor
                TargetSegment = Value.CompareFor((segment) => MathF.Abs(ratio - segment.Position), false);

                if (MathF.Abs(ratio - TargetSegment.Position) >= 3f)
                    TargetSegment = null;
            }

                // If not holding left click we are not dragging a segment.
            if (!PlayerInput.Triggers.Current.MouseLeft)
                TargetSegment = null;

            if (TargetSegment is not null)
            {
                TargetSegment.Position =
                    Utilities.Saturate(ratio);

                Interface.modConfig.SetPendingChanges();
            }
        }

        if (rightHover is not null &&
            rightLock is null &&
            PlayerInput.Triggers.JustPressed.MouseLeft)
            rightLock = rightHover;
    }

    public void DrawSlider(SpriteBatch spriteBatch, out float ratio)
    {
        Texture2D colorBar = TextureAssets.ColorBar.Value;
        Texture2D colorSlider = TextureAssets.ColorSlider.Value;

        IngameOptions.valuePosition.X -= colorBar.Width;

        Rectangle rectangle = new(
            (int)IngameOptions.valuePosition.X,
            (int)IngameOptions.valuePosition.Y - (int)(colorBar.Height * .5f),
            colorBar.Width,
            colorBar.Height);

        bool isHovering = rectangle.Contains(Utilities.MousePosition) || rightLock == DummyRangeElement;

        if (rightLock != DummyRangeElement && rightLock is not null)
            isHovering = false;

        Utilities.DrawVanillaSlider(spriteBatch, Color.White, isHovering, out ratio, out Rectangle destinationRectangle, out Rectangle inner);

        IngameOptions.inBar = isHovering;

        for (int i = 0; i < Value.Count; i++)
        {
            GradientSegment segment = Value[i];

            Vector2 position = new(destinationRectangle.X + (SliderWidth * segment.Position), destinationRectangle.Y + 4f);

            Color color = TargetSegment == segment ? Main.OurFavoriteColor : Color.White;

            spriteBatch.Draw(colorSlider, position, null, color, 0f, colorSlider.Size() * .5f, 1f, SpriteEffects.None, 0f);
        }
    }

    #endregion
}
