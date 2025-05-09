using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Utilities;
using Daybreak.Common.Rendering;

namespace ZensSky.Common.Systems.SunAndMoon;

public sealed class SunAndMoonRenderingSystem : ModSystem
{
    private static SunAndMoonTargetContent? _sunAndMoonRenderTarget;

    public static SunAndMoonTargetContent? SunAndMoonRenderTarget => _sunAndMoonRenderTarget;

    #region Loading

    public override void Load()
    {
        On_Main.DrawSunAndMoon += DrawSunAndMoonTarget;
        _sunAndMoonRenderTarget = new();
        Main.ContentThatNeedsRenderTargets.Add(_sunAndMoonRenderTarget);
    }

    public override void Unload()
    {
        On_Main.DrawSunAndMoon -= DrawSunAndMoonTarget;

        if (_sunAndMoonRenderTarget is not null)
        {
            Main.ContentThatNeedsRenderTargets.Remove(_sunAndMoonRenderTarget);
            _sunAndMoonRenderTarget = null;
        }
    }

    #endregion

    #region Drawing

    private static void DrawWithoutRenderTargets()
    {
        SpriteBatch spriteBatch = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

        SunAndMoonTargetContent.DrawSunAndMoon(spriteBatch, device);

        spriteBatch.Restart(in snapshot);
    }

    private void DrawSunAndMoonTarget(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
    {
        if (StarSystem.CanDrawStars)
        {
            if (SkyConfig.Instance.MinimizeRenderTargetUsage)
                DrawWithoutRenderTargets();
            else if (_sunAndMoonRenderTarget is not null)
                Main.spriteBatch.RequestAndDrawRenderTarget(_sunAndMoonRenderTarget, MiscUtils.ScreenDimensions);
        }

        orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
    }

    #endregion
}
