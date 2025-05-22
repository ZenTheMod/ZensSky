using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.SunAndMoon;

[Autoload(Side = ModSide.Client)]
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

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>("bgTopY"));

            c.EmitPop();
            c.EmitLdcI4(-85);

            #region Sun

            int val6 = -1;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(out val6));

            c.EmitStloc(val6);

            c.EmitBr(sunSkipTarget);

            if (SkipDrawing)
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdsfld<Main>("dayTime"),
                    i => i.MatchBrtrue(out _),
                    i => i.MatchLdcR4(1f));
            else
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg1(),
                    i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(val6));

            c.MarkLabel(sunSkipTarget);

            c.EmitLdarg1(); // SceneArea
            c.EmitLdloc(val6); // Position
            c.EmitLdloc(18); // Color
            c.EmitLdloc(7); // Rotation
            c.EmitLdloc(6); // Scale

            c.EmitDelegate(FetchInfo);

            #endregion

            #region Moon

            int val7 = -1;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg1(),
                i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                i => i.MatchCall<Vector2>("op_Addition"),
                i => i.MatchStloc(out val7));

            c.EmitStloc(val7);

            c.EmitBr(moonSkipTarget);

            if (SkipDrawing)
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdsfld<Main>("dayTime"),
                    i => i.MatchBrfalse(out _),
                    i => i.MatchLdloc(4));
            else
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg1(),
                    i => i.MatchLdfld<Main.SceneArea>("SceneLocalScreenPositionOffset"),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(val7));
            
            c.MarkLabel(moonSkipTarget);

            c.EmitLdarg1(); // SceneArea
            c.EmitLdloc(val7); // Position
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
