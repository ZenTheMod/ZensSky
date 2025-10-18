using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core.Utils;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonHooks;

namespace ZensSky.Common.Systems.Sky;

public static class AtmosphereRendering
{
    #region Loading

    [OnLoad(Side = ModSide.Client)]
    public static void Load() =>
        PostDrawSunAndMoon += AtmospherePostDraw;

    #endregion

    #region Drawing

    private static void AtmospherePostDraw(SpriteBatch spriteBatch)
    {
        Texture2D gradient = SkyTextures.SkyGradient;

        Color color = GetColor();

        spriteBatch.Draw(gradient, Utilities.ScreenDimensions, color);
    }

    #endregion

    #region Private Methods

    private static Color GetColor() =>
        (SkyConfig.Instance.SkyGradient.GetColor(Utilities.TimeRatio) * Easings.InCubic(Main.atmo)) with { A = 0 };

    #endregion
}
