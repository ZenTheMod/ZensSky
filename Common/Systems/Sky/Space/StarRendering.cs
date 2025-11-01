using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core;
using ZensSky.Core.ModCall;
using ZensSky.Core.Utils;
using static ZensSky.Common.Systems.Sky.Space.StarHooks;
using static ZensSky.Common.Systems.Sky.Space.StarSystem;
using Star = ZensSky.Common.DataStructures.Star;

namespace ZensSky.Common.Systems.Sky.Space;

public static class StarRendering
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

    [OnLoad(Side = ModSide.Client)]
    public static void Load() => 
        MainThreadSystem.Enqueue(() => On_Main.DrawStarsInBackground += DrawStarsInBackground);

    [OnUnload(Side = ModSide.Client)]
    public static void Unload() => 
        MainThreadSystem.Enqueue(() => On_Main.DrawStarsInBackground -= DrawStarsInBackground);

    #endregion

    #region Drawing

    #region Stars

    [ModCall("DrawAllStars")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawStars(SpriteBatch spriteBatch, float alpha) =>
        DrawStars(spriteBatch, alpha, -StarRotation, Stars, SkyConfig.Instance.StarStyle);

    public static void DrawStars(SpriteBatch spriteBatch, float alpha, float rotation, Star[] stars, StarVisual style)
    {
        Texture2D texture;

        Vector2 origin;

        ReadOnlySpan<Star> activeStars = [.. stars.Where(s => s.IsActive)];

        switch (style)
        {
            case StarVisual.Vanilla:
                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawVanilla(spriteBatch, alpha);
                return;

            case StarVisual.Diamond:
                texture = StarTextures.DiamondStar;
                origin = texture.Size() * .5f;

                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawDiamond(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.FourPointed:
                texture = StarTextures.FourPointedStar;
                origin = texture.Size() * .5f;

                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawFlare(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.OuterWilds:
                texture = StarTextures.CircleStar;
                origin = texture.Size() * .5f;

                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawCircle(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.Random:
                for (int i = 0; i < stars.Length; i++)
                    if (stars[i].IsActive)
                        DrawStar(spriteBatch, alpha, rotation, stars[i], (StarVisual)(i % 3 + 1));
                return;
        }
    }

    public static void DrawStar(SpriteBatch spriteBatch, float alpha, float rotation, Star star, StarVisual style)
    {
        if (!star.IsActive)
            return;

        Texture2D texture;
        Vector2 origin;

        switch (style)
        {
            case StarVisual.Vanilla:
                star.DrawVanilla(spriteBatch, alpha);
                return;

            case StarVisual.Diamond:
                texture = StarTextures.DiamondStar;
                origin = texture.Size() * .5f;

                star.DrawDiamond(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.FourPointed:
                texture = StarTextures.FourPointedStar;
                origin = texture.Size() * .5f;

                star.DrawFlare(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.OuterWilds:
                texture = StarTextures.CircleStar;
                origin = texture.Size() * .5f;

                star.DrawCircle(spriteBatch, texture, alpha, origin, rotation);
                return;
        }
    }

    #endregion

    private static void DrawStarsInBackground(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
    {
            // TODO: Better method of detecting when a mod uses custom sky to hide the visuals.
        if (!ZensSky.CanDrawSky ||
            MacrocosmSystem.IsEnabled && MacrocosmSystem.InAnySubworld ||
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

        spriteBatch.End();

        if (InvokePreDrawStars(spriteBatch, in snapshot, ref alpha, ref transform))
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, snapshot.DepthStencilState, snapshot.RasterizerState, RealisticSkySystem.ApplyStarShader(), transform);

            if (alpha > 0)
                DrawStars(spriteBatch, alpha);

            spriteBatch.End();
        }

        InvokePostDrawStars(spriteBatch, in snapshot, alpha, transform);

        spriteBatch.Begin(in snapshot);
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
