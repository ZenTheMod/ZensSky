using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[Autoload(Side = ModSide.Client)]
public sealed class CalamityFablesSystem : ModSystem
{
    #region Public Properties

    public static int PriorMoonStyles { get; private set; }

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load() 
    {
        PriorMoonStyles = TextureAssets.Moon.Length;

        if (!ModLoader.HasMod("CalamityFables"))
            return;

        IsEnabled = true;

        Assembly fablessAsm = ModLoader.GetMod("CalamityFables").Code;

        Type? moddedMoons = fablessAsm.GetType("CalamityFables.Core.ModdedMoons");

        FieldInfo? vanillaMoonCount = moddedMoons?.GetField("VanillaMoonCount", Public | Static);

        PriorMoonStyles = (int?)vanillaMoonCount?.GetValue(null) ?? PriorMoonStyles;
    }

    #endregion

    public static bool IsEdgeCase()
    {
        return (Main.moonType - (PriorMoonStyles - 1)) switch
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
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, (Main.moonType - (PriorMoonStyles - 1)).ToString(), position, Color.White, 0f, Vector2.Zero, Vector2.One);
    }
}
