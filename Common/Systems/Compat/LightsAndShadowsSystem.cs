using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Compat;

    // There are a few issues I would like to fix; (e.g. the effect not showing on the titlescreen)
        // But all of them could be solved by using a Filter type instead of a detour of FilterManager.EndCapture.
[JITWhenModsEnabled("Lights")]
[ExtendsFromMod("Lights")]
[Autoload(Side = ModSide.Client)]
public sealed class LightsAndShadowsSystem : ModSystem
{
    #region Private Fields

    private delegate Vector2 orig_GetSunPos(RenderTarget2D render);
    private static Hook? PatchSunPosition;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // QueueMainThreadAction can be ignored as this mod is loaded first regardless.
    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? getSunPos = typeof(Lights.Lights).GetMethod(nameof(Lights.Lights.GetSunPos), Public | Static);

        if (getSunPos is not null)
            PatchSunPosition = new(getSunPos,
                SetPosition);
    }

    public override void Unload() => 
        PatchSunPosition?.Dispose();

    #endregion

        // This gets a bit funky with RedSun as both then sun and moon can be visible but I'm hoping its not noticable.
    private Vector2 SetPosition(orig_GetSunPos orig, RenderTarget2D render)
    {
        Vector2 position = Main.dayTime ? Info.SunPosition : Info.MoonPosition;

            // I tend to use this over checking the players gravity direction, as its much safer.
        if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            position.Y = render.Height - position.Y;

        return position;
    }
}
