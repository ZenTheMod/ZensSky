using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RedSunAndRealisticSky;
using RedSunAndRealisticSky.Graphics;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Ambience;
using ZensSky.Common.Systems.Menu;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonRendering;
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

    private const float MinSunBrightness = .82f;
    private const float MinMoonBrightness = .65f;

    private static ILHook? PatchSunAndMoonDrawing;

    private static readonly bool SkipDrawing = SkyConfig.Instance.SunAndMoonRework;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    public static bool FlipSunAndMoon
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => ModContent.GetInstance<ClientConfig>().FlipSunAndMoon;
    }

    #endregion

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? changePositionAndDrawDayMoon = typeof(GeneralLightingIL).GetMethod(nameof(GeneralLightingIL.ChangePositionAndDrawDayMoon), NonPublic | Instance);

        MainThreadSystem.Enqueue(() =>
        {
            if (changePositionAndDrawDayMoon is not null)
                PatchSunAndMoonDrawing = new(changePositionAndDrawDayMoon,
                    ModifyDrawing);
        });
    }

    public override void Unload() =>
        MainThreadSystem.Enqueue(() => PatchSunAndMoonDrawing?.Dispose());

    public override void PostSetupContent() =>
        SkyColorSystem.ModifyInMenu += ModContent.GetInstance<GeneralLighting>().ModifySunLightColor;

    #endregion

    #region Drawing

    private void ModifyDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel sunSkipTarget = c.DefineLabel();
            ILLabel moonSkipTarget = c.DefineLabel();

            ILLabel? jumpSunOrMoonGrabbing = c.DefineLabel();

            c.EmitDelegate(() =>
            {
                if (!ZensSky.CanDrawSky ||
                !SkipDrawing ||
                (!ShowSun && !ShowMoon))
                    return;

                Draw();

                ShowSun = true;
                ShowMoon = true;
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

                // With the 'FancyLighting' mod enabled the game will attempt to render the moon here with the shader used for the sun.
            if (FancyLightingSystem.IsEnabled)
                c.EmitDelegate(() => { Main.pixelShader.CurrentTechnique.Passes[0].Apply(); });

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
                    i => i.MatchBrfalse(out _));
            else
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg3(),
                    i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(moonPosition));

            c.MarkLabel(moonSkipTarget);

            #endregion

            c.Index--;

                // Now actually grab the info.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.dayTime)),
                i => i.MatchBrfalse(out _));

            c.MoveAfterLabels();

            c.EmitLdloc(sunPosition);
            c.EmitLdloc(sunColor);
            c.EmitLdloc(sunRotation);
            c.EmitLdloc(sunScale);

            c.EmitLdloc(moonPosition);
            c.EmitLdarg(moonColor);
            c.EmitLdloc(moonRotation);
            c.EmitLdloc(moonScale);

            c.EmitLdcI4(0); // This info is not forced.

            c.EmitDelegate<Action<Vector2, Color, float, float, Vector2, Color, float, float, bool>>(SetInfo);

            #region Misc

                // Make the player unable to grab the sun while hovering the panel.
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.hasFocus)),
                i => i.MatchBrfalse(out jumpSunOrMoonGrabbing));

            c.EmitDelegate(() => MenuControllerSystem.Hovering && !Main.alreadyGrabbingSunOrMoon);

            c.EmitBrtrue(jumpSunOrMoonGrabbing);

            if (BetterNightSkySystem.IsEnabled)
            {
                c.GotoPrev(MoveType.After,
                    i => i.MatchLdsfld<Main>(nameof(Main.ForcedMinimumZoom)),
                    i => i.MatchMul(),
                    i => i.MatchStloc(moonScale));

                c.EmitLdloca(moonScale);

                c.EmitDelegate(BetterNightSkySystem.ModifyMoonScale);
            }

            #endregion
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private static void Draw()
    {
        SpriteBatch spriteBatch = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        
        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

        DrawSunAndMoon(spriteBatch, device, Main.dayTime && ShowSun, ShowMoon);

        spriteBatch.Restart(in snapshot);
    }

    #endregion
}
