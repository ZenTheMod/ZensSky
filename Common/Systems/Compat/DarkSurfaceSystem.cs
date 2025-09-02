using System;
using System.Reflection;
using Terraria.ModLoader;
using ZensSky.Common.Systems.Ambience;
using ZensSky.Core.Utils;

namespace ZensSky.Common.Systems.Compat;

[ExtendsFromMod("DarkSurface")]
[Autoload(Side = ModSide.Client)]
public sealed class DarkSurfaceSystem : ModSystem
{
    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // DarkSurface is a Both-Sided mod, meaning we cannot deliberatly load before or after it with build.txt sorting.
    public override void PostSetupContent()
    {
        if (!ModLoader.TryGetMod("DarkSurface", out Mod darkSurface))
            return;

        IsEnabled = true;

        Assembly darkAsm = darkSurface.Code;

        Type? darkSurfaceSystem = darkAsm.GetType("DarkSurface.DarkSurfaceSystem");
        ArgumentNullException.ThrowIfNull(darkSurfaceSystem);

        ModSystem system = (ModSystem)Utilities.GetInstance(darkSurfaceSystem);

        SkyColorSystem.ModifyInMenu +=
            system.ModifySunLightColor;
    }

    #endregion
}
