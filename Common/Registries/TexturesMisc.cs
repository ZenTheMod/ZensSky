using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.ModLoader;

namespace ZensSky.Common.Registries;

public static partial class Textures
{
    private const string Prefix = "ZensSky/Assets/Textures/";
    private static readonly Lazy<Asset<Texture2D>> _pixel = new(() => Request("Pixel"));
    private static readonly Lazy<Asset<Texture2D>> _invis = new(() => Request("Invis"));

    public static Asset<Texture2D> Pixel => _pixel.Value;
    public static Asset<Texture2D> Invis => _invis.Value;
    private static Asset<Texture2D> Request(string path) => ModContent.Request<Texture2D>(Prefix + path);

    private static Asset<Texture2D>[] RequestArray(string TexturePath, int count)
    {
        Asset<Texture2D>[] textures = new Asset<Texture2D>[count];

        for (int i = 0; i < count; i++)
            textures[i] = ModContent.Request<Texture2D>(Prefix + TexturePath + i);

        return textures;
    }
}
