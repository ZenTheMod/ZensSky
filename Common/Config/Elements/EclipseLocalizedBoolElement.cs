using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;
using Terraria.UI;
using Terraria.ModLoader;
using ZensSky.Common.MenuStyles;
using Terraria;

namespace ZensSky.Common.Config.Elements;

public sealed class EclipseLocalizedBoolElement : ConfigElement<bool>
{
    private const string LocalizationKey = "Mods.ZensSky.Configs.SkyConfig.EclipseMode.Name";

    public override void OnBind()
    {
        base.OnBind();

        OnLeftClick += delegate
        {
            Value = !Value;
        };
        OnRightClick += delegate
        {
            if (Main.gameMenu)
                MenuLoader.switchToMenu = ModContent.GetInstance<Eclipse>();
        };
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        CalculatedStyle dimensions = GetDimensions();

        string text = Language.GetTextValue(LocalizationKey + Value);

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text, new Vector2(dimensions.X + dimensions.Width - 60, dimensions.Y + 8f), Color.White, 0f, Vector2.Zero, new Vector2(0.8f));
    }
}
