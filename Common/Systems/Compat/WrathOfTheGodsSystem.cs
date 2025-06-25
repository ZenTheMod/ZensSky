using System;
using System.Reflection;
using Terraria.ModLoader;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("NoxusBoss")]
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
        IsEnabled = true;

            // I don't feel like adding a project reference for a massive mod just for 4 lines of compat.
        Assembly noxusBossAsm = ModLoader.GetMod("NoxusBoss").Code;

        Type? sunMoonPositionRecorder = noxusBossAsm.GetType("NoxusBoss.Core.Graphics.SunMoonPositionRecorder");

        SetSunPosition = sunMoonPositionRecorder?.GetProperty("SunPosition", Public | Static)?.GetSetMethod(true);
        SetMoonPosition = sunMoonPositionRecorder?.GetProperty("MoonPosition", Public | Static)?.GetSetMethod(true);
    }

    #endregion

    #region Public Methods

    public static void UpdateSunAndMoonPosition(Vector2 position)
    {
        SetSunPosition?.Invoke(null, [position]);
        SetMoonPosition?.Invoke(null, [position]);
    }

    public static void UpdateMoonPosition(Vector2 position) =>
        SetMoonPosition?.Invoke(null, [position]);

    #endregion
}
