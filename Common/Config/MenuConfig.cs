using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria.ModLoader.Config;
using ZensSky.Core.Config;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CA2211 // Non-constant fields should not be visible.

namespace ZensSky.Common.Config;

[HideConfig]
public sealed class MenuConfig : ModConfig
{
        // ConfigManager.Add Sets any instance fields to the ModConfig type.
    public static MenuConfig Instance;

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

    [DefaultValue(false)]
    public bool UseMenuButtonColor;

    [DefaultValue(typeof(Vector3), "0, 0, 0")]
    public Vector3 MenuButtonColor = new();

    [DefaultValue(false)]
    public bool UseMenuButtonHoverColor;

    [DefaultValue(typeof(Vector3), "0, 0, 0")]
    public Vector3 MenuButtonHoverColor = new();
}
