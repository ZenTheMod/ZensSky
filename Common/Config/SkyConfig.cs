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
            // TODO: Force this fix for vanilla.
    [CustomModConfigItem(typeof(LockedBoolElement))]
    public bool UseSunAndMoon;

    [DefaultValue(false)]
    [LockedElement(typeof(LockedBoolElement), typeof(SkyConfig), nameof(TransparentMoonShadowLocked))]
    public bool TransparentMoonShadow;

    [DefaultValue(false)]
    [LockedElement(typeof(LockedBoolElement), typeof(SkyConfig), nameof(RealisticSunLocked))]
    public bool RealisticSun;

    #endregion

    #region Stars

    [Header("Stars")]

    [DefaultValue(StarVisual.Vanilla)]
    [CustomModConfigItem(typeof(StarEnumElement))]
    public StarVisual StarStyle;

    [DefaultValue(true)]
    [LockedElement(typeof(LockedBoolElement), typeof(SkyConfig), nameof(DrawRealisticStarsLocked))]
    public bool DrawRealisticStars;

    #endregion

    #region Background

    [Header("Background")]

    [DefaultValue(true)]
    [LockedElement(typeof(LockedBoolElement), typeof(SkyConfig), nameof(PitchBlackBackgroundLocked))]
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
    [LockedElement(typeof(LockedBoolElement), typeof(SkyConfig), nameof(UseCloudGodraysLocked))]
    public bool UseCloudGodrays;

    [DefaultValue(32)]
    [LockedElement(typeof(LockedIntSlider), typeof(SkyConfig), nameof(CloudGodraysSamplesLocked))]
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
    [LockedElement(typeof(LockedFloatSlider), typeof(SkyConfig), nameof(WindOpacityLocked))]
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
    [LockedElement(typeof(LockedIntSlider), typeof(SkyConfig), nameof(ColorStepsLocked))]
    [SliderColor(240, 103, 135)]
    [Range(8, 256)]
    public int ColorSteps;

    #endregion

#pragma warning disable

    #region Locked Properties

        // -- SunAndMoon --
    private bool TransparentMoonShadowLocked =>
        !UseSunAndMoon;

    private bool RealisticSunLocked =>
        !RealisticSkySystem.IsEnabled || !UseSunAndMoon;

        // -- Stars --
    private bool DrawRealisticStarsLocked =>
        !RealisticSkySystem.IsEnabled;

        // -- Background --
    private bool PitchBlackBackgroundLocked =>
        DarkSurfaceSystem.IsEnabled;

        // -- Clouds --
    private bool UseCloudGodraysLocked =>
        !UseCloudLighting;

    private bool CloudGodraysSamplesLocked =>
        !UseCloudLighting || !UseCloudGodrays;

        // -- Weather --
    private bool WindOpacityLocked =>
        !UseWindParticles;

        // -- Pixelation --
    private bool ColorStepsLocked =>
        !UsePixelatedSky;

    #endregion
}
