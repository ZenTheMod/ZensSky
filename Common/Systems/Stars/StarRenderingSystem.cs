using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.Systems.Stars;

public sealed class StarRenderingSystem : ModSystem
{
    private static StarTargetContent? _starRenderTarget;

    public static StarTargetContent? StarRenderTarget => _starRenderTarget;

    #region Loading

    public override void Load()
    {
        On_Main.DrawStarsInBackground += DrawTargetToSky;
        _starRenderTarget = new();
        Main.ContentThatNeedsRenderTargets.Add(_starRenderTarget);
    }

    public override void Unload()
    {
        On_Main.DrawStarsInBackground -= DrawTargetToSky;
        if (_starRenderTarget is not null)
        {
            Main.ContentThatNeedsRenderTargets.Remove(_starRenderTarget);
            _starRenderTarget = null;
        }
    }

    #endregion

    #region Drawing

    private static void DrawWithoutRenderTargets(SpriteBatch spriteBatch, GraphicsDevice device, Vector2 screenCenter, float alpha)
    {
        if (alpha > 0)
            StarTargetContent.DrawStars(spriteBatch, screenCenter, alpha);

            // if (RealisticSkyCompatSystem.RealisticSkyEnabled)
            // {
            //     DrawRealisticGalaxy(Helper.ScreenSize * GalaxyScaleMultiplier, Helper.ScreenSize.X);
            // 
            //     Matrix backgroundMatrix = GenerateGarbageMatrixForRealisticStars();
            // 
            //     Vector2 sunPosition = SunAndMoonSystem.SunMoonPosition;
            //     float falloff = Main.eclipse ? EclipseFalloff : NormalFalloff;
            // 
            //     DrawRealisticStars(device, alpha * RealisticStarAlphaMultiplier, Helper.ScreenSize, sunPosition, backgroundMatrix, Main.GlobalTimeWrappedHourly, falloff, false);
            // }
    }

    private void DrawTargetToSky(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
    {
        if (!StarSystem.CanDrawStars || Main.starGame)
        {
            orig(self, sceneArea, artificial);
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;

        if (SkyConfig.Instance.MinimizeRenderTargetUsage)
            DrawWithoutRenderTargets(spriteBatch, device, MiscUtils.HalfScreenSize, StarSystem.StarAlpha);
        else if (_starRenderTarget is not null)
            spriteBatch.RequestAndDrawRenderTarget(_starRenderTarget);
    }

    #endregion
}
