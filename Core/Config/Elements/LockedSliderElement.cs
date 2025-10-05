using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;

namespace ZensSky.Core.Config.Elements;

[Autoload(Side = ModSide.Client)]
[HideRangeSlider]
public abstract class LockedSliderElement<T> : PrimitiveRangeElement<T> where T : IComparable<T>
{
    #region Private Fields

    private const string LockTooltipKey = "LockReason";

    private const float SliderWidth = 167f;

    private const float LockedBackgroundMultiplier = .4f;

    private static readonly Color LockedGradient = new(40, 40, 40);

    #endregion

    #region Properties

    public object? TargetInstance { get; private set; }

    public PropertyFieldWrapper? TargetMember { get; private set; }

    public bool Mode { get; private set; } = false;

    public bool IsLocked =>
        (bool)(TargetMember?.GetValue(TargetInstance) ?? false) == Mode;

    #endregion

    #region Initialization

    public override void OnBind()
    {
        base.OnBind();

        LockedElementAttribute? attri = ConfigManager.GetCustomAttributeFromMemberThenMemberType<LockedElementAttribute>(MemberInfo, Item, List);

        Type? type = attri?.TargetConfig;

        string? name = attri?.MemberName;

        bool? mode = attri?.Mode;

        if (type is null || string.IsNullOrEmpty(name) || mode is null)
            return;

            // TODO: Switch to using a MemberInfo based impl.
        FieldInfo? field = type.GetField(name, Static | Instance | Public | NonPublic);
        PropertyInfo? property = type.GetProperty(name, Static | Instance | Public | NonPublic);

        if (field is not null)
            TargetMember = new(field);
        else
            TargetMember = new(property);

        if (ConfigManager.Configs.TryGetValue(ModContent.GetInstance<ZensSky>(), out List<ModConfig>? value))
            TargetInstance = value.Find(c => c.Name == type.Name);
        else
            TargetInstance = null;

        Mode = mode ?? false;

        string tooltip = ConfigManager.GetLocalizedTooltip(MemberInfo);
        string? lockReason = ConfigManager.GetLocalizedText<LockedKeyAttribute, LockedArgsAttribute>(MemberInfo, LockTooltipKey);

        TooltipFunction = () =>
            tooltip +
            (IsLocked && lockReason is not null ?
            (string.IsNullOrEmpty(tooltip) ? string.Empty : "\n") + $"[c/{Color.Red.Hex3()}:" + lockReason + "]" :
            string.Empty);
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
