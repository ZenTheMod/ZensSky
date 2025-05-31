using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ZensSky.Common.Config;

public sealed class MenuConfig : ModConfig
{
    public static MenuConfig Instance => ModContent.GetInstance<MenuConfig>();

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(1f)]
    [Range(-20f, 20f)]
    public float TimeMultiplier;

    [DefaultValue(0f)]
    [Range(0f, 1f)]
    public float Rain;

    [DefaultValue(false)]
    public bool UseWind;

    [DefaultValue(0f)]
    [Range(-1f, 1f)]
    public float Wind;

    [DefaultValue(4f)]
    [Range(-5f, 5f)]
    public float Parallax;

    [DefaultValue(false)]
    public bool UseCloudDensity;

    [DefaultValue(0.3f)]
    [Range(0f, 1f)]
    public float CloudDensity;
}
