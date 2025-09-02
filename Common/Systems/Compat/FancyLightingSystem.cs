using FancyLighting;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("FancyLighting")]
[ExtendsFromMod("FancyLighting")]
[Autoload(Side = ModSide.Client)]
public sealed class FancyLightingSystem : ModSystem
{
    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void PostSetupContent()
    {
        IsEnabled = true;

        MainThreadSystem.Enqueue(() =>
        {
                // Remove their hook that applies an unwanted shader.
            if (SkyConfig.Instance.SunAndMoonRework)
                On_Main.DrawSunAndMoon -= ModContent.GetInstance<FancyLightingMod>()._Main_DrawSunAndMoon;

                // Reapply their background gradient hook so it takes priority over ours.
            On_Main.DrawStarsInBackground -= FancySkyRendering._Main_DrawStarsInBackground;
            On_Main.DrawStarsInBackground += FancySkyRendering._Main_DrawStarsInBackground;
        });
    }

    #endregion
}
