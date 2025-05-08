using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;
using Terraria.UI;
using Terraria;
using Terraria.ModLoader.UI;
using Terraria.Localization;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Config;

public abstract class BaseLockedBoolElement : ConfigElement<bool>
{
    private const float LockedOffset = 110f;
    private const float UnlockedOffset = 60f;

    private const float LockedBackgroundMultiplier = 0.4f;

    /// <summary>
    /// The bool that can "lock" this bool.
    /// </summary>
    public abstract bool IsLocked { get; }

    public override void OnBind()
    {
        base.OnBind();

        OnLeftClick += delegate
        {
            if (!IsLocked)
                Value = !Value;
        };
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
            // Change the background color before drawing the base ConfigElement<T>.
        backgroundColor = IsLocked ? (UICommon.DefaultUIBlue * LockedBackgroundMultiplier) : UICommon.DefaultUIBlue;
        base.DrawSelf(spriteBatch);

        Texture2D texture = Textures.LockedToggle.Value;

        CalculatedStyle dimensions = GetDimensions();

        string text = Value ? Lang.menu[126].Value : Lang.menu[124].Value; // On / Off

        if (IsLocked)
            text += " " + Language.GetTextValue("Mods.ZensSky.Configs.Locked");

        float offset = IsLocked ? LockedOffset : UnlockedOffset;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text, new Vector2(dimensions.X + dimensions.Width - offset, dimensions.Y + 8f), Color.White, 0f, Vector2.Zero, new Vector2(0.8f));

        Vector2 position = new(dimensions.X + dimensions.Width - 28, dimensions.Y + 4);
        Rectangle rectangle = texture.Frame(2, 2, Value.ToInt(), IsLocked.ToInt());

        spriteBatch.Draw(texture, position, rectangle, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }
}
