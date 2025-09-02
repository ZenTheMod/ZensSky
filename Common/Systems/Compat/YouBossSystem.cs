using Microsoft.Xna.Framework;
using System;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZensSky.Common.Systems.Space;
using ZensSky.Common.Systems.SunAndMoon;
using ZensSky.Core.Systems;
using static System.Reflection.BindingFlags;

namespace ZensSky.Common.Systems.Compat;

[ExtendsFromMod("YouBoss")]
[Autoload(Side = ModSide.Client)]
public sealed class YouBossSystem : ModSystem
{
    #region Private Fields

    private const string SkyKey = "YouBoss:TerraBlade";

    private static Type? TerraBladeSkyType;

    private static FieldInfo? TerraBladeSkyOpacityInfo;

    #endregion

    #region Public Properties

    public static bool IsEnabled { get; private set; }

    #endregion

    #region Loading

    public override void Load() 
    {
        if (!ModLoader.TryGetMod("YouBoss", out Mod youBoss))
            return;

        IsEnabled = true;

        Assembly youBossAsm = youBoss.Code;

        TerraBladeSkyType = youBossAsm.GetType("YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers.TerraBladeSky");
        ArgumentNullException.ThrowIfNull(TerraBladeSkyType);

        TerraBladeSkyOpacityInfo = TerraBladeSkyType?.GetField("Opacity", NonPublic | Static);
        ArgumentNullException.ThrowIfNull(TerraBladeSkyOpacityInfo);

        MainThreadSystem.Enqueue(() =>
            On_Main.DoDraw += HideSky);
    }

    public override void Unload() =>
        MainThreadSystem.Enqueue(() => On_Main.DoDraw -= HideSky);

    #endregion

    #region Updating

    private void HideSky(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);

        if (!SkyManager.Instance[SkyKey].IsActive() || SkyManager.Instance[SkyKey].GetType() != TerraBladeSkyType)
            return;

        float opacity = (float)(TerraBladeSkyOpacityInfo?.GetValue(null) ?? 0f);
        opacity = 1f - opacity;

        if (StarSystem.StarAlpha >= opacity)
            StarSystem.StarAlphaOverride = opacity;

        SunAndMoonSystem.ShowMoon = false;
        ShootingStarSystem.ShowShootingStars = false;
    }

    #endregion
}
