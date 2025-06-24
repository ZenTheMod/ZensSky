using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Config.Elements;

public sealed class EclipseLocalizedBoolElement : ConfigElement<bool>
{
    private const float LockedBackgroundMultiplier = 0.4f;

    private const string LocalizationKey = "Mods.ZensSky.Configs.SkyConfig.EclipseMode.Name";

    private static bool IsLocked => !SkyConfig.Instance.SunAndMoonRework;

    public override void OnBind()
    {
        base.OnBind();

        OnLeftClick += delegate
        {
            if (!IsLocked)
                Value = !Value;
        };
            // FIX
        OnRightClick += delegate
        {
                // if (Main.gameMenu)
                    // MenuLoader.switchToMenu = ModContent.GetInstance<Eclipse>();
        };
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        backgroundColor = IsLocked ? UICommon.DefaultUIBlue * LockedBackgroundMultiplier : UICommon.DefaultUIBlue;
        base.DrawSelf(spriteBatch);
        CalculatedStyle dimensions = GetDimensions();

        string text = Language.GetTextValue(LocalizationKey + Value);

        if (IsLocked)
            text += " " + Language.GetTextValue("Mods.ZensSky.Configs.Locked");

        DynamicSpriteFont font = FontAssets.ItemStack.Value;

        Vector2 textSize = font.MeasureString(text);
        Vector2 origin = new(textSize.X, 0);

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(dimensions.X + dimensions.Width - 36, dimensions.Y + 8f), Color.White, 0f, origin, new(0.8f));

        if (!IsLocked)
            return;

        Texture2D lockTexture = Textures.Lock.Value;
        Vector2 position = new(dimensions.X + dimensions.Width - 24, dimensions.Y + 8);
        spriteBatch.Draw(lockTexture, position, null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }
}
