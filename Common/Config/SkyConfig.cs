using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ZensSky.Common.Config;

internal sealed class PixelatedSunAndMoonLockedBoolElement : BaseLockedBoolElement { public override bool LockToggle => !SkyConfig.Instance.SunAndMoonRework || SkyConfig.Instance.MinimizeRenderTargetUsage; public override bool LockMode => true; }
internal sealed class TransparentMoonShadowLockedBoolElement : BaseLockedBoolElement { public override bool LockToggle => SkyConfig.Instance.SunAndMoonRework; public override bool LockMode => false; }
internal sealed class PixelatedStarsLockedBoolElement : BaseLockedBoolElement { public override bool LockToggle => SkyConfig.Instance.MinimizeRenderTargetUsage; public override bool LockMode => true; }
    // internal sealed class RealisticSkyLockedBoolElement : BaseLockedBoolElement { public override bool LockToggle => RealisticSkyCompatSystem.RealisticSkyEnabled; public override bool LockMode => false; }

public class SkyConfig : ModConfig
{
    public static SkyConfig Instance => ModContent.GetInstance<SkyConfig>();

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(false)]
    public bool MinimizeRenderTargetUsage;

    [Header("SunMoon")]

    [DefaultValue(true)]
    [ReloadRequired]
    public bool SunAndMoonRework;

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(PixelatedSunAndMoonLockedBoolElement))]
    public bool PixelatedSunAndMoon;

    [DefaultValue(false)]
    [CustomModConfigItem(typeof(TransparentMoonShadowLockedBoolElement))]
    public bool TransparentMoonShadow;

    [Header("Stars")]

    [DefaultValue(false)]
    public bool PixelatedStars;

    [DefaultValue(false)]
    public bool VanillaStyleStars;

        // [DefaultValue(true)]
        // [CustomModConfigItem(typeof(RealisticSkyLockedBoolElement))]
        // public bool DrawRealisticStars;
}
