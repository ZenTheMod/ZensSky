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

    public override ConfigScope Mode =>
        ConfigScope.ClientSide;

    #region SunAndMoon

    [Header("SunAndMoon")]

    [ReloadRequired]
    [DefaultValue(true)]
        // Notably don't decorate this member with the LockedElement attribute, this is just to fix the offset on boolean elements.
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool UseSunAndMoon;

    [DefaultValue(false)]
    [LockedElement(typeof(SkyConfig), nameof(UseSunAndMoon), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool TransparentMoonShadow;

    [DefaultValue(false)]
    [LockedElement(typeof(RealisticSkySystem), nameof(RealisticSkySystem.IsEnabled), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool RealisticSun;

    #endregion

    #region Stars

    [Header("Stars")]

    [DefaultValue(StarVisual.Vanilla)]
    [CustomModConfigItem(typeof(StarEnumElement))]
    public StarVisual StarStyle;

    [DefaultValue(true)]
    [LockedElement(typeof(RealisticSkySystem), nameof(RealisticSkySystem.IsEnabled), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool DrawRealisticStars;

    #endregion

    #region Background

    [Header("Background")]

    [DefaultValue(true)]
    [LockedElement(typeof(DarkSurfaceSystem), nameof(DarkSurfaceSystem.IsEnabled), true)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool PitchBlackBackground;

    [CustomModConfigItem(typeof(SkyGradientElement))]
    public Gradient SkyGradient = new(32)
    {
        new(.15f, new(13, 13, 70)),
        new(.195f, new(197, 101, 192)),
        new(.21f, new(255, 151, 125)),
        new(.25f, new(219, 188, 126)),
        new(.375f, new(74, 111, 137)),
        new(.6f, new(74, 111, 137)),
        new(.7f, new(211, 200, 144)),
        new(.75f, new(255, 109, 182)),
        new(.82f, new(13, 13, 70))
    };

    #endregion

    #region Clouds

    [Header("Clouds")]

    [DefaultValue(true)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool UseCloudLighting;

    [DefaultValue(true)]
    [LockedElement(typeof(SkyConfig), nameof(UseCloudLighting), false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool UseCloudGodrays;

    private bool CloudGodraysSamplesLocked =>
        UseCloudLighting && UseCloudGodrays;

    [DefaultValue(32)]
    [LockedElement(typeof(SkyConfig), nameof(CloudGodraysSamplesLocked), false)]
    [CustomModConfigItem(typeof(LockedIntSlider))]
    [SliderColor(240, 103, 135)]
    [Range(4, 64)]
    public int CloudGodraysSamples;

    #endregion

    #region Weather

    [Header("Weather")]

    [DefaultValue(true)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool UseWindParticles;

    [DefaultValue(.85f)]
    [LockedElement(typeof(SkyConfig), nameof(UseWindParticles), false)]
    [CustomModConfigItem(typeof(LockedFloatSlider))]
    [SliderColor(148, 203, 227)]
    [Range(0f, 1f)]
    public float WindOpacity;

    #endregion

    #region Pixelation

    [Header("Pixelation")]

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool UsePixelatedSky;

    [DefaultValue(16)]
    [LockedElement(typeof(SkyConfig), nameof(UsePixelatedSky), false)]
    [CustomModConfigItem(typeof(LockedIntSlider))]
    [SliderColor(240, 103, 135)]
    [Range(8, 256)]
    public int ColorSteps;

    #endregion
}
