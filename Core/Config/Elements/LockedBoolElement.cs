using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI.Chat;
using ZensSky.Core.Utils;

namespace ZensSky.Core.Config.Elements;

public sealed class LockedBoolElement : ConfigElement<bool>, ILockedConfigElement
{
    #region Private Fields

    private const string LockTooltipKey = "LockReason";

    private const float LockedBackgroundMultiplier = 0.4f;

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

        OnLeftClick += delegate
        {
            if (!IsLocked)
                Value = !Value;
        };

        this.As<ILockedConfigElement>().InitializeLockedElement(this);
    }

    #endregion

    #region Drawing

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
            // Change the background color before drawing the base ConfigElement<T>.
        backgroundColor = IsLocked ? UICommon.DefaultUIBlue * LockedBackgroundMultiplier : UICommon.DefaultUIBlue;
        base.DrawSelf(spriteBatch);

        Texture2D texture = UITextures.LockedSettingsToggle;

        Rectangle dims = this.Dimensions;

        string text = Value ? Lang.menu[126].Value : Lang.menu[124].Value; // On / Off

        if (IsLocked)
            text += " " + Language.GetTextValue("Mods.ZensSky.Configs.Locked");

        DynamicSpriteFont font = FontAssets.ItemStack.Value;

        Vector2 textSize = font.MeasureString(text);
        Vector2 origin = new(textSize.X, 0);

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(dims.X + dims.Width - 36f, dims.Y + 8f), Color.White, 0f, origin, new(0.8f));

        Vector2 position = new(dims.X + dims.Width - 28, dims.Y + 4);
        Rectangle rectangle = texture.Frame(2, 2, Value.ToInt(), IsLocked.ToInt());

        spriteBatch.Draw(texture, position, rectangle, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }

    #endregion
}
