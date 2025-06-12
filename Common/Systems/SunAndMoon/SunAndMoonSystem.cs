using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Compat;
using ZensSky.Common.Systems.MainMenu;

namespace ZensSky.Common.Systems.SunAndMoon;

[Autoload(Side = ModSide.Client)]
public sealed class SunAndMoonSystem : ModSystem
{
    #region Fields

    private const int SunMoonY = -80;

    private const float MinSunBrightness = 0.82f;
    private const float MinMoonBrightness = 0.35f;

        // These used to be the same for both the sun and moon, but due to other mods I've seperated them for ease of use.
    public static Vector2 SunPosition { get; private set; }
    public static Color SunColor { get; private set; }
    public static float SunRotation { get; private set; }
    public static float SunScale { get; private set; }

    public static Vector2 MoonPosition { get; private set; }
    public static Color MoonColor { get; private set; }
    public static float MoonRotation { get; private set; }
    public static float MoonScale { get; private set; }

    public static Vector2 SceneAreaSize { get; private set; }

    // This is fine because this is ONLY changed on load.
    private static readonly bool SkipDrawing = SkyConfig.Instance.SunAndMoonRework;

    #endregion

    public override void Load() => Main.QueueMainThreadAction(() => IL_Main.DrawSunAndMoon += ModifyDrawing);
    public override void Unload() => Main.QueueMainThreadAction(() => IL_Main.DrawSunAndMoon -= ModifyDrawing);

    private void ModifyDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel sunSkipTarget = c.DefineLabel();
            ILLabel moonSkipTarget = c.DefineLabel();

            ILLabel? jumpSunOrMoonGrabbing = c.DefineLabel();

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.bgTopY)));

            c.EmitPop();
            c.EmitLdcI4(SunMoonY);

            #region Sun

                // Force a constant brightness of the sun.
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

                // Store sunPosition before SceneLocalScreenPositionOffset is added to it, then jump over the rest.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(out sunPosition));

                // This is just to fetch the local IDs.
                    // These hooks can apply at varied times -- due to QueueMainThreadAction -- so I have to account for them with safer edits.
            c.FindNext(out _,
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdloc0(),
                i => i.MatchLdloc(sunPosition),
                i => i.MatchLdloca(out _),
                i => i.MatchInitobj<Rectangle?>(),
                i => i.MatchLdloc(out _),
                i => i.MatchLdloc(out sunColor),
                i => i.MatchLdloc(out sunRotation),
                i => i.MatchLdloc(out _),
                i => i.MatchLdloc(out sunScale),
                i => i.MatchLdcI4(0));

            c.EmitStloc(sunPosition);

            c.EmitBr(sunSkipTarget);

            if (SkipDrawing)
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdsfld<Main>(nameof(Main.dayTime)),
                    i => i.MatchBrtrue(out _),
                    i => i.MatchLdcR4(1f));
            else
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg1(),
                    i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(sunPosition));

            c.MarkLabel(sunSkipTarget);

            c.EmitLdarg1(); // SceneArea
            c.EmitLdloc(sunPosition); // Position
            c.EmitLdloc(sunColor); // Color
            c.EmitLdloc(sunRotation); // Rotation
            c.EmitLdloc(sunScale); // Scale

            c.EmitDelegate(FetchSunInfo);

            #endregion

            #region Moon

                // Force a constant brightness of the moon.
            int moonAlpha = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.atmo)),
                i => i.MatchMul(),
                i => i.MatchSub(),
                i => i.MatchStloc(out moonAlpha));

            c.EmitLdloca(moonAlpha);
            c.EmitDelegate((ref float mult) => { mult = MathF.Max(mult, MinMoonBrightness); });

            int moonPosition = -1;
            int moonRotation = -1;
            int moonScale = -1;

                // Store sunPosition before SceneLocalScreenPositionOffset is added to it, then jump over the rest.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(out moonPosition));

            c.EmitStloc(moonPosition);

            c.EmitBr(moonSkipTarget);

                // Fetch IDs from the Draw call.
            c.FindNext(out _, 
                i => i.MatchNewobj<Rectangle?>(),
                i => i.MatchLdarg2(),
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
                    i => i.MatchLdarg1(),
                    i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(moonPosition));
            
            c.MarkLabel(moonSkipTarget);

            c.EmitLdarg1(); // SceneArea
            c.EmitLdloc(moonPosition); // Position
            c.EmitLdarg2(); // Color
            c.EmitLdloc(moonRotation); // Rotation
            c.EmitLdloc(moonScale); // Scale

            c.EmitDelegate(FetchMoonInfo);

            #endregion

                // Make the player unable to grab the sun while hovering the panel.
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.hasFocus)),
                i => i.MatchBrfalse(out jumpSunOrMoonGrabbing));

            c.EmitDelegate(() => MenuControllerSystem.Hovering && !Main.alreadyGrabbingSunOrMoon);

            c.EmitBrtrue(jumpSunOrMoonGrabbing);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"Main.DrawSunAndMoon\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    public static void FetchSunInfo(Main.SceneArea sceneArea, Vector2 position, Color color, float rotation, float scale)
    {
        SunPosition = position;
        SunColor = color;
        SunRotation = rotation;
        SunScale = scale;

        SceneAreaSize = new(sceneArea.totalWidth, sceneArea.totalHeight);

        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.UpdateSunAndMoonPosition(position);
    }

    public static void FetchMoonInfo(Main.SceneArea sceneArea, Vector2 position, Color color, float rotation, float scale)
    {
        MoonPosition = position;
        MoonColor = color;
        MoonRotation = rotation;
        MoonScale = scale;

        SceneAreaSize = new(sceneArea.totalWidth, sceneArea.totalHeight);
    }
}
