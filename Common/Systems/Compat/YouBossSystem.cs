using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers;
using ZensSky.Common.Systems.Stars;
using ZensSky.Common.Systems.SunAndMoon;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Compat;

[JITWhenModsEnabled("YouBoss")]
[ExtendsFromMod("YouBoss")]
[Autoload(Side = ModSide.Client)]
public sealed class YouBossSystem : ModSystem
{
    #region Loading

    public override void Load() => 
        MainThreadSystem.Enqueue(() => On_Main.DoDraw += HideSky);

    public override void Unload() => 
        MainThreadSystem.Enqueue(() => On_Main.DoDraw -= HideSky);

    #endregion

    #region Updating

    private void HideSky(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);

        if (!(SkyManager.Instance[TerraBladeSky.SkyKey]?.IsActive() ?? false) || SkyManager.Instance[TerraBladeSky.SkyKey] is not TerraBladeSky sky)
            return;

        float opacity = 1f - TerraBladeSky.Opacity;

        if (StarSystem.StarAlpha >= opacity)
            StarSystem.StarAlphaOverride = opacity;

        SunAndMoonSystem.ShowMoon = false;
        ShootingStarSystem.ShowShootingStars = false;
    }

    #endregion
}
