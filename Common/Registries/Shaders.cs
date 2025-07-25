﻿using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.ModLoader;

namespace ZensSky.Common.Registries;

public static class Shaders
{
    private const string Prefix = "ZensSky/Assets/Effects/";

    private static readonly Lazy<Asset<Effect>> _planet = new(() => Request("Sky/Planet"));
    private static readonly Lazy<Asset<Effect>> _rings = new(() => Request("Sky/Rings"));
    private static readonly Lazy<Asset<Effect>> _eclipse = new(() => Request("Sky/Eclipse"));
    private static readonly Lazy<Asset<Effect>> _cloud = new(() => Request("Sky/CloudLighting"));
    private static readonly Lazy<Asset<Effect>> _supernova = new(() => Request("Sky/Supernova"));
    private static readonly Lazy<Asset<Effect>> _meteor = new(() => Request("Sky/Meteor"));

    private static readonly Lazy<Asset<Effect>> _pixelateAndQuantize = new(() => Request("Sky/PixelateAndQuantize"));

    private static readonly Lazy<Asset<Effect>> _starAtmosphere = new(() => Request("Compat/StarAtmosphere"));

    private static readonly Lazy<Asset<Effect>> _shatter = new(() => Request("Compat/Shatter"));
    private static readonly Lazy<Asset<Effect>> _cyst = new(() => Request("Compat/Cyst"));

    private static readonly Lazy<Asset<Effect>> _panel = new(() => Request("UI/Panel"));

    public static Asset<Effect> Planet => _planet.Value;
    public static Asset<Effect> Rings => _rings.Value;
    public static Asset<Effect> Eclipse => _eclipse.Value;
    public static Asset<Effect> Cloud => _cloud.Value;
    public static Asset<Effect> Supernova => _supernova.Value;
    public static Asset<Effect> Meteor => _meteor.Value;

    public static Asset<Effect> PixelateAndQuantize => _pixelateAndQuantize.Value;

    public static Asset<Effect> StarAtmosphere => _starAtmosphere.Value;

    public static Asset<Effect> Shatter => _shatter.Value;
    public static Asset<Effect> Cyst => _cyst.Value;

    public static Asset<Effect> Panel => _panel.Value;

    private static Asset<Effect> Request(string path) => ModContent.Request<Effect>(Prefix + path);
}
