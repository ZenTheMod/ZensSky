using System.ComponentModel;
using Terraria.ModLoader.Config;
using ZensSky.Common.Config.Elements;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Config.Elements;
using ZensSky.Core.DataStructures;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CA2211 // Non-constant fields should not be visible.

namespace ZensSky.Common.Config;

public sealed class SkyConfig : ModConfig
{
        // 'ConfigManager.Add' Automatically sets public fields named 'Instance' to the ModConfig's type.
    public static SkyConfig Instance;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("SunMoon")]

    [ReloadRequired]
    [DefaultValue(true)]
    public bool UseSunAndMoon;

    [DefaultValue(false)]
    [LockedElement(typeof(SkyConfig), nameof(UseSunAndMoon), false)]
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
    public bool UsePixelatedSky;

    [DefaultValue(16)]
    [LockedElement(typeof(SkyConfig), nameof(UsePixelatedSky), false)]
    [CustomModConfigItem(typeof(LockedIntSlider))]
    [SliderColor(240, 103, 135)]
    [Range(8, 256)]
    public int ColorSteps;

    [Header("Clouds")]

    [DefaultValue(true)]
    public bool UseCloudLighting;

    [DefaultValue(32)]
    [LockedElement(typeof(SkyConfig), nameof(UseCloudLighting), false)]
    [CustomModConfigItem(typeof(LockedIntSlider))]
    [SliderColor(240, 103, 135)]
    [Range(4, 64)]
    public int CloudLightingSamples;

    [Header("Ambient")]

    [DefaultValue(true)]
    public bool UseWindParticles;

    [DefaultValue(.85f)]
    [LockedElement(typeof(SkyConfig), nameof(UseWindParticles), false)]
    [CustomModConfigItem(typeof(LockedFloatSlider))]
    [SliderColor(148, 203, 227)]
    [Range(0f, 1f)]
    public float WindOpacity;

    [DefaultValue(true)]
    [LockedElement(typeof(DarkSurfaceSystem), nameof(DarkSurfaceSystem.IsEnabled), true)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool PitchBlackBackground;

    [CustomModConfigItem(typeof(GradientElement))]
    public Gradient SkyGradient =
        [new(0f, Color.Black), new(.333f, Color.Yellow), new(.5f, Color.Green), new(1f, Color.White)];
}
