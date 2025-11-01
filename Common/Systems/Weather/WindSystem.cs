using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Systems.Compat;
using ZensSky.Core;
using ZensSky.Core.Particles;
using ZensSky.Core.Utils;

namespace ZensSky.Common.Systems.Weather;

[Autoload(Side = ModSide.Client)]
public sealed class WindSystem : ModSystem
{
    #region Private Fields

    private const float MinWind = 0.17f;
    private const float WindSpawnChance = 35f;
    private const int WindLoopChance = 14;

    private const int Margin = 100;

    #endregion

    #region Public Fields

    public const int WindCount = 65;
    public static readonly ParticleHandler<WindParticle> Winds = new(WindCount);

    #endregion

    #region Loading

    public override void Load() =>
        MainThreadSystem.Enqueue(() => On_Main.DoUpdate += UpdateWind);

    public override void Unload() => 
        MainThreadSystem.Enqueue(() => On_Main.DoUpdate -= UpdateWind);

    #endregion

    #region Updating

    private void UpdateWind(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);

        if (Main.dedServ ||
            Main.gamePaused && !Main.gameMenu ||
            HighFPSSupportSystem.IsPartialTick ||
            !SkyConfig.Instance.UseWindParticles ||
            SkyConfig.Instance.WindOpacity <= 0)
            return;

        Winds.Update();

        if (MathF.Abs(Main.WindForVisuals) < MinWind)
            return;

        SpawnWind();
    }

    private static void SpawnWind()
    {
        float spawnChance = WindSpawnChance / MathF.Abs(Main.WindForVisuals);

        if (!Main.rand.NextBool((int)spawnChance))
            return;

        Vector2 screensize = Utilities.ScreenSize;

        Rectangle spawn = new((int)(Main.screenPosition.X - screensize.X * Main.WindForVisuals * .5f), (int)Main.screenPosition.Y,
            (int)screensize.X, (int)screensize.Y);

        spawn.Inflate(Margin, Margin);

        Vector2 position = Main.rand.NextVector2FromRectangle(spawn);

        if (!Main.gameMenu && (position.Y > Main.worldSurface * 16f || Collision.SolidCollision(position, 1, 1)))
            return;

        Winds.Spawn(new(position, Main.WindForVisuals, Main.rand.NextBool(WindLoopChance)));
    }

    #endregion
}
