using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace ZensSky.Common.Registries;

public static class Textures
{
    private const string Prefix = "ZensSky/Assets/Textures/";

    public static readonly Asset<Texture2D> Invis = Request("MagicPixel");
    public static readonly Asset<Texture2D> Pixel = Request("NotSoMagicPixel");

    public static readonly Asset<Texture2D> LockedToggle = Request("UI/LockedSettingsToggle");

    public static readonly Asset<Texture2D> Star = Request("Sky/Star");

    private static Asset<Texture2D> Request(string path) => ModContent.Request<Texture2D>(Prefix + path);
}
