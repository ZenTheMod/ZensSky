using BetterNightSky;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Systems.SunAndMoon;
using static BetterNightSky.BetterNightSky;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("BetterNightSky")]
[ExtendsFromMod("BetterNightSky")]
[Autoload(Side = ModSide.Client)]
public sealed class BetterNightSkySystem : IOrderedLoadable
{
    #region Private Fields

    private delegate void orig_On_Main_DrawStarsInBackground(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial);
    private static Hook? DisableDoubleOrig;

    private static ILHook? LoadMoonStyle;
    private static ILHook? UnloadMoonStyle;

    #endregion

    #region Public Properties

    public static int StyleIndex { get; private set; }

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // QueueMainThreadAction can be ignored as this mod is loaded first regardless.
    public void Load()
    {
        IsEnabled = true;

        MethodInfo? on_Main_DrawStarsInBackground = typeof(BetterNightSky.BetterNightSky).GetMethod(nameof(On_Main_DrawStarsInBackground), NonPublic | Static);

        if (on_Main_DrawStarsInBackground is not null)
            DisableDoubleOrig = new(on_Main_DrawStarsInBackground,
                DoubleDetour);

            // TODO: Rename this class.
            // TODO: PR this to the original developer?
            // TODO: Don't use sketchy load IL edits.
        MethodInfo? onModLoad = typeof(BetterNightSky.BetterNightSky.BetterNightSkySystem).GetMethod(nameof(BetterNightSky.BetterNightSky.BetterNightSkySystem.OnModLoad), Public | Instance);

        if (onModLoad is not null)
            LoadMoonStyle = new(onModLoad,
                AddMoonStyle);

        MethodInfo? doUnloads = typeof(BetterNightSky.BetterNightSky.BetterNightSkySystem).GetMethod(nameof(BetterNightSky.BetterNightSky.BetterNightSkySystem.DoUnloads), Public | Instance);

        if (doUnloads is not null)
            UnloadMoonStyle = new(doUnloads,
                RemoveMoonStyle);
    }

    public void Unload() 
    { 
        DisableDoubleOrig?.Dispose();

        LoadMoonStyle?.Dispose();
        UnloadMoonStyle?.Dispose();
    }

    public short Index => 2;

    #endregion

    private void DoubleDetour(orig_On_Main_DrawStarsInBackground orig, On_Main.orig_DrawStarsInBackground origorig, Main self, Main.SceneArea sceneArea, bool artificial) =>
        origorig(self, sceneArea, artificial);

    #region Load Moon Styles

    private void AddMoonStyle(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<NightConfig>(nameof(NightConfig.Config)),
                i => i.MatchLdfld<NightConfig>(nameof(NightConfig.UseHighResMoon)));

            c.EmitDelegate((bool useHighRes) =>
            {
                StyleIndex = -1;

                if (!useHighRes)
                    return false;

                Array.Resize(ref TextureAssets.Moon, TextureAssets.Moon.Length + 1);

                StyleIndex = TextureAssets.Moon.Length - 1;

                    // TODO: Lazy loading.
                TextureAssets.Moon[^1] = ModContent.Request<Texture2D>("BetterNightSky/Textures/Moon1", AssetRequestMode.AsyncLoad);

                return false;
            });
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error("Failed to patch \"BetterNightSky.BetterNightSkySystem.OnModLoad\".");

            throw new ILPatchFailureException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    private void RemoveMoonStyle(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel skipLoopTarget = c.DefineLabel();

            c.GotoNext(i => i.MatchRet());

            c.GotoPrev(MoveType.Before,
                i => i.MatchLdcI4(-1),
                i => i.MatchCall<Star>(nameof(Star.SpawnStars)));

            c.MarkLabel(skipLoopTarget);

            c.GotoPrev(MoveType.Before,
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out _),
                i => i.MatchBr(out _));

            c.EmitDelegate(() =>
            {
                if (!NightConfig.Config.UseHighResMoon)
                    return;

                Asset<Texture2D> betterNightSkyMoon = ModContent.Request<Texture2D>("BetterNightSky/Textures/Moon1", AssetRequestMode.AsyncLoad);
                int index = -1;
                for (int i = 0; i < TextureAssets.Moon.Length; i++)
                {
                    if (TextureAssets.Moon[i] != betterNightSkyMoon)
                        continue;

                    index = i;
                    break;
                }

                if (TextureAssets.Moon.Length > StyleIndex + 1)
                {
                    int j = 0;
                    for (int i = index + 1; i < TextureAssets.Moon.Length; i++)
                    {
                        TextureAssets.Moon[index + j] = TextureAssets.Moon[i];
                        j++;
                    }
                }

                Array.Resize(ref TextureAssets.Moon, TextureAssets.Moon.Length - 1);
            });

            c.EmitBr(skipLoopTarget);
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error("Failed to patch \"Main.DrawSunAndMoon\".");

            throw new ILPatchFailureException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    #endregion

    #region Drawing

    public static void DrawSpecialStars(float alpha)
    {
        Main.spriteBatch.End(out var snapshot);
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, snapshot.RasterizerState, RealisticSkySystem.ApplyStarShader(), snapshot.TransformMatrix);

            // This isn't ideal but it doesn't matter.
        int i = 0;

        CountStars();
        drawStarPhase = 1;

            // Can't ref a readonly.
        Main.SceneArea sceneArea = SunAndMoonSystem.SceneArea;
        foreach (Star star in Main.star.Where(s => s is not null && !s.hidden && SpecialStarType(s) && CanDrawSpecialStar(s)))
        {
            i++;
            Main.instance.DrawStar(ref sceneArea, alpha, Main.ColorOfTheSkies, i, star, false, false);
        }
    }

    #endregion

    #region Public Methods

    public static void ModifyMoonScale(ref float scale) => AdjustMoonScaleMethod(ref scale);

    #endregion
}
