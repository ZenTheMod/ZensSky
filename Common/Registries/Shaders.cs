using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.ModLoader;

namespace ZensSky.Common.Registries;

public static class Shaders
{
    private const string Prefix = "ZensSky/Assets/Effects/Sky/";

    private static readonly Lazy<Asset<Effect>> _planet = new(() => Request("Planet"));
    private static readonly Lazy<Asset<Effect>> _rings = new(() => Request("Rings"));
    private static readonly Lazy<Asset<Effect>> _eclipse = new(() => Request("Eclipse"));

    public static Asset<Effect> Planet => _planet.Value;
    public static Asset<Effect> Rings => _rings.Value;
    public static Asset<Effect> Eclipse => _eclipse.Value;

    private static Asset<Effect> Request(string path) => ModContent.Request<Effect>(Prefix + path);
}
