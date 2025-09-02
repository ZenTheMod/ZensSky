using System;
using System.Reflection;
using Terraria.ModLoader;
using ZensSky.Common.DataStructures;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonHooks;

namespace ZensSky.Common.Systems.Compat;

[ExtendsFromMod("NoxusBoss")]
[Autoload(Side = ModSide.Client)]
public sealed class WrathOfTheGodsSystem : ModSystem
{
    #region Private Fields

    private static MethodInfo? SetSunPosition;
    private static MethodInfo? SetMoonPosition;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        if (!ModLoader.HasMod("NoxusBoss"))
            return;

        IsEnabled = true;

        OnUpdateSunAndMoonInfo += UpdateSunMoonPositionRecorder;

            // I don't feel like adding a project reference for a massive mod just for 4 lines of compat.
        Assembly noxusBossAsm = ModLoader.GetMod("NoxusBoss").Code;

        Type? sunMoonPositionRecorder = noxusBossAsm.GetType("NoxusBoss.Core.Graphics.SunMoonPositionRecorder");
        ArgumentNullException.ThrowIfNull(sunMoonPositionRecorder);

        SetSunPosition = sunMoonPositionRecorder?.GetProperty("SunPosition", Public | Static)?.GetSetMethod(true);
        ArgumentNullException.ThrowIfNull(SetSunPosition);

        SetMoonPosition = sunMoonPositionRecorder?.GetProperty("MoonPosition", Public | Static)?.GetSetMethod(true);
        ArgumentNullException.ThrowIfNull(SetMoonPosition);
    }

    #endregion

    #region Public Methods

    public static void UpdateSunMoonPositionRecorder(SunAndMoonInfo info)
    {
        SetSunPosition?.Invoke(null, [info.SunPosition]);
        SetMoonPosition?.Invoke(null, [info.MoonPosition]);
    }

    public static void UpdateMoonPosition(Vector2 position) =>
        SetMoonPosition?.Invoke(null, [position]);

    #endregion
}
