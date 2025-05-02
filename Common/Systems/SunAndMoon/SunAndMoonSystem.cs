using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.SunAndMoon;

public sealed class SunAndMoonSystem : ModSystem
{
    #region Fields

    public static Vector2 SunMoonPosition { get; private set; }
    public static Color SunMoonColor { get; private set; }
    public static float SunMoonRotation { get; private set; }
    public static float SunMoonScale { get; private set; }
    public static Vector2 SceneAreaSize { get; private set; }

        // This is fine because this is ONLY changed on load.
    private static readonly bool SkipDrawing = SkyConfig.Instance.SunAndMoonRework;

    #endregion

    public override void Load() => IL_Main.DrawSunAndMoon += ModifyDrawing;
    public override void Unload() => IL_Main.DrawSunAndMoon -= ModifyDrawing;

    private void ModifyDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            ILLabel sunSkipTarget = c.DefineLabel();
            ILLabel moonSkipTarget = c.DefineLabel();

            #region Sun

            c.GotoNext(MoveType.After,
                i => i.MatchNewobj<Vector2>(),
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(22));

            if (SkipDrawing)
                c.EmitBr(sunSkipTarget);

            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>("dayTime"),
                i => i.MatchBrtrue(out _),
                i => i.MatchLdcR4(1f));

            if (SkipDrawing)
                c.MarkLabel(sunSkipTarget);

            c.EmitLdarg1(); // SceneArea
            c.EmitLdloc(22); // Position
            c.EmitLdloc(18); // Color
            c.EmitLdloc(7); // Rotation
            c.EmitLdloc(6); // Scale

            c.EmitDelegate(FetchInfo);

            #endregion

            #region Moon

            c.GotoNext(MoveType.After,
                i => i.MatchNewobj<Vector2>(),
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(25));

            if (SkipDrawing)
                c.EmitBr(moonSkipTarget);

            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>("dayTime"),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdloc(4));

            if (SkipDrawing)
                c.MarkLabel(moonSkipTarget);

            c.EmitLdarg1(); // SceneArea
            c.EmitLdloc(25); // Position
            c.EmitLdarg2(); // Color
            c.EmitLdloc(11); // Rotation
            c.EmitLdloc(10); // Scale

            c.EmitDelegate(FetchInfo);

            #endregion
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"Main.DrawSunAndMoon\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    private void FetchInfo(Main.SceneArea sceneArea, Vector2 position, Color color, float rotation, float scale)
    {
        SunMoonPosition = position;
        SunMoonColor = color;
        SunMoonRotation = rotation;
        SunMoonScale = scale;

        SceneAreaSize = new(sceneArea.totalWidth, sceneArea.totalHeight);
    }
}
