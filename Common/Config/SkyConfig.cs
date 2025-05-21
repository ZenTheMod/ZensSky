using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using ZensSky.Common.Config.Elements;

namespace ZensSky.Common.Config;

internal sealed class PixelatedSunAndMoonElement : BaseLockedBoolElement { public override bool IsLocked => !SkyConfig.Instance.SunAndMoonRework || SkyConfig.Instance.MinimizeRenderTargetUsage; }
internal sealed class TransparentMoonShadowElement : BaseLockedBoolElement { public override bool IsLocked => !SkyConfig.Instance.SunAndMoonRework; }
internal sealed class PixelatedStarsElement : BaseLockedBoolElement { public override bool IsLocked => SkyConfig.Instance.MinimizeRenderTargetUsage; }
    // internal sealed class RealisticSkyLockedBoolElement : BaseLockedBoolElement { public override bool LockToggle => RealisticSkyCompatSystem.RealisticSkyEnabled; public override bool LockMode => false; }

public sealed class SkyConfig : ModConfig
{
    public static SkyConfig Instance => ModContent.GetInstance<SkyConfig>();

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)]
    public bool MinimizeRenderTargetUsage;

    [Header("SunMoon")]

    [DefaultValue(true)]
    [ReloadRequired]
    public bool SunAndMoonRework;

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(PixelatedSunAndMoonElement))]
    public bool PixelatedSunAndMoon;
    
    [DefaultValue(true)]
    [CustomModConfigItem(typeof(EclipseLocalizedBoolElement))]
    public bool EclipseMode;

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(TransparentMoonShadowElement))]
    public bool TransparentMoonShadow;

    [Header("Stars")]

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(PixelatedStarsElement))]
    public bool PixelatedStars;

    [DefaultValue(false)]
    public bool VanillaStyleStars;

        // [DefaultValue(true)]
        // [CustomModConfigItem(typeof(RealisticSkyLockedBoolElement))]
        // public bool DrawRealisticStars;

    [Header("Clouds")]

    [DefaultValue(true)]
    public bool CloudsEnabled;

    [Header("Ambient")]

    [DefaultValue(true)]
    public bool WindParticles;
}
