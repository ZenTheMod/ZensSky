using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Animations;
using Terraria.GameContent.Skies;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;
using static ZensSky.Common.Systems.Sky.Lighting.SkyLightSystem;

namespace ZensSky.Common.Systems.Menu;

/// <summary>
/// Edits and Hooks:
/// <list type="bullet">
///     <item>
///         <see cref="LogoDrawing"/><br/>
///         Forces the credits to draw after <see cref="ModMenu.PreDrawLogo"/>, and draws a lighting effect over the logo.<br/>
///         The former fixes a bug where credits are hidden on mods that have <a href="https://i.imgur.com/IqsIGYT.png">obnoxious</a> menu backkgrounds;
///         see <a href="https://github.com/tModLoader/tModLoader/pull/4716#issuecomment-3146423549">this comment</a> for more details.<br/>
///         The latter only works on the default logo (tModLoader's.)
///     </item>
///     <item>
///         <see cref="UncapMoonTextures"/><br/>
///         Uncaps the vanilla moon styles allowed on <see cref="ModMenu"/>s. - Untested, may inline.
///     </item>
///     <item>
///         <see cref="HideCredits"/><br/>
///         Hides vanilla credits seeing as we draw them after <see cref="ModMenu.PreDrawLogo"/>.
///     </item>
/// </list>
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class ModMenuSystem : ModSystem
{
    #region Private Fields

    private static ILHook? PatchUpdateAndDrawModMenuInner;

    private static ILHook? PatchGetMoonTexture;

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() =>
        {
            MethodInfo? updateAndDrawModMenuInner = typeof(MenuLoader).GetMethod(nameof(MenuLoader.UpdateAndDrawModMenuInner), NonPublic | Static);

            if (updateAndDrawModMenuInner is not null)
                PatchUpdateAndDrawModMenuInner = new(updateAndDrawModMenuInner,
                    LogoDrawing);

            MethodInfo? getMoonTexture = typeof(ModMenu).GetProperty(nameof(ModMenu.MoonTexture), Public | Instance)?.GetGetMethod();

            if (getMoonTexture is not null)
                PatchGetMoonTexture = new(getMoonTexture,
                    UncapMoonTextures);
        });

        On_CreditsRollSky.Draw += HideCredits;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            PatchUpdateAndDrawModMenuInner?.Dispose();

            PatchGetMoonTexture?.Dispose();
        });

        On_CreditsRollSky.Draw -= HideCredits;
    }

    #endregion

    #region Logo

    private void LogoDrawing(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int spriteBatchIndex = -1;
            int logoDrawCenterIndex = -1;
            int colorIndex = -1; // arg
            int logoRotationIndex = -1; // arg
            int logoScale2Index = -1;

            c.GotoNext(
                i => i.MatchCallvirt<ModMenu>(nameof(ModMenu.PreDrawLogo)));

                // Pull indicies from the instructions of the SpriteBatch.Draw call.
            c.GotoNext(
                i => i.MatchLdloc(out logoDrawCenterIndex),
                i => i.MatchLdcI4(out _),
                i => i.MatchLdcI4(out _));

            c.GotoNext(
                i => i.MatchNewobj<Rectangle?>(),
                i => i.MatchLdarg(out colorIndex),
                i => i.MatchLdarg(out logoRotationIndex));

            c.GotoNext(
               i => i.MatchNewobj<Vector2>(),
               i => i.MatchLdloc(out logoScale2Index));

            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld(typeof(MenuLoader).FullName ?? "Terraria.ModLoader.MenuLoader", nameof(MenuLoader.currentMenu)),
                i => i.MatchLdarg(out spriteBatchIndex));

            c.MoveBeforeLabels();

            c.EmitLdarg(spriteBatchIndex);

            c.EmitLdloc(logoDrawCenterIndex);
            c.EmitLdarg(colorIndex);
            c.EmitLdarg(logoRotationIndex);
            c.EmitLdloc(logoScale2Index);

            c.EmitCall(DrawLighting);

            c.MoveAfterLabels();

                // Draw credits outside of the jump.
            c.EmitLdarg(spriteBatchIndex);

            c.EmitCall(DrawCredits);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Lighting

    private static void DrawLighting(SpriteBatch spriteBatch, Vector2 logoDrawCenter, Color color, float logoRotation, float logoScale2)
    {
        if (!ZensSky.CanDrawSky ||
            MenuLoader.currentMenu.Logo.Value != ModMenu.modLoaderLogo.Value ||
            !UIEffects.LogoNormals.IsReady)
            return;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Immediate, snapshot.BlendState, snapshot.SamplerState, snapshot.DepthStencilState, snapshot.RasterizerState, null, snapshot.TransformMatrix);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        Viewport viewport = device.Viewport;

        Vector2 viewportSize = viewport.Bounds.Size();

        UIEffects.LogoNormals.ScreenSize = viewportSize;

        Texture2D normal = MiscTextures.ModLoaderLogoNormals;
        Vector2 normalOrigin = normal.Size() * .5f;

        InvokeForActiveLights((info) =>
        {
            UIEffects.LogoNormals.LightPosition = info.Position;
            UIEffects.LogoNormals.LightColor = info.Color.ToVector4();
            
            UIEffects.LogoNormals.UseTexture = false;

            if (info.Texture is not null)
            {
                UIEffects.LogoNormals.UseTexture = true;
                device.Textures[1] = info.Texture;
            }

            UIEffects.LogoNormals.Rotation = logoRotation;

            UIEffects.LogoNormals.Apply();

            spriteBatch.Draw(normal, logoDrawCenter, null, color, logoRotation, normalOrigin, logoScale2, SpriteEffects.None, 0f);
        });

        spriteBatch.Restart(in snapshot);
    }

    #endregion

    #region Credits

    private void HideCredits(On_CreditsRollSky.orig_Draw orig, CreditsRollSky self, SpriteBatch spriteBatch, float minDepth, float maxDepth)
    {
        if (!Main.gameMenu)
            orig(self, spriteBatch, minDepth, maxDepth);
    }

    private static void DrawCredits(SpriteBatch spriteBatch)
    {
        CreditsRollSky creditsRoll = (CreditsRollSky)SkyManager.Instance["CreditsRoll"];

        if (!creditsRoll.IsActive() ||
            !creditsRoll.IsLoaded)
            return;

        spriteBatch.End(out var snapshot);

        Matrix transform = Main.CurrentFrameFlags.Hacks.CurrentBackgroundMatrixForCreditsRoll;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, transform);

        Vector2 anchorPositionOnScreen = new(Utilities.HalfScreenSize.X, 300);

        GameAnimationSegment info = new()
        {
            SpriteBatch = spriteBatch,
            AnchorPositionOnScreen = anchorPositionOnScreen,
            TimeInAnimation = creditsRoll._currentTime,
            DisplayOpacity = creditsRoll._opacity
        };

        List<IAnimationSegment> list = creditsRoll._segmentsInMainMenu;

        for (int i = 0; i < list.Count; i++)
            list[i].Draw(ref info);

        spriteBatch.Restart(in snapshot);
    }

    #endregion

    #region Moon Textures

    private void UncapMoonTextures(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdcI4(out _),
                i => i.MatchLdcI4(out _));

            c.EmitPop();

            c.EmitDelegate(() => TextureAssets.Moon.Length - 1);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion
}
