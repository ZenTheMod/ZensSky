using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.ModLoader;

namespace ZensSky.Common.Registries;

public static class Textures
{
    private const string Prefix = "ZensSky/Assets/Textures/";

    private const int MoonTextures = 9;

    private static readonly Lazy<Asset<Texture2D>> _pixel = new(() => Request("Pixel"));
    private static readonly Lazy<Asset<Texture2D>> _invis = new(() => Request("Invis"));

    private static readonly Lazy<Asset<Texture2D>> _lockedToggle = new(() => Request("UI/LockedSettingsToggle"));

    private static readonly Lazy<Asset<Texture2D>> _star = new(() => Request("Sky/Star"));

    private static readonly Lazy<Asset<Texture2D>> _sunBloom = new(() => Request("Sky/SunBloom"));
    private static readonly Lazy<Asset<Texture2D>> _sunglasses = new(() => Request("Sky/Sunglasses"));

        // Probably stupid.
    private static readonly Lazy<Asset<Texture2D>[]> _moon = new(() => RequestArray("Sky/Moon", MoonTextures));
    private static readonly Lazy<Asset<Texture2D>> _moon2Rings = new(() => Request("Sky/Rings"));
    private static readonly Lazy<Asset<Texture2D>> _pumpkinMoon = new(() => Request("Sky/MoonPumpkin"));
    private static readonly Lazy<Asset<Texture2D>> _snowMoon = new(() => Request("Sky/MoonSnow"));

    private static readonly Lazy<Asset<Texture2D>> _modDeps = new(() => Request("UI/PanelStyle/ModDeps"));
    private static readonly Lazy<Asset<Texture2D>> _modConfig = new(() => Request("UI/PanelStyle/ModConfig"));
    private static readonly Lazy<Asset<Texture2D>> _modInfo = new(() => Request("UI/PanelStyle/ModInfo"));

    private static readonly Lazy<Asset<Texture2D>> _panelGradient = new(() => Request("UI/PanelStyle/PanelGradient"));

    public static Asset<Texture2D> Pixel => _pixel.Value;
    public static Asset<Texture2D> Invis => _invis.Value;

    public static Asset<Texture2D> LockedToggle => _lockedToggle.Value;

    public static Asset<Texture2D> Star => _star.Value;

    public static Asset<Texture2D> SunBloom => _sunBloom.Value;
    public static Asset<Texture2D> Sunglasses => _sunglasses.Value;

    public static Asset<Texture2D>[] Moon => _moon.Value;
    public static Asset<Texture2D> Moon2Rings => _moon2Rings.Value;
    public static Asset<Texture2D> PumpkinMoon => _pumpkinMoon.Value;
    public static Asset<Texture2D> SnowMoon => _snowMoon.Value;

    public static Asset<Texture2D> ModDeps => _modDeps.Value;
    public static Asset<Texture2D> ModConfig => _modConfig.Value;
    public static Asset<Texture2D> ModInfo => _modInfo.Value;

    public static Asset<Texture2D> PanelGradient => _panelGradient.Value;

    private static Asset<Texture2D> Request(string path) => ModContent.Request<Texture2D>(Prefix + path);

    private static Asset<Texture2D>[] RequestArray(string TexturePath, int count)
    {
        Asset<Texture2D>[] textures = new Asset<Texture2D>[count];

        for (int i = 0; i < count; i++)
            textures[i] = ModContent.Request<Texture2D>(Prefix + TexturePath + i);

        return textures;
    }
}
