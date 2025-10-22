using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Menu;
using ZensSky.Core;
using ZensSky.Core.Exceptions;
using ZensSky.Core.ModCall;
using ZensSky.Core.Utils;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonHooks;

namespace ZensSky.Common.Systems.Sky.SunAndMoon;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="ModifyDrawing"/><br/>
///         Injects vanilla drawing to capture a <see cref="SunAndMoonInfo"/> from the sun/moon.
///     </item>
/// </list>
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SunAndMoonSystem : ModSystem
{
    #region Private Fields

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private const float SunNoonAlpha = .082f;

    private static readonly Color SunMultiplier = new(255, 245, 225);

    private static readonly Color MoonMultiplier = new(50, 50, 55);

    private const int SunMoonY = -80;

    private const float MinSunBrightness = .82f;
    private const float MinMoonBrightness = .65f;

    private static readonly bool UseSunAndMoon = SkyConfig.Instance.UseSunAndMoon;

    #endregion

    #region Public Properties

    public static bool ForceInfo { get; set; }

    public static bool ShowSun
    {
        [ModCall(nameof(ShowSun), $"Get{nameof(ShowSun)}")]
        get;
        [ModCall($"Set{nameof(ShowSun)}")]
        set; 
    } = true;

    public static bool ShowMoon
    {
        [ModCall(nameof(ShowMoon), $"Get{nameof(ShowMoon)}n")]
        get;
        [ModCall($"Set{nameof(ShowMoon)}")]
        set;
    } = true;

    public static SunAndMoonInfo Info { get; private set; }

    /// <summary>
    /// The high-resolution asset to use for moon drawing.
    /// </summary>
    public static Asset<Texture2D> MoonTexture
    {
        get =>
            UseSunAndMoon ? field : GetBaseMoonTexture();
        set;
    }
        = Asset<Texture2D>.Empty;

    /// <summary>
    /// If any changing events are active.
    /// </summary>
    public static bool EventMoon =>
        WorldGen.drunkWorldGen || Main.pumpkinMoon || Main.snowMoon;

    #endregion

    #region Loading

    public override void Load() =>
        MainThreadSystem.Enqueue(() => IL_Main.DrawSunAndMoon += ModifyDrawing);

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
            IL_Main.DrawSunAndMoon -= ModifyDrawing);

        Clear();
    }

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

            c.EmitDelegate((ref float mult) =>
                { mult = MathF.Max(mult, MinSunBrightness); });

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

            if (UseSunAndMoon)
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdsfld<Main>(nameof(Main.dayTime)),
                    i => i.MatchBrtrue(out _));
            else
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg1(),
                    i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
                    i => i.MatchCall<Vector2>("op_Addition"),
                    i => i.MatchStloc(sunPosition));

            c.MarkLabel(sunSkipTarget);

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

            c.EmitDelegate((ref float mult) =>
                { mult = MathF.Max(mult, MinMoonBrightness); });

            int moonPosition = -1;
            int moonColor = -1;
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
                i => i.MatchLdarg(out moonColor),
                i => i.MatchLdloc(out moonRotation));
            c.FindNext(out _, 
                i => i.MatchDiv(),
                i => i.MatchConvR4(),
                i => i.MatchNewobj<Vector2>(),
                i => i.MatchLdloc(out moonScale));

            if (UseSunAndMoon)
                c.GotoNext(MoveType.Before,
                    i => i.MatchLdsfld<Main>(nameof(Main.dayTime)),
                    i => i.MatchBrfalse(out _));
            else
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg1(),
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

            c.EmitCall<Action<Vector2, Color, float, float, Vector2, Color, float, float, bool>>(SetInfo);

            #region Misc

                // Make the player unable to grab the sun while hovering the menu controller panel.
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.hasFocus)),
                i => i.MatchBrfalse(out jumpSunOrMoonGrabbing));

            c.EmitDelegate(() =>
                MenuControllerSystem.Hovering && !Main.alreadyGrabbingSunOrMoon);

            c.EmitBrtrue(jumpSunOrMoonGrabbing);

            #endregion
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Public Methods

    [ModCall]
    public static Asset<Texture2D> GetBaseMoonTexture()
    {
        Asset<Texture2D> ret = SkyTextures.Moon[Math.Min(Main.moonType, SkyTextures.Moon.Length - 1)];

        if (ExtraMoonStyles.TryGetValue(Main.moonType,
            out Asset<Texture2D>? style))
            ret = style;

        if (Main.pumpkinMoon)
            ret = SkyTextures.MoonPumpkin;
        else if (Main.snowMoon)
            ret = SkyTextures.MoonSnow;

        InvokeModifyMoonTexture(ref ret, EventMoon);

        return ret;
    }

    public static Color GetLightColor(bool day)
    {
        Vector2 position = day ? Info.SunPosition : Info.MoonPosition;
        float centerX = Utilities.HalfScreenSize.X;

        float distanceFromCenter = MathF.Abs(centerX - position.X) / centerX;

        Color color = day ? Info.SunColor : Info.MoonColor;
        color = color.MultiplyRGB(day ? SunMultiplier : MoonMultiplier);

            // Add a fadeinout effect so the color doesnt just suddenly pop up.
        color *= Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);

            // Decrease the intensity at noon to make the clouds not just be pure white.
            // And alter the intensity depending on the moon phase, where a new moon would cast no light.
        if (day)
            color *= MathHelper.Lerp(SunNoonAlpha, 1f, Easings.InQuart(distanceFromCenter));
        else
            color *= MathF.Abs(4 - Main.moonPhase) * .25f;

        color.A = 255;

        return color;
    }

    /// <summary>
    /// Updates sun and moon positions as well as updating other mod's values.
    /// </summary>
    /// <param name="forced">If the info provided should be prioritized over the vanilla data.</param>
    [ModCall(false, "SetSunAndMoonInfo")]
    public static void SetInfo(Vector2 sunPosition, Color sunColor, float sunRotation, float sunScale,
        Vector2 moonPosition, Color moonColor, float moonRotation, float moonScale, bool forced = false)
    {
        ForceInfo |= forced;

        if (ForceInfo == forced)
            Info = new(sunPosition, sunColor, sunRotation, sunScale,
                moonPosition, moonColor, moonRotation, moonScale);

        InvokeOnUpdateSunAndMoonInfo(Info);
    }

    /// <inheritdoc cref="SetInfo(Vector2, Color, float, float, Vector2, Color, float, float, bool)"/>
    [ModCall(false, "SetSunAndMoonInfo")]
    public static void SetInfo(Vector2 position, Color color, float rotation, float scale, bool forced = false) =>
        SetInfo(position, color, rotation, scale, 
            position, color, rotation, scale, forced);

    #endregion
}
