using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Registries;
using ZensSky.Common.Systems.Compat;
using ZensSky.Common.Utilities;
using Star = ZensSky.Common.DataStructures.Star;
using static ZensSky.Common.Systems.Stars.StarSystem;

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

    public override void Load() => Main.QueueMainThreadAction(() => On_Main.DrawStarsInBackground += DrawStarsInBackground);

    public override void Unload() => Main.QueueMainThreadAction(() => On_Main.DrawStarsInBackground -= DrawStarsInBackground);

    #endregion

    #region Drawing

    #region Stars

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
                texture = Textures.DiamondStar.Value;
                origin = texture.Size() * .5f;
                Array.ForEach(StarSystem.Stars, s => s.DrawDiamond(spriteBatch, texture, alpha, origin, -StarRotation));
                break;

            case StarVisual.FourPointed:
                texture = Textures.Star.Value;
                origin = texture.Size() * .5f;
                Array.ForEach(StarSystem.Stars, s => s.DrawFlare(spriteBatch, texture, alpha, origin, -StarRotation));
                break;

            case StarVisual.OuterWilds:
                texture = Textures.OuterWildsStar.Value;
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
                texture = Textures.DiamondStar.Value;
                origin = texture.Size() * .5f;
                star.DrawDiamond(spriteBatch, texture, alpha, origin, rotation);
                break;

            case StarVisual.FourPointed:
                texture = Textures.Star.Value;
                origin = texture.Size() * .5f;
                star.DrawFlare(spriteBatch, texture, alpha, origin, rotation);
                break;

            case StarVisual.OuterWilds:
                texture = Textures.OuterWildsStar.Value;
                origin = texture.Size() * .5f;
                star.DrawCircle(spriteBatch, texture, alpha, origin, rotation);
                break;
        }
    }

    #endregion

    #region Supernovae

    public static void DrawSupernovae(SpriteBatch spriteBatch, float alpha)
    {
        Effect supernova = Shaders.Supernova.Value;

        if (supernova is null)
            return;

            // supernova.CurrentTechnique.Passes[0].Apply();

            // Set all of the generic color info.
        supernova.Parameters["background"]?.SetValue(Background);

        supernova.Parameters["ringStartColor"]?.SetValue(RingStart);
        supernova.Parameters["ringEndColor"]?.SetValue(RingEnd);

        supernova.Parameters["globalTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.SetAtmosphereParams(supernova);

        Texture2D texture = Textures.SupernovaNoise.Value;

        Vector2 origin = texture.Size() * 0.5f;

        foreach (Star star in StarSystem.Stars.Where(s => s.SupernovaProgress == SupernovaProgress.Exploding))
        {
            float time = star.SupernovaTimer / star.BaseSize;
            Vector2 position = star.Position;

                // Multiply the Vector4 and not the Color to give values past 1.
            supernova.Parameters["startColor"]?.SetValue(star.BaseColor.ToVector4() * ExplosionStart);
            supernova.Parameters["endColor"]?.SetValue(star.BaseColor.ToVector4() * ExplosionEnd);

            supernova.Parameters["quickTime"]?.SetValue(MathF.Min(time * QuickTimeMultiplier, 1f));
            supernova.Parameters["expandTime"]?.SetValue(MathF.Min(time * ExpandTimeMultiplier, 1f));
            supernova.Parameters["ringTime"]?.SetValue(MathF.Min(time * RingTimeMultiplier, 1f));
            supernova.Parameters["longTime"]?.SetValue(time);

            supernova.Parameters["offset"]?.SetValue(position / MiscUtils.ScreenSize);

            supernova.CurrentTechnique.Passes[0].Apply();

            float opacity = alpha + (MinimumSupernovaAlpha / star.BaseSize);

            float rotation = star.Rotation;

            spriteBatch.Draw(texture, position, null, Color.White * opacity, rotation, origin, SupernovaScale * star.BaseSize, SpriteEffects.None, 0f);
        }
    }

    #endregion

    private void DrawStarsInBackground(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
    {
            // TODO: Better method of detecting when a mod uses custom sky to hide the visuals.
        if (!ZensSky.CanDrawSky || MacrocosmSystem.InAnySubworld)
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
        GraphicsDevice device = Main.instance.GraphicsDevice;

        UpdateStarAlpha();

        spriteBatch.End(out var snapshot);

        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.DrawStars();

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix * RotationMatrix());

        RealisticSkySystem.ApplyStarShader();

        if (alpha > 0)
            DrawStars(spriteBatch, alpha);

        if (StarSystem.Stars.Any(s => s.SupernovaProgress > SupernovaProgress.Shrinking))
            DrawSupernovae(spriteBatch, alpha);

        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.DrawGalaxy();

        if (BetterNightSkySystem.IsEnabled)
            BetterNightSkySystem.DrawSpecialStars(alpha);

        spriteBatch.Restart(in snapshot);
    }

    public static Matrix RotationMatrix()
    {
        Matrix rotation = Matrix.CreateRotationZ(StarRotation);
        Matrix offset = Matrix.CreateTranslation(new(MiscUtils.HalfScreenSize, 0f));

        return rotation * offset; // * Main.BackgroundViewMatrix.EffectMatrix;
    }

    #endregion
}
