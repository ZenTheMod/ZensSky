using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Systems;
using ZensSky.Core.Systems.ModCall;
using ZensSky.Core.Utils;
using static ZensSky.Common.Systems.Stars.StarHooks;
using static ZensSky.Common.Systems.Stars.StarSystem;
using Star = ZensSky.Common.DataStructures.Star;

namespace ZensSky.Common.Systems.Stars;

[Autoload(Side = ModSide.Client)]
public sealed class StarRenderingSystem : ModSystem
{
    #region Private Fields

    private static readonly Vector4 ExplosionStart = new(1.5f, 2.5f, 4f, 1f);
    private static readonly Vector4 ExplosionEnd = new(1.4f, .25f, 2.2f, .7f);
    private static readonly Vector4 RingStart = new(3.5f, 2.9f, 1f, 1f);
    private static readonly Vector4 RingEnd = new(4.5f, 1.8f, .5f, .5f);

    private static readonly Vector4 Background = new(0, 0, 0, 0);

    private const float QuickTimeMultiplier = 20f;
    private const float ExpandTimeMultiplier = 13.3f;
    private const float RingTimeMultiplier = 6.6f;

    private const float MinimumSupernovaAlpha = 0.6f;

    private const float SupernovaScale = 0.27f;

    #endregion

    #region Loading

    public override void Load() => 
        MainThreadSystem.Enqueue(() => On_Main.DrawStarsInBackground += DrawStarsInBackground);

    public override void Unload() => 
        MainThreadSystem.Enqueue(() => On_Main.DrawStarsInBackground -= DrawStarsInBackground);

    #endregion

    #region Drawing

    #region Stars

    [ModCall("DrawAllStars")]
    public static void DrawStars(SpriteBatch spriteBatch, float alpha)
    {
        Texture2D texture;
        Vector2 origin;
        switch (SkyConfig.Instance.StarStyle)
        {
            case StarVisual.Vanilla:
                Array.ForEach(StarSystem.Stars, s => s.DrawVanilla(spriteBatch, alpha));
                break;

            case StarVisual.Diamond:
                texture = StarTextures.DiamondStar;
                origin = texture.Size() * .5f;
                Array.ForEach(StarSystem.Stars, s => s.DrawDiamond(spriteBatch, texture, alpha, origin, -StarRotation));
                break;

            case StarVisual.FourPointed:
                texture = StarTextures.FourPointedStar;
                origin = texture.Size() * .5f;
                Array.ForEach(StarSystem.Stars, s => s.DrawFlare(spriteBatch, texture, alpha, origin, -StarRotation));
                break;

            case StarVisual.OuterWilds:
                texture = StarTextures.CircleStar;
                origin = texture.Size() * .5f;
                Array.ForEach(StarSystem.Stars, s => s.DrawCircle(spriteBatch, texture, alpha, origin, -StarRotation));
                break;

                // TODO: Clean up this logic.
            case StarVisual.Random:
                for (int i = 0; i < StarCount; i++)
                {
                    Star star = StarSystem.Stars[i];

                    int style = (i % 3) + 1;

                    DrawStar(spriteBatch, alpha, -StarRotation, star, (StarVisual)style);
                }
                break;
        }
    }

    [ModCall("DrawSingleStar")]
    public static void DrawStar(SpriteBatch spriteBatch, float alpha, float rotation, Star star, StarVisual style)
    {
        Texture2D texture;
        Vector2 origin;

        switch (style)
        {
            case StarVisual.Vanilla:
                star.DrawVanilla(spriteBatch, alpha);
                break;

            case StarVisual.Diamond:
                texture = StarTextures.DiamondStar;
                origin = texture.Size() * .5f;
                star.DrawDiamond(spriteBatch, texture, alpha, origin, rotation);
                break;

            case StarVisual.FourPointed:
                texture = StarTextures.FourPointedStar;
                origin = texture.Size() * .5f;
                star.DrawFlare(spriteBatch, texture, alpha, origin, rotation);
                break;

            case StarVisual.OuterWilds:
                texture = StarTextures.CircleStar;
                origin = texture.Size() * .5f;
                star.DrawCircle(spriteBatch, texture, alpha, origin, rotation);
                break;
        }
    }

    #endregion

    private void DrawStarsInBackground(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
    {
            // TODO: Better method of detecting when a mod uses custom sky to hide the visuals.
        if (!ZensSky.CanDrawSky ||
            (MacrocosmSystem.IsEnabled && MacrocosmSystem.InAnySubworld) ||
            artificial)
        {
            orig(self, sceneArea, artificial);
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;

        float alpha = StarAlpha;

        DrawStarsToSky(spriteBatch, alpha);
    }

    #endregion

    #region Public Methods

    public static void DrawStarsToSky(SpriteBatch spriteBatch, float alpha)
    {
        UpdateStarAlpha();

        SpriteBatchSnapshot snapshot = new(spriteBatch);

        Matrix transform = RotationMatrix() * snapshot.TransformMatrix;

        if (InvokePreDrawStars(spriteBatch, ref alpha, ref transform))
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, RealisticSkySystem.ApplyStarShader(), transform);

            if (alpha > 0)
                DrawStars(spriteBatch, alpha);

            spriteBatch.Restart(in snapshot);
        }

        InvokePostDrawStars(spriteBatch, alpha, transform);
    }


    [ModCall(false, "StarRotationMatrix", "GetStarRotationMatrix", "StarDrawMatrix", "StarTransform")]
    public static Matrix RotationMatrix()
    {
        Matrix rotation = Matrix.CreateRotationZ(StarRotation);
        Matrix offset = Matrix.CreateTranslation(new(Utilities.HalfScreenSize, 0f));

        return rotation * offset;
    }

    #endregion
}
