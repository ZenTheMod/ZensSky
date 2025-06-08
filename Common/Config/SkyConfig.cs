using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using ZensSky.Common.Config.Elements;
using ZensSky.Common.Systems.Compat;

namespace ZensSky.Common.Config;

internal sealed class SunAndMoonReworkElement : LockedBoolElement { public override bool IsLocked => !SkyConfig.Instance.SunAndMoonRework; }
internal sealed class RealisticSkyElement : LockedBoolElement { public override bool IsLocked => !RealisticSkySystem.IsEnabled; }
internal sealed class WindOpacityElement : LockedFloatSlider { public override bool IsLocked => !SkyConfig.Instance.WindParticles; }
internal sealed class ColorStepsElement : LockedIntSlider { public override bool IsLocked => !SkyConfig.Instance.PixelatedSky; }

public sealed class SkyConfig : ModConfig
{
    public static SkyConfig Instance => ModContent.GetInstance<SkyConfig>();

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("SunMoon")]

    [ReloadRequired]
    [DefaultValue(true)]
    public bool SunAndMoonRework;
    
    [DefaultValue(true)]
    [CustomModConfigItem(typeof(EclipseLocalizedBoolElement))]
    public bool EclipseMode;

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(SunAndMoonReworkElement))]
    public bool TransparentMoonShadow;

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(RealisticSkyElement))]
    public bool RealisticSun;

    [Header("Stars")]

    [DefaultValue(false)]
    public bool VanillaStyleStars;

    [DefaultValue(true)]
    [CustomModConfigItem(typeof(RealisticSkyElement))]
    public bool DrawRealisticStars;

    [Header("Pixelation")]

    [DefaultValue(false)]
    public bool PixelatedSky;

    [DefaultValue(16)]
    [CustomModConfigItem(typeof(ColorStepsElement))]
    [SliderColor(240, 103, 135)]
    [Range(8, 255)]
    public int ColorSteps;

    [Header("Clouds")]

    [DefaultValue(true)]
    public bool CloudsEnabled;

    [Header("Ambient")]

    [DefaultValue(true)]
    public bool WindParticles;

    [DefaultValue(0.85f)]
    [CustomModConfigItem(typeof(WindOpacityElement))]
    [SliderColor(148, 203, 227)]
    [Range(0f, 1f)]
    public float WindOpacity;

    [DefaultValue(false)]
    public bool PitchBlackBackground;
}
