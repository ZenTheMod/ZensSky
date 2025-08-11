using Daybreak.Common.CIL;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Compat;
using ZensSky.Common.Systems.SunAndMoon;
using ZensSky.Core.Utils;
using ZensSky.Core.Exceptions;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.SunAndMoon.SunAndMoonSystem;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Clouds;

[Autoload(Side = ModSide.Client)]
public sealed class CloudSystem : ModSystem
{
    #region Private Fields

    private const float FlareEdgeFallOffStart = 1f;
    private const float FlareEdgeFallOffEnd = 1.11f;

    private const float NoonAlpha = .35f;

    private static readonly Color SunMultiplier = new(255, 245, 225);
    private static readonly Color MoonMultiplier = new(40, 40, 50);

    private delegate void orig_DrawCloud(int cloudIndex, Color color, float yOffset);
    private static Hook? PatchDrawCloud;

    #endregion

    #region Public Properties

    public static bool LightClouds { get; set; }

    #endregion

    #region Loading

    public override void Load() 
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DrawSurfaceBG += ApplyCloudLighting;

            MethodInfo? drawCloud = typeof(Main).GetMethod($"<{nameof(Main.DrawSurfaceBG)}>g__DrawCloud|1826_0", Static | NonPublic);

            if (drawCloud is not null)
                PatchDrawCloud = new(drawCloud,
                    ApplyEdgeLighting);
        }); 
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            IL_Main.DrawSurfaceBG -= ApplyCloudLighting;
            PatchDrawCloud?.Dispose();
        });
    }

    #endregion

    private void ApplyCloudLighting(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            VariableDefinition snapshot = c.AddVariable<SpriteBatchSnapshot>();

            #region Shader Parameters

            c.EmitDelegate(() =>
            {
                if (!SkyConfig.Instance.CloudsEnabled ||
                    !SkyEffects.CloudLighting.IsReady)
                    return;

                Viewport viewport = Main.instance.GraphicsDevice.Viewport;

                Vector2 viewportSize = viewport.Bounds.Size();
                SkyEffects.CloudLighting.ScreenSize = viewportSize;

                SkyEffects.CloudLighting.UseEdgeLighting = false;

                Vector2 sunPosition = Info.SunPosition;
                Vector2 moonPosition = Info.MoonPosition;

                if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
                {
                    sunPosition.Y = viewportSize.Y - sunPosition.Y;
                    moonPosition.Y = viewportSize.Y - moonPosition.Y;
                }

                SkyEffects.CloudLighting.SunPosition = sunPosition;
                SkyEffects.CloudLighting.MoonPosition = moonPosition;

                Color sunColor = GetColor(true);
                SkyEffects.CloudLighting.SunColor = sunColor.ToVector4();

                Color moonColor = GetColor(false);
                SkyEffects.CloudLighting.MoonColor = moonColor.ToVector4();

                SkyEffects.CloudLighting.DrawSun = Main.dayTime && ShowSun;
                SkyEffects.CloudLighting.DrawMoon = (RedSunSystem.IsEnabled || !Main.dayTime) && ShowMoon;
            });

            #endregion

            #region Various Clouds

            for (int i = 0; i < 3; i++)
            {
                    // Match to before the loop.
                c.GotoNext(MoveType.Before, 
                    i => i.MatchBr(out _),
                    i => i.MatchLdsfld<Main>(nameof(Main.cloud)),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdelemRef(),
                    i => i.MatchLdfld<Cloud>(nameof(Cloud.active)));

                    // Apply our shader.
                c.EmitLdloca(snapshot);
                c.EmitDelegate(ApplyShader);

                    // Match to after the loop.
                c.GotoNext(MoveType.After, 
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdcI4(1),
                    i => i.MatchAdd(),
                    i => i.MatchStloc(out _),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdcI4(200),
                    i => i.MatchBlt(out _));

                c.EmitLdloc(snapshot);
                c.EmitDelegate(ResetSpritebatch);
            }

            #endregion

            #region CloudBG

                // Match to before the loop.
            c.GotoPrev(MoveType.Before,
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                i => i.MatchLdsfld(typeof(TextureAssets).FullName ?? "Terraria.GameContent.TextureAssets", nameof(TextureAssets.Background)),
                i => i.MatchLdsfld<Main>(nameof(Main.cloudBG)),
                i => i.MatchLdcI4(0),
                i => i.MatchLdelemI4());

                // Apply our shader.
            c.EmitLdloca(snapshot);
            c.EmitDelegate(ApplyShader);

                // Match to after the loop of the other drawn cloud background.
            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(22), // I dislike this.
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdarg(out _),
                i => i.MatchLdfld<Main>(nameof(Main.bgLoops)),
                i => i.MatchBlt(out _));

            c.EmitLdloc(snapshot);
            c.EmitDelegate(ResetSpritebatch);

            #endregion

            c.GotoNext(MoveType.Before,
                i => i.MatchRet());

            c.MoveAfterLabels();

            c.EmitDelegate(() => { LightClouds = true; });
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    private static void ApplyEdgeLighting(orig_DrawCloud orig, int cloudIndex, Color color, float yOffset)
    {
        if (!ZensSky.CanDrawSky || 
            !LightClouds || 
            !SkyConfig.Instance.CloudsEnabled || 
            !SkyConfig.Instance.CloudsEdgeLighting || 
            !SkyEffects.CloudLighting.IsReady)
        {
            orig(cloudIndex, color, yOffset);
            return;
        }

        SkyEffects.CloudLighting.UseEdgeLighting = true;

        Cloud cloud = Main.cloud[cloudIndex];

            // This has the potential to break when modders use custom draw functions with a ModCloud class, but no one uses ModCloud anyway lmao.
        Texture2D cloudTexture = TextureAssets.Cloud[cloud.type].Value;

        Vector2 pixelSize = new Vector2(2) / cloudTexture.Size();
        pixelSize /= cloud.scale;

        SkyEffects.CloudLighting.Pixel = pixelSize;

            // Account for the sprite direction.
        SpriteEffects dir = cloud.spriteDir;

        Vector2 flip = new(
            dir.HasFlag(SpriteEffects.FlipHorizontally) ? -1 : 1,
            dir.HasFlag(SpriteEffects.FlipVertically) ? -1 : 1);

        SkyEffects.CloudLighting.Flipped = flip;

        orig(cloudIndex, color, yOffset);
    }

    private static void ApplyShader(ref SpriteBatchSnapshot snapshot)
    {
        if (!ZensSky.CanDrawSky || 
            !LightClouds || 
            !SkyConfig.Instance.CloudsEnabled || 
            !SkyEffects.CloudLighting.IsReady)
            return;

        SkyEffects.CloudLighting.UseEdgeLighting = false;

        SkyEffects.CloudLighting.Apply();

        bool edgeLighting = SkyConfig.Instance.CloudsEdgeLighting;

        Main.spriteBatch.End(out snapshot);
        Main.spriteBatch.Begin(edgeLighting ? SpriteSortMode.Immediate : snapshot.SortMode, snapshot.BlendState, SamplerState.PointClamp, snapshot.DepthStencilState, snapshot.RasterizerState, SkyEffects.CloudLighting.Value, snapshot.TransformMatrix);

        GraphicsDevice device = Main.instance.GraphicsDevice;

            // Samples the moon texture to grab a more accurate color. (May not work correctly when not using the moon overhaul.)
        device.Textures[1] = SunAndMoonRenderingSystem.MoonTexture.Value;
        device.SamplerStates[1] = SamplerState.PointWrap;
    }

    private static void ResetSpritebatch(SpriteBatchSnapshot snapshot)
    {
        if (!ZensSky.CanDrawSky || !LightClouds || !SkyConfig.Instance.CloudsEnabled)
            return;

        Main.spriteBatch.Restart(in snapshot);
    }

    private static Color GetColor(bool day)
    {
            // This will behave a little buggy with Red Sun as the sun will take priority but I'm not implementing an array based light system as of now.
        Vector2 position = day ? Info.SunPosition : Info.MoonPosition;
        float centerX = Utilities.HalfScreenSize.X;

        float distanceFromCenter = MathF.Abs(centerX - position.X) / centerX;

        Color color = day ? Info.SunColor : Info.MoonColor;
        color = color.MultiplyRGB(day ? SunMultiplier : MoonMultiplier);

            // Add a fadeinout effect so the color doesnt just suddenly pop up.
        color *= Utils.Remap(distanceFromCenter, FlareEdgeFallOffStart, FlareEdgeFallOffEnd, 1f, 0f);
            // And lessen it at the lower part of the screen.
        color *= 1 - (position.Y / Utilities.ScreenSize.Y);

            // Decrease the intensity at noon to make the clouds not just be pure white.
            // And alter the intensity depending on the moon phase, where a new moon casts no light.
        if (day)
            color *= MathHelper.Lerp(NoonAlpha, 1f, MathF.Pow(distanceFromCenter, 2));
        else
            color *= MathF.Abs(4 - Main.moonPhase) * .25f;

        return color;
    }
}
