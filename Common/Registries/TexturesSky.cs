using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

namespace ZensSky.Common.Registries;

public static partial class Textures
{
    private const int MoonTextures = 9;

    private static readonly Lazy<Asset<Texture2D>> _star = new(() => Request("Sky/Star"));

    private static readonly Lazy<Asset<Texture2D>> _sunBloom = new(() => Request("Sky/SunBloom"));
    private static readonly Lazy<Asset<Texture2D>> _sunglasses = new(() => Request("Sky/Sunglasses"));

        // Probably stupid.
    private static readonly Lazy<Asset<Texture2D>[]> _moon = new(() => RequestArray("Sky/Moon", MoonTextures));
    private static readonly Lazy<Asset<Texture2D>> _moon2Rings = new(() => Request("Sky/Rings"));
    private static readonly Lazy<Asset<Texture2D>> _pumpkinMoon = new(() => Request("Sky/MoonPumpkin"));
    private static readonly Lazy<Asset<Texture2D>> _snowMoon = new(() => Request("Sky/MoonSnow"));

    public static Asset<Texture2D> Star => _star.Value;

    public static Asset<Texture2D> SunBloom => _sunBloom.Value;
    public static Asset<Texture2D> Sunglasses => _sunglasses.Value;

    public static Asset<Texture2D>[] Moon => _moon.Value;
    public static Asset<Texture2D> Moon2Rings => _moon2Rings.Value;
    public static Asset<Texture2D> PumpkinMoon => _pumpkinMoon.Value;
    public static Asset<Texture2D> SnowMoon => _snowMoon.Value;
}
