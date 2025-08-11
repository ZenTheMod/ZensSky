using Macrocosm.Common.Drawing.Sky;
using Macrocosm.Content.Skies.Moon;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using SubworldLibrary;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Stars;
using ZensSky.Core.Exceptions;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonRenderingSystem;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;
using MacrocosmSky = Macrocosm.Common.Drawing.Sky;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("Macrocosm", "SubworldLibrary")]
[ExtendsFromMod("Macrocosm", "SubworldLibrary")]
[Autoload(Side = ModSide.Client)]
public sealed class MacrocosmSystem : ModSystem
{
    #region Private Fields

    private static ILHook? PatchStarDrawing;

    private delegate void orig_Rotate(CelestialBody self);
    private static Hook? PatchRotation;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    public static bool InAnySubworld
    {
            // Although unlikely certain steps taken with reloading mods might cause inlined code refering to other assemblies to throw.
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => SubworldSystem.AnyActive<Macrocosm.Macrocosm>();
    } 

    #endregion

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? draw = typeof(MoonSky).GetMethod(nameof(MoonSky.Draw), Public | Instance);

        if (draw is not null)
            PatchStarDrawing = new(draw,
                DrawStars);

            // Account for RedSun's reversal of the sun's orbit.
        if (!RedSunSystem.IsEnabled || !RedSunSystem.FlipSunAndMoon)
            return;

        MethodInfo? rotate = typeof(CelestialBody).GetMethod(nameof(CelestialBody.Rotate), Public | Instance);

        if (rotate is not null)
            PatchRotation = new(rotate,
                ReverseRotation);
    }

    public override void Unload()
    {
        PatchStarDrawing?.Dispose();

        PatchRotation?.Dispose();
    }

    #endregion

    #region MoonSky

    private void DrawStars(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            ILLabel jumpStarDrawingTarget = c.DefineLabel();

            ILLabel jumpSunDrawingTarget = c.DefineLabel();

            int selfIndex = -1;

            int spriteBatchIndex = -1;

            int brightnessIndex = -1;

                // Skip over star drawing.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg(out selfIndex),
                i => i.MatchLdfld<MoonSky>(nameof(MoonSky.starsDay)),
                i => i.MatchLdarg(out spriteBatchIndex));

            c.EmitBr(jumpStarDrawingTarget);

            c.GotoNext(MoveType.After,
                i => i.MatchLdfld<MoonSky>(nameof(MoonSky.starsNight)),
                i => i.MatchLdarg(spriteBatchIndex),
                i => i.MatchLdloc(out brightnessIndex),
                i => i.MatchCallvirt<MacrocosmSky::Stars>(nameof(MacrocosmSky::Stars.DrawAll)));

            c.MarkLabel(jumpStarDrawingTarget);

            c.EmitLdarg(spriteBatchIndex);

            c.EmitDelegate((SpriteBatch spriteBatch) =>
            {
                float alpha = MoonSky.ComputeBrightness(7200.0, 46800.0, .3f, 1f);

                StarRenderingSystem.DrawStarsToSky(spriteBatch, alpha);
            });

                // Skip over sun drawing.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdarg(out _),
                i => i.MatchLdfld<MoonSky>(nameof(MoonSky.sun)));

            c.MoveAfterLabels();

                // Skip over sun drawing.
            if (SkyConfig.Instance.SunAndMoonRework || RealisticSkySystem.IsEnabled)
            {
                c.EmitDelegate(() => ShowSun);
                c.EmitBrtrue(jumpSunDrawingTarget);

                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg(spriteBatchIndex),
                    i => i.MatchCallvirt<CelestialBody>(nameof(CelestialBody.Draw)));

                c.MarkLabel(jumpSunDrawingTarget);

                c.EmitLdarg(selfIndex);
                c.EmitLdarg(spriteBatchIndex);

                c.EmitDelegate((MoonSky self, SpriteBatch spriteBatch) =>
                {
                    GraphicsDevice device = Main.instance.GraphicsDevice;

                    CelestialBody sun = self.sun;

                    sun.Update();

                    SetInfo(sun.Center, sun.Color, sun.Rotation, sun.Scale, true);

                        // EventSystem.DemonSun
                    if (sun.ShouldDraw())
                        DrawSun(spriteBatch, device);
                });
            }

                // TODO: High-res earth drawing, (Good luck with the flat earth model.)
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region RedSun Rotation

    private void ReverseRotation(orig_Rotate orig, CelestialBody self)
    {
        orig(self);

        self.Rotation = -self.Rotation;

        float width = Main.screenWidth + self.bodyTexture.Value.Width * 2;

        self.Center = new(width - self.Center.X, self.Center.Y);
    }

    #endregion
}
