﻿using RealisticSky.Common.DataStructures;
using System.ComponentModel;
using Terraria.ModLoader.Config;
using ZensSky.Common.Config.Elements;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CA2211 // Non-constant fields should not be visible.

namespace ZensSky.Common.Config;

internal sealed class SunAndMoonReworkElement : LockedBoolElement { public override bool IsLocked => !SkyConfig.Instance.SunAndMoonRework; }
internal sealed class RealisticSkyElement : LockedBoolElement { public override bool IsLocked => !RealisticSkySystem.IsEnabled; }
internal sealed class WindOpacityElement : LockedFloatSlider { public override bool IsLocked => !SkyConfig.Instance.WindParticles; }
internal sealed class ColorStepsElement : LockedIntSlider { public override bool IsLocked => !SkyConfig.Instance.PixelatedSky; }
internal sealed class CloudEdgeLightingElement : LockedBoolElement { public override bool IsLocked => !SkyConfig.Instance.CloudsEnabled; }

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
    [CustomModConfigItem(typeof(SunAndMoonReworkElement))]
    public bool TransparentMoonShadow;

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(RealisticSkyElement))]
    public bool RealisticSun;

    [Header("Stars")]

    [DefaultValue(StarVisual.Vanilla)]
    [CustomModConfigItem(typeof(StarEnumElement))]
    public StarVisual StarStyle;

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

    [DefaultValue(true)]
    [CustomModConfigItem(typeof(CloudEdgeLightingElement))]
    public bool CloudsEdgeLighting;

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
