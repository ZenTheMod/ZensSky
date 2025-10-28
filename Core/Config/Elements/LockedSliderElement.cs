using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Config.Elements;

[HideRangeSlider]
public abstract class LockedSliderElement<T> : PrimitiveRangeElement<T>, ILockedConfigElement where T : IComparable<T>
{
    #region Private Fields

    private const float SliderWidth = 167f;

    private const float LockedBackgroundMultiplier = .4f;

    private static readonly Color LockedGradient = new(40, 40, 40);

    #endregion

    #region Private Properties

    object? ILockedConfigElement.TargetInstance { get; set; }

    PropertyFieldWrapper? ILockedConfigElement.TargetMember { get; set; }

    #endregion

    #region Public Properties

    public bool IsLocked =>
        this.As<ILockedConfigElement>().IsLocked;

    #endregion

    #region Initialization

    public override void OnBind()
    {
        base.OnBind();

        this.As<ILockedConfigElement>().InitializeLockedElement(this);
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        backgroundColor = IsLocked ? UICommon.DefaultUIBlue * LockedBackgroundMultiplier : UICommon.DefaultUIBlue;
        base.DrawSelf(spriteBatch);

        rightHover = null;

        if (!Main.mouseLeft)
            rightLock = null;

        Rectangle dims = this.Dimensions;

            // Not sure the purpose of this.
        IngameOptions.valuePosition = new(dims.X + dims.Width - 10f, dims.Y + 16f);

        DrawSlider(spriteBatch, Proportion, out float ratio);

            // No need to run logic if the value doesn't do anything.
        if (IsLocked)
            return;

        if (IngameOptions.inBar || rightLock == this)
        {
            rightHover = this;
            if (PlayerInput.Triggers.Current.MouseLeft && rightLock == this)
                Proportion = ratio;
        }

        if (rightHover is not null &&
            rightLock is null &&
            PlayerInput.Triggers.JustPressed.MouseLeft)
            rightLock = rightHover;
    }

    public void DrawSlider(SpriteBatch spriteBatch, float perc, out float ratio)
    {
        perc = MathHelper.Clamp(perc, -.05f, 1.05f);

        Texture2D colorBar = TextureAssets.ColorBar.Value;
        Texture2D gradient = MiscTextures.Gradient;
        Texture2D colorSlider = TextureAssets.ColorSlider.Value;
        Texture2D lockIcon = UITextures.Lock;

        IngameOptions.valuePosition.X -= colorBar.Width;

        Rectangle rectangle = new(
            (int)IngameOptions.valuePosition.X,
            (int)IngameOptions.valuePosition.Y - (int)(colorBar.Height * .5f),
            colorBar.Width,
            colorBar.Height);

        bool isHovering = rectangle.Contains(Utilities.MousePosition) || rightLock == this;

        if (rightLock != this && rightLock is not null || IsLocked)
            isHovering = false;

        Color color = IsLocked ? Color.Gray : Color.White;

        Utilities.DrawVanillaSlider(spriteBatch, color, isHovering, out ratio, out Rectangle destinationRectangle, out Rectangle inner);

        spriteBatch.Draw(gradient, inner, null, IsLocked ? LockedGradient : SliderColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);

        Vector2 lockOffset = new(0, -4);

        if (IsLocked)
            spriteBatch.Draw(lockIcon, inner.Center() + lockOffset, null, Color.White, 0f, lockIcon.Size() * .5f, 1f, SpriteEffects.None, 0f);
        else
            spriteBatch.Draw(colorSlider, new(destinationRectangle.X + 5f + (SliderWidth * perc), destinationRectangle.Y + 8f), null, Color.White, 0f, colorSlider.Size() * .5f, 1f, SpriteEffects.None, 0f);

        IngameOptions.inBar = isHovering;
    }

    #endregion
}
