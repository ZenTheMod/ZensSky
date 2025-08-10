using System.ComponentModel;
using Terraria.ModLoader.Config;
using ZensSky.Common.Config.Elements;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Config.Elements;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CA2211 // Non-constant fields should not be visible.

namespace ZensSky.Common.Config;

public sealed class SkyConfig : ModConfig
{
        // ConfigManager.Add Sets any instance fields to the ModConfig type.
    public static SkyConfig Instance;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("SunMoon")]

    [ReloadRequired]
    [DefaultValue(true)]
    public bool SunAndMoonRework;

    [DefaultValue(false)]
    [LockedElement(typeof(SkyConfig), nameof(SunAndMoonRework), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool TransparentMoonShadow;

    [DefaultValue(false)]
    [LockedElement(typeof(RealisticSkySystem), nameof(RealisticSkySystem.IsEnabled), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool RealisticSun;

    [Header("Stars")]

    [DefaultValue(StarVisual.Vanilla)]
    [CustomModConfigItem(typeof(StarEnumElement))]
    public StarVisual StarStyle;

    [DefaultValue(true)]
    [LockedElement(typeof(RealisticSkySystem), nameof(RealisticSkySystem.IsEnabled), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool DrawRealisticStars;

    [Header("Pixelation")]

    [DefaultValue(false)]
    public bool PixelatedSky;

    [DefaultValue(16)]
    [LockedElement(typeof(SkyConfig), nameof(PixelatedSky), false)]
    [CustomModConfigItem(typeof(LockedIntSlider))]
    [SliderColor(240, 103, 135)]
    [Range(8, 255)]
    public int ColorSteps;

    [Header("Clouds")]

    [DefaultValue(true)]
    public bool CloudsEnabled;

    [DefaultValue(true)]
    [LockedElement(typeof(SkyConfig), nameof(CloudsEnabled), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool CloudsEdgeLighting;

    [Header("Ambient")]

    [DefaultValue(true)]
    public bool WindParticles;

    [DefaultValue(.85f)]
    [LockedElement(typeof(SkyConfig), nameof(WindParticles), false)]
    [CustomModConfigItem(typeof(LockedFloatSlider))]
    [SliderColor(148, 203, 227)]
    [Range(0f, 1f)]
    public float WindOpacity;

    [DefaultValue(false)]
    [LockedElement(typeof(DarkSurfaceSystem), nameof(DarkSurfaceSystem.IsEnabled), true)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool PitchBlackBackground;
}
