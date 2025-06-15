using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Systems.SunAndMoon;
using static BetterNightSky.BetterNightSky;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("BetterNightSky")]
[ExtendsFromMod("BetterNightSky")]
[Autoload(Side = ModSide.Client)]
public sealed class BetterNightSkySystem : ModSystem
{
    #region Private Fields

    private delegate void orig_On_Main_DrawStarsInBackground(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial);
    private static Hook? DisableDoubleOrig;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // QueueMainThreadAction can be ignored as this mod is loaded first regardless.
    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? on_Main_DrawStarsInBackground = typeof(BetterNightSky.BetterNightSky).GetMethod(nameof(On_Main_DrawStarsInBackground), NonPublic | Static);

        if (on_Main_DrawStarsInBackground is not null)
            DisableDoubleOrig = new(on_Main_DrawStarsInBackground,
                DoubleDetour);
    }

    public override void Unload() => DisableDoubleOrig?.Dispose();

    #endregion

    private void DoubleDetour(orig_On_Main_DrawStarsInBackground orig, On_Main.orig_DrawStarsInBackground origorig, Main self, Main.SceneArea sceneArea, bool artificial) =>
        origorig(self, sceneArea, artificial);

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
