using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using ZensSky.Common.DataStructures;
using ZensSky.Core.Utils;

namespace ZensSky.Common.Systems.Sky.Lighting;

public sealed class SkyLightSystem : ModSystem
{
    #region Public Properties

    public static List<SkyLight> Lights { get; }
        = [];

    #endregion

    #region Loading

    public override void PostSetupContent() =>
        Lights.AddRange(Utilities.GetAllInstancesOf<SkyLight>(Mod.Code));

    #endregion

    #region Public Methods

    public static void InvokeForActiveLights(Action<SkyLightInfo> action)
    {
        foreach (SkyLight light in Lights)
        {
            SkyLightInfo info = light.GetInfo();

            Color color = info.Color;

            if (light.Active &&
                (color.R > 0 ||
                color.G > 0 ||
                color.B > 0))
                action(light.GetInfo());
        }
    }

    #endregion
}
