using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ZensSky.Common.Config;

public sealed class MenuConfig : ModConfig
{
    public static MenuConfig Instance => ModContent.GetInstance<MenuConfig>();

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(4f)]
    [Range(-5f, 5f)]
    public float Parallax;

    [DefaultValue(false)]
    public bool UseCloudDensity;

    [DefaultValue(0.3f)]
    [Range(0f, 1f)]
    public float CloudDensity;
}
