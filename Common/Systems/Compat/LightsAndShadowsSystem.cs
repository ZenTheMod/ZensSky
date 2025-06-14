using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("Lights")]
[ExtendsFromMod("Lights")]
[Autoload(Side = ModSide.Client)]
public sealed class LightsAndShadowsSystem : ModSystem
{
    #region Private Fields

    private delegate Vector2 orig_GetSunPos(RenderTarget2D render);
    private static Hook? ModifySunPosition;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? getSunPos = typeof(Lights.Lights).GetMethod("GetSunPos", Public | Static);

        if (getSunPos is not null)
            ModifySunPosition = new(getSunPos,
                RealSunPosition);
    }

    public override void Unload() => ModifySunPosition?.Dispose();

    #endregion

        // This gets a bit funky with RedSun as both then sun and moon can be visible but I'm hoping its not noticable.
    private Vector2 RealSunPosition(orig_GetSunPos orig, RenderTarget2D render)
    {
        Vector2 position = Main.dayTime ? SunPosition : MoonPosition;

            // I tend to use this over checking the players gravity direction, as its much safer.
        if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            position.Y = render.Height - position.Y;

        return position;
    }
}
