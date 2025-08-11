using Terraria.ModLoader;
using ZensSky.Common.Systems.Ambience;
using DarkSurfaceSys = DarkSurface.DarkSurfaceSystem;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("DarkSurface")]
[ExtendsFromMod("DarkSurface")]
[Autoload(Side = ModSide.Client)]
public sealed class DarkSurfaceSystem : ModSystem
{
    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // Annoyingly DarkSurface is a Both-Sided mod, meaning I cannot deliberatly load before or after it.
    public override void PostSetupContent()
    {
        IsEnabled = true;

        SkyColorSystem.ModifyInMenu +=
            ModContent.GetInstance<DarkSurfaceSys>().ModifySunLightColor;
    }

    #endregion
}
