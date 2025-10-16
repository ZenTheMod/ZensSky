using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Compat;

/// <summary>
/// Lights and Shadows has many issues relating to how they handle their infamous screen shader;<br/>
/// you can notice this yourself by simply changing 'Wave Quality' in video settings to 'Off,'<br/>
/// this will disable the wave screen shader used and — assuming no other screen shaders are active — completely hide the effect.<br/>
/// The above happens due to the mod applying their shader via a hook of <see cref="FilterManager.EndCapture"/>,<br/>
/// instead of using a proper <see cref="Filter"/>.<br/><br/>
/// 
/// TODO: PR a fix to Lights and Shadows proper, as I don't feel like rewriting an entire mod for one mediocre visual 'overhaul.'<br/><br/>
/// 
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="SetPosition"/><br/>
///         Makes the Lights and Shadows™ Godrays™ effect use an accurate sun/moon position.
///     </item>
/// </list>
/// </summary>
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

        // This gets a bit funky with RedSun as both then sun and moon can be visible but I'm hoping its not noticable.
    private Vector2 SetPosition(orig_GetSunPos orig, RenderTarget2D render)
    {
        Vector2 position = Main.dayTime ? Info.SunPosition : Info.MoonPosition;

            // I tend to use this over checking the players gravity direction, as its much safer.
        if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            position.Y = render.Height - position.Y;

        return position;
    }

    #endregion
}
