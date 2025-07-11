﻿using Daybreak.Common.CIL;
using Macrocosm.Common.Drawing.Sky;
using Macrocosm.Content.Skies.Moon;
using Macrocosm.Content.Subworlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Utilities;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonRenderingSystem;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;
using MacrocosmSky = Macrocosm.Common.Drawing.Sky;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("Macrocosm")]
[ExtendsFromMod("Macrocosm")]
[Autoload(Side = ModSide.Client)]
public sealed class MacrocosmSystem : ModSystem
{
    #region Private Fields

    private static ILHook? InjectStarDrawing;

    private delegate void orig_Update(MoonSky self, GameTime gameTime);
    private static Hook? SunPosition;

    private delegate void orig_Rotate(CelestialBody self);
    private static Hook? ModifyRotation;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        IsEnabled = true;

        MethodInfo? draw = typeof(MoonSky).GetMethod(nameof(MoonSky.Draw), Public | Instance);

        if (draw is not null)
            InjectStarDrawing = new(draw,
                DrawStars);

        MethodInfo? update = typeof(MoonSky).GetMethod(nameof(MoonSky.Update), Public | Instance);

        if (update is not null)
            SunPosition = new(update,
                MoonSkySunPosition);

            // Account for RedSun's reversal of the sun's orbit.
                // This is probably fine lorewise.
        if (!RedSunSystem.IsEnabled || !RedSunSystem.FlipSunAndMoon)
            return;

        MethodInfo? rotate = typeof(CelestialBody).GetMethod(nameof(CelestialBody.Rotate), NonPublic | Instance);

        if (rotate is not null)
            ModifyRotation = new(rotate,
                ReverseRotation);
    }

    public override void Unload()
    {
        InjectStarDrawing?.Dispose();

        ModifyRotation?.Dispose();
    }

    #endregion

    #region MoonSky

    private void DrawStars(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            VariableDefinition oldTargetsIndex = il.AddVariable<RenderTargetBinding[]>();

            ILLabel jumpStarDrawingTarget = c.DefineLabel();

            ILLabel jumpSunDrawingTarget = c.DefineLabel();

            int selfIndex = -1;

            int spriteBatchIndex = -1;

            int brightnessIndex = -1;

            c.EmitLdloca(oldTargetsIndex);

            c.EmitDelegate(PixelateSkySystem.PrepareTarget);

                // Skip over star drawing.
            c.GotoNext(MoveType.Before,
                i => i.MatchNop(),
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
            c.EmitLdloc(brightnessIndex);

            c.EmitDelegate(StarRenderingSystem.DrawStarsToSky);

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

                        // EventSystem.DemonSun
                    DrawSun(spriteBatch, device);
                });
            }
            else
            {
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg(spriteBatchIndex),
                    i => i.MatchCallvirt<CelestialBody>(nameof(CelestialBody.Draw)));
            }

            c.EmitLdloca(oldTargetsIndex);

            c.EmitDelegate(PixelateSkySystem.DrawTarget);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"MoonSky.Draw\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    #endregion

    #region Sun Position

    private void MoonSkySunPosition(orig_Update orig, MoonSky self, GameTime gameTime)
    {
        orig(self, gameTime);

        FetchSunInfo(SceneArea, self.sun.Center, self.sun.Color, self.sun.Rotation, self.sun.Scale);
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

        // public static bool InAnySubworld => SubworldSystem.IsActive<Moon>();
}
