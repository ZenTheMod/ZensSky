using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

namespace ZensSky.Common.Registries;

public static partial class Textures
{
    private const int MoonTextures = 9;
    private const int FablesMoonTextures = 16;

    private static readonly Lazy<Asset<Texture2D>> _star = new(() => Request("Sky/StarStyles/FourPointedStar"));
    private static readonly Lazy<Asset<Texture2D>> _outerWildsStar = new(() => Request("Sky/StarStyles/CircleStar"));
    private static readonly Lazy<Asset<Texture2D>> _diamondStar = new(() => Request("Sky/StarStyles/DiamondStar"));

    private static readonly Lazy<Asset<Texture2D>> _supernovaNoise = new(() => Request("Sky/Supernova"));

    private static readonly Lazy<Asset<Texture2D>> _sunBloom = new(() => Request("Sky/SunBloom"));
    private static readonly Lazy<Asset<Texture2D>> _sunglasses = new(() => Request("Sky/Sunglasses"));

    private static readonly Lazy<Asset<Texture2D>[]> _moon = new(() => RequestArray("Sky/Moon", MoonTextures));
    private static readonly Lazy<Asset<Texture2D>> _moon2Rings = new(() => Request("Sky/Rings"));
    private static readonly Lazy<Asset<Texture2D>> _pumpkinMoon = new(() => Request("Sky/MoonPumpkin"));
    private static readonly Lazy<Asset<Texture2D>> _snowMoon = new(() => Request("Sky/MoonSnow"));

    private static readonly Lazy<Asset<Texture2D>[]> _fablesMoon = new(() => RequestArray("Sky/FablesMoons/Moon", FablesMoonTextures));

    private static readonly Lazy<Asset<Texture2D>> _betterNightSkyMoon = new(() => Request("Sky/BetterNightSkyMoon"));

    private static readonly Lazy<Asset<Texture2D>> _shootingStar = new(() => Request("Sky/ShootingStar"));

    public static Asset<Texture2D> Star => _star.Value;
    public static Asset<Texture2D> OuterWildsStar => _outerWildsStar.Value;
    public static Asset<Texture2D> DiamondStar => _diamondStar.Value;

    public static Asset<Texture2D> ShootingStar => _shootingStar.Value;

    public static Asset<Texture2D> SupernovaNoise => _supernovaNoise.Value;

    public static Asset<Texture2D> Bloom => _sunBloom.Value;
    public static Asset<Texture2D> Sunglasses => _sunglasses.Value;

    public static Asset<Texture2D>[] Moon => _moon.Value;
    public static Asset<Texture2D> Moon2Rings => _moon2Rings.Value;
    public static Asset<Texture2D> PumpkinMoon => _pumpkinMoon.Value;
    public static Asset<Texture2D> SnowMoon => _snowMoon.Value;

    public static Asset<Texture2D>[] FablesMoon => _fablesMoon.Value;

    public static Asset<Texture2D> BetterNightSkyMoon => _betterNightSkyMoon.Value;
}
