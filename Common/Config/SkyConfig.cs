﻿using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using ZensSky.Common.Config.Elements;
using ZensSky.Common.Systems.Compat;

namespace ZensSky.Common.Config;

internal sealed class SunAndMoonReworkElement : BaseLockedBoolElement { public override bool IsLocked => !SkyConfig.Instance.SunAndMoonRework; }
internal sealed class RealisticSkyElement : BaseLockedBoolElement { public override bool IsLocked => !RealisticSkySystem.IsEnabled; }

public sealed class SkyConfig : ModConfig
{
    public static SkyConfig Instance => ModContent.GetInstance<SkyConfig>();

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("SunMoon")]

    [DefaultValue(true)]
    [ReloadRequired]
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

    [Header("Clouds")]

    [DefaultValue(true)]
    public bool CloudsEnabled;

    [Header("Ambient")]

    [DefaultValue(true)]
    public bool WindParticles;

    [DefaultValue(false)]
    public bool PitchBlackBackground;
}
