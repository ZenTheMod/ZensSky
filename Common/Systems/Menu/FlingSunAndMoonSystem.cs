using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Utils;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonHooks;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Menu;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="FlingSunAndMoonPostDraw"/><br/>
///         Self-explanitory.
///     </item>
/// </list>
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class FlingSunAndMoonSystem : ModSystem
{
    #region Private Fields

    private static readonly Vector2 VelocityMultiplier = new(.92f, .85f);
    private const float SunMoonModMultiplier = .976f;

    private static Vector2 SunMoonOldPosition;

    private static Vector2 SunMoonVelocity;

    #endregion

    #region Loading

    public override void Load() =>
        PostDrawSunAndMoon += FlingSunAndMoonPostDraw;

    #endregion

    #region Flinging

    private void FlingSunAndMoonPostDraw(SpriteBatch spriteBatch)
    {
        if (!Main.gameMenu || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        Vector2 position = Main.dayTime ? Info.SunPosition : Info.MoonPosition;

        float sunMoonWidth =
            Main.dayTime ?
            TextureAssets.Sun.Value.Width :
            TextureAssets.Moon[Main.moonType].Value.Width;

        double timeLength =
            Main.dayTime ?
            Main.dayLength :
            Main.nightLength;

        if (Main.alreadyGrabbingSunOrMoon)
        {
            SunMoonVelocity = position - SunMoonOldPosition;

            SunMoonOldPosition = position;

            return;
        }

        SunMoonVelocity *= VelocityMultiplier;

        if (Main.dayTime)
            Main.sunModY += (short)SunMoonVelocity.Y;
        else
            Main.moonModY += (short)SunMoonVelocity.Y;

        Main.sunModY = (short)(Main.sunModY * SunMoonModMultiplier);
        Main.moonModY = (short)(Main.moonModY * SunMoonModMultiplier);

        Main.time = timeLength *
            ((position.X + SunMoonVelocity.X + sunMoonWidth) / (Utilities.ScreenSize.X + (sunMoonWidth * 2f)));

            // Account for RedSun's reversal of the sun's orbit.
        if (!RedSunSystem.IsEnabled || !RedSunSystem.FlipSunAndMoon)
            return;

        Main.time = timeLength - Main.time;
    }

    #endregion
}
