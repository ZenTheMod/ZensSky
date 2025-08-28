using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;
using static ZensSky.Common.Systems.Space.ShootingStarSystem;

namespace ZensSky.Common.Systems.Space;

[Autoload(Side = ModSide.Client)]
public sealed class ShootingStarRendering : ModSystem
{
    #region Loading

    public override void Load()
    {
        MainThreadSystem.Enqueue(() => IL_Main.DoDraw += DrawAfterSunAndMoon);

        IL_Main.DrawCapture += DrawAfterSunAndMoon;
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() => IL_Main.DoDraw -= DrawAfterSunAndMoon);

        IL_Main.DrawCapture -= DrawAfterSunAndMoon;
    }

    #endregion

    #region Inject

    private void DrawAfterSunAndMoon(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchCall<Main>(nameof(Main.DrawSunAndMoon)));

            c.EmitDelegate(DrawShootingStars);
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion

    #region Drawing

    private static void DrawShootingStars()
    {
        if (!ZensSky.CanDrawSky || !ShowShootingStars)
        {
            ShowShootingStars = true;
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        float alpha = StarSystem.StarAlpha;

        spriteBatch.End(out var snapshot);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, RealisticSkySystem.ApplyStarShader(), snapshot.TransformMatrix);

        ReadOnlySpan<ShootingStar> activeShootingStars = [.. ShootingStars.Where(s => s.IsActive)];

        for (int i = 0; i < activeShootingStars.Length; i++)
            activeShootingStars[i].Draw(spriteBatch, device, alpha);

        spriteBatch.Restart(in snapshot);
    }

    #endregion
}
