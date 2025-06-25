using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Registries;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("CalamityFables")]
[ExtendsFromMod("CalamityFables")]
[Autoload(Side = ModSide.Client)]
public sealed class CalamityFablesSystem : ModSystem
{
    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load() => IsEnabled = true;

    #endregion

    public static bool IsEdgeCase()
    {
        return (Main.moonType - (Textures.Moon.Length - 1)) switch
        {
            1 => true,
            2 => true,
            8 => true,
            9 => true,
            10 => true,
            13 => true,
            14 => true,
            15 => true,
            _ => false
        };
    }

    public static void DrawMoon(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {

    }
}
