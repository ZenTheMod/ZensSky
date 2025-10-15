using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using RainOverhaul.Content;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZensSky.Core;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="UpdateRainShaders"/><br/>
///         Manually update the rain shader when on the main menu.
///     </item>
///     <item>
///         <see cref="ApplyNoiseTexture"/><br/>
///         See <a href="https://github.com/supchyan/terraria-rain-overhaul/issues/1">this issue</a> for more information.<br/>
///         TL;DR Rain Overhaul assumes the use of a vanilla noise texture.
///     </item>
/// </list>
/// </summary>
[JITWhenModsEnabled("RainOverhaul")]
[ExtendsFromMod("RainOverhaul")]
[Autoload(Side = ModSide.Client)]
public sealed class RainOverhaulSystem : ModSystem
{
    #region Private Fields

    private const string RainFilterKey = "RainFilter";

    private const float RainTransitionIncrement = .005f;

    private delegate void orig_PostUpdateTime(RainSystem self);

    private static Hook? PatchPostUpdateTime;

    private static RainSystem RainSystemInstance =>
        ModContent.GetInstance<RainSystem>();

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

        // RainOverhaul is a Both-Sided mod, meaning we cannot deliberatly load before or after it with build.txt sorting.
    public override void PostSetupContent()
    {
        IsEnabled = true;

        MainThreadSystem.Enqueue(() =>
            On_Main.DoUpdate += UpdateRainShaders);

        MethodInfo? postUpdateTime = typeof(RainSystem).GetMethod(nameof(RainSystem.PostUpdateTime), Public | Instance);

        if (postUpdateTime is not null)
            PatchPostUpdateTime = new(postUpdateTime,
                ApplyNoiseTexture);
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
            On_Main.DoUpdate -= UpdateRainShaders);

        PatchPostUpdateTime?.Dispose();
    }

    #endregion

    #region Updating

    private void UpdateRainShaders(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);

            // Only update this shader this way while on the titlescreen.
        if (!Main.gameMenu ||
            Filters.Scene[RainFilterKey] is null)
            return;

        Filters.Scene.Activate(RainFilterKey);

            // Increase a transition value based on if rain is active.
        float increment = Main.raining.ToDirectionInt() * RainTransitionIncrement;
        RainSystemInstance.RainTransition =
            MathHelper.Clamp(RainSystemInstance.RainTransition + increment, 0, Main.cloudAlpha);

        float cIntensity = ModContent.GetInstance<RainConfig>().cIntensity;

        float rainTransition = RainSystemInstance.RainTransition;

            // Unsure what to make of these magic numbers.
        float opacity = cIntensity * rainTransition;

        float intensity = RainSystemInstance.RainTransition;

        float progress = -Main.windSpeedCurrent * 4f;

        Filters.Scene[RainFilterKey].GetShader()
            .UseOpacity(opacity)
            .UseIntensity(intensity)
            .UseProgress(progress)
            .UseImage(MiscTextures.ColoredNoise.Asset, 0, SamplerState.LinearWrap);
    }

    #endregion

    #region Apply Noise Texture

    private void ApplyNoiseTexture(orig_PostUpdateTime orig, RainSystem self)
    {
        orig(self);

        if (Filters.Scene[RainFilterKey] is null)
            return;

        Filters.Scene[RainFilterKey].GetShader()
            .UseImage(MiscTextures.ColoredNoise.Asset, 0, SamplerState.LinearWrap);
    }

    #endregion
}
