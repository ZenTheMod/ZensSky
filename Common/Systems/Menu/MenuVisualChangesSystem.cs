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
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;
using ZensSky.Core.Utils;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Menu;

[Autoload(Side = ModSide.Client)]
public sealed class MenuVisualChangesSystem : ModSystem
{
    #region Private Fields

    private static ILHook? RelocateCreditsDrawing;

    private static ILHook? PatchMoonTextures;

    #endregion

    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() =>
        {
            MethodInfo? updateAndDrawModMenuInner = typeof(MenuLoader).GetMethod(nameof(MenuLoader.UpdateAndDrawModMenuInner), NonPublic | Static);

            if (updateAndDrawModMenuInner is not null)
                RelocateCreditsDrawing = new(updateAndDrawModMenuInner,
                    DrawAfterLogo);

            MethodInfo? getMoonTexture = typeof(ModMenu).GetProperty(nameof(ModMenu.MoonTexture), Public | Instance)?.GetGetMethod();

            if (getMoonTexture is not null)
                PatchMoonTextures = new(getMoonTexture,
                    UncapMoonTextures);
        });

        On_CreditsRollSky.Draw += HideCredits;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
        {
            RelocateCreditsDrawing?.Dispose();

            PatchMoonTextures?.Dispose();
        });

        On_CreditsRollSky.Draw -= HideCredits;
    }

    #endregion

    #region Credits

    private void DrawAfterLogo(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int spriteBatchIndex = -1;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld(typeof(MenuLoader).FullName ?? "Terraria.ModLoader.MenuLoader", nameof(MenuLoader.currentMenu)),
                i => i.MatchLdarg(out spriteBatchIndex),
                i => i.MatchLdloc(out _),
                i => i.MatchLdarg(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdarg(out _),
                i => i.MatchCallvirt<ModMenu>(nameof(ModMenu.PostDrawLogo)));

            c.MoveAfterLabels();

            c.EmitLdarg(spriteBatchIndex);

            c.EmitDelegate(DrawCredits);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

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
