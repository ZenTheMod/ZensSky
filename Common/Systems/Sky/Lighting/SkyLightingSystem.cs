using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using ZensSky.Core.Utils;

namespace ZensSky.Common.Systems.Sky.Lighting;

public sealed class SkyLightingSystem : ModSystem
{
    #region Public Properties

    public static List<ISkyLight> Lights { get; }
        = [];

    #endregion

    #region Loading

    public override void PostSetupContent()
    {
        Assembly assembly = Mod.Code;

        Type[] types = [.. assembly.GetTypes()
            .Where(p => p.IsAssignableTo(typeof(ISkyLight)) &&
            p.IsClass &&
            p != typeof(ISkyLight))];

        foreach (Type type in types)
            Lights.Add((ISkyLight)Activator.CreateInstance(type)!);
    }

    #endregion

    #region Public Methods

    public static void InvokeForActiveLights(Action<ISkyLight> action)
    {
        foreach (ISkyLight light in Lights)
            action(light);
    }

    #endregion
}
