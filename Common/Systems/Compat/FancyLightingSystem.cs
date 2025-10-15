using FancyLighting;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Sky.Space;
using ZensSky.Core;

namespace ZensSky.Common.Systems.Compat;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="FancyLightingMod._Main_DrawSunAndMoon"/><br/>
///         Unapply unwanted shader effect on the sun when the Sun and Moon Rework is active.
///     </item>
///     <item>
///         <see cref="FancySkyRendering._Main_DrawStarsInBackground"/><br/>
///         Reapply hook to allow it to take priority over <see cref="StarRendering.DrawStarsInBackground"/>.
///     </item>
/// </list>
/// </summary>
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
            if (SkyConfig.Instance.UseSunAndMoon)
                On_Main.DrawSunAndMoon -= ModContent.GetInstance<FancyLightingMod>()._Main_DrawSunAndMoon;

                // Reapply their background gradient hook so it takes priority over ours.
            On_Main.DrawStarsInBackground -= FancySkyRendering._Main_DrawStarsInBackground;
            On_Main.DrawStarsInBackground += FancySkyRendering._Main_DrawStarsInBackground;
        });
    }

    #endregion
}
