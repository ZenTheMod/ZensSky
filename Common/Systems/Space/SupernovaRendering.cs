using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core.Systems.ModCall;
using static ZensSky.Common.Systems.Space.StarHooks;
using static ZensSky.Common.Systems.Space.StarSystem;
using static ZensSky.Common.Systems.Space.SupernovaSystem;
using Supernova = ZensSky.Common.DataStructures.Supernova;

namespace ZensSky.Common.Systems.Space;

public static class SupernovaRendering
{
    #region Loading

    [OnLoad(Side = ModSide.Client)]
    public static void Load() =>
        PostDrawStars += SupernovaePostDraw;

    #endregion

    #region Drawing

    [ModCall(
        "DrawSupernova", "DrawSupernovas",
        "DrawAllSupernova", "DrawAllSupernovas", "DrawAllSupernovae")]
    public static void DrawSupernovae(SpriteBatch spriteBatch, GraphicsDevice device, float alpha)
    {
        if (!SkyEffects.Supernova.IsReady)
            return;

        float rotation = -StarRotation;

        ReadOnlySpan<Supernova> activeSupernovae = [.. Supernovae.Where(s => s.IsActive && s.State == SupernovaState.Expanding)];

        if (RealisticSkySystem.IsEnabled)
            RealisticSkySystem.SetAtmosphereParams(SkyEffects.Supernova.Value);

        for (int i = 0; i < activeSupernovae.Length; i++)
            activeSupernovae[i].Draw(spriteBatch, device, alpha, rotation);
    }

    private static void SupernovaePostDraw(SpriteBatch spriteBatch, in SpriteBatchSnapshot snapshot, float alpha, Matrix transform)
    {
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, snapshot.DepthStencilState, snapshot.RasterizerState, null, transform);

        GraphicsDevice device = Main.instance.GraphicsDevice;

        DrawSupernovae(spriteBatch, device, alpha);

        spriteBatch.End();
    }

    #endregion
}
