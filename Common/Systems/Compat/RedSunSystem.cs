using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RedSunAndRealisticSky.Graphics;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Utilities;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonRenderingSystem;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("RedSunAndRealisticSky")]
[ExtendsFromMod("RedSunAndRealisticSky")]
[Autoload(Side = ModSide.Client)]
public sealed class RedSunSystem : ModSystem
{
    #region Private Fields

    private static readonly Color SkyColor = new(128, 168, 248);

    private const int SunTopBuffer = 50;

    private const int SunMoonY = -80;

    private const float MoonBrightness = 16f;

    private const float MinSunBrightness = 0.82f;
    private const float MinMoonBrightness = 0.35f;

    private static ILHook? SunAndMoonDrawing;

    private static readonly bool SkipDrawing = SkyConfig.Instance.SunAndMoonRework;

    #endregion

    public static bool IsEnabled { get; private set; }

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? changePositionAndDrawDayMoon = typeof(GeneralLightingIL).GetMethod("ChangePositionAndDrawDayMoon", NonPublic | Instance);

        if (changePositionAndDrawDayMoon is not null)
            SunAndMoonDrawing = new(changePositionAndDrawDayMoon,
                ModifyDrawing);
    }

    public override void Unload() => SunAndMoonDrawing?.Dispose();

    #endregion

    private void ModifyDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            ILLabel sunSkipTarget = c.DefineLabel();
            ILLabel moonSkipTarget = c.DefineLabel();

            c.EmitDelegate(() =>
            {
                if (!StarSystem.CanDrawStars || !SkipDrawing)
                    return;

                Draw();
            });

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg3(),
                i => i.MatchLdfld<Main.SceneArea>("bgTopY"));

            c.EmitPop();
            c.EmitLdcI4(SunMoonY);

            #region Sun

            int sunAlpha = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.atmo)),
                i => i.MatchMul(),
                i => i.MatchSub(),
                i => i.MatchStloc(out sunAlpha));

            c.EmitLdloca(sunAlpha);
            c.EmitDelegate((ref float mult) => { mult = MathF.Max(mult, MinSunBrightness); });

            int sunPosition = -1;
            int sunColor = -1;
            int sunRotation = -1;
            int sunScale = -1;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg3(),
                i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(out sunPosition));

            c.EmitStloc(sunPosition);
            c.EmitBr(sunSkipTarget);

                // This is just to find the index of various locals.
            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(sunPosition),
                i => i.MatchLdloca(out _),
                i => i.MatchInitobj<Rectangle?>(),
                i => i.MatchLdloc(out _),
                i => i.MatchLdloc(out sunColor),
                i => i.MatchLdloc(out sunRotation),
                i => i.MatchLdloc(out _),
                i => i.MatchLdloc(out sunScale));

            if (SkipDrawing)
                c.GotoNext(MoveType.After,
                    i => i.MatchLdloc(sunPosition),
                    i => i.MatchLdloca(out _),
                    i => i.MatchInitobj<Rectangle?>(),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdloc(sunRotation),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdloc(sunScale),
                    i => i.MatchLdcI4(0),
                    i => i.MatchLdcR4(0f),
                    i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.Draw)));
            else
                c.GotoPrev(MoveType.After,
                    i => i.MatchLdarg3(),
                    i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(out sunPosition));

            c.MarkLabel(sunSkipTarget);

            c.EmitLdarg3(); // SceneArea
            c.EmitLdloc(sunPosition); // Position
            c.EmitLdloc(sunColor); // Color
            c.EmitLdloc(sunRotation); // Rotation
            c.EmitLdloc(sunScale); // Scale

            c.EmitDelegate(FetchSunInfo);

            #endregion

            #region Moon

            int moonAlpha = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.atmo)),
                i => i.MatchMul(),
                i => i.MatchSub(),
                i => i.MatchStloc(out moonAlpha));

            c.EmitLdloca(moonAlpha);
            c.EmitDelegate((ref float mult) => { mult = MathF.Max(mult, MinMoonBrightness); });

            int moonPosition = -1;
            int moonColor = -1;
            int moonRotation = -1;
            int moonScale = -1;

                // Store sunPosition before SceneLocalScreenPositionOffset is added to it, then jump over the rest.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg3(),
                i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(out moonPosition));

            c.EmitStloc(moonPosition);

            c.EmitBr(moonSkipTarget);

                // Fetch IDs from the Draw call.
            c.FindNext(out _,
                i => i.MatchNewobj<Rectangle>(),
                i => i.MatchNewobj<Rectangle?>(),
                i => i.MatchLdarg(out moonColor),
                i => i.MatchLdloc(out moonRotation));
            c.FindNext(out _,
                i => i.MatchDiv(),
                i => i.MatchConvR4(),
                i => i.MatchNewobj<Vector2>(),
                i => i.MatchLdloc(out moonScale));

            if (SkipDrawing)
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdsfld<Main>(nameof(Main.dayTime)),
                    i => i.MatchBrfalse(out _),
                    i => i.MatchLdloc(out _));
            else
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg3(),
                    i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(moonPosition));

            c.MarkLabel(moonSkipTarget);

            c.EmitLdarg3(); // SceneArea
            c.EmitLdloc(moonPosition); // Position
            c.EmitLdarg(moonColor); // Color
            c.EmitLdloc(moonRotation); // Rotation
            c.EmitLdloc(moonScale); // Scale

            c.EmitDelegate(FetchMoonInfo);

            #endregion
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"GeneralLightingIL.ChangePositionAndDrawDayMoon\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    private static void Draw()
    {
        SpriteBatch spriteBatch = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

        float centerX = MiscUtils.HalfScreenSize.X;
        float distanceFromCenter = MathF.Abs(centerX - SunPosition.X) / centerX;

        float distanceFromTop = (SunPosition.Y + SunTopBuffer) / SceneAreaSize.Y;

        Color skyColor = Main.ColorOfTheSkies.MultiplyRGB(SkyColor);

        Color moonShadowColor = SkyConfig.Instance.TransparentMoonShadow ? Color.Transparent : skyColor;
        Color moonColor = MoonColor * MoonBrightness * MoonScale;
        moonColor.A = 255;

        if (Main.dayTime)
            DrawSun(spriteBatch, SunPosition, SunColor, SunRotation, SunScale, distanceFromCenter, distanceFromTop, device);

        // Draw the moon regardless due to this mod.
        DrawMoon(spriteBatch, MoonPosition, MoonColor, MoonRotation, MoonScale, moonColor, moonShadowColor, device);

        spriteBatch.Restart(in snapshot);
    }
}
