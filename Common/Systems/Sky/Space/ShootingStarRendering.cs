using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using static ZensSky.Common.Systems.Sky.Space.ShootingStarSystem;
using static ZensSky.Common.Systems.Sky.SunAndMoon.SunAndMoonHooks;

namespace ZensSky.Common.Systems.Sky.Space;

[Autoload(Side = ModSide.Client)]
public sealed class ShootingStarRendering : ModSystem
{
    #region Loading

    public override void Load() =>
        PostDrawSunAndMoon += DrawShootingStarsPostSunAndMoon;

    #endregion

    #region Drawing

    private static void DrawShootingStarsPostSunAndMoon(SpriteBatch spriteBatch)
    {
        if (!ZensSky.CanDrawSky || !ShowShootingStars)
        {
            ShowShootingStars = true;
            return;
        }
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
