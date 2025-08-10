using Terraria.ModLoader;

namespace ZensSky.Common.Systems.Compat;

[Autoload(Side = ModSide.Client)]
public sealed class DarkSurfaceSystem : ModSystem
{
    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        if (!ModLoader.HasMod("DarkSurface"))
            return;

        IsEnabled = true;
    }

    #endregion
}
