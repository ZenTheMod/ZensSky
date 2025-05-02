using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria;

namespace ZensSky.Common.MenuStyles;

internal class Eclipse : ModMenu
{
    public override string DisplayName => Language.GetTextValue("Mods.ZensSky.MenuStyles.Eclipse");
    public static bool InMenu => MenuLoader.CurrentMenu == ModContent.GetInstance<Eclipse>() && Main.gameMenu;

    public override void Load() => On_Main.DrawMenu += CastDarkness;

    public override void Unload() => On_Main.DrawMenu -= CastDarkness;

    private void CastDarkness(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);

        if (InMenu)
            Main.eclipse = true;
    }
}
