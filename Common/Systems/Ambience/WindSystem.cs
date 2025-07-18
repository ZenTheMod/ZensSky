﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Core.Utils;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Ambience;

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

    public const int WindCount = 45;
    public static readonly WindParticle[] Winds = new WindParticle[WindCount];

    #endregion

    #region Loading

    public override void Load() 
    { 
        Array.Clear(Winds);
        MainThreadSystem.Enqueue(() => On_Main.DoUpdate += UpdateWind);
    }

    public override void Unload() => 
        MainThreadSystem.Enqueue(() => On_Main.DoUpdate -= UpdateWind);

    #endregion

    #region Updating

    private void UpdateWind(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);

        if (Main.dedServ || Main.gamePaused)
            return;

        if (!SkyConfig.Instance.WindParticles || SkyConfig.Instance.WindOpacity <= 0)
            return;

        for (int i = 0; i < WindCount; i++)
            if (Winds[i].IsActive)
                Winds[i].Update();

        if (MathF.Abs(Main.WindForVisuals) < MinWind)
            return;

        SpawnWind();
    }

    private static void SpawnWind()
    {
        if (!Main.rand.NextBool((int)(WindSpawnChance / MathF.Abs(Main.WindForVisuals))))
            return;

        int index = Array.FindIndex(Winds, w => !w.IsActive);

        if (index == -1)
            return;

        Vector2 screensize = Utilities.ScreenSize;

        Rectangle spawn = new((int)(Main.screenPosition.X - screensize.X * Main.WindForVisuals * 0.5f), (int)Main.screenPosition.Y,
            (int)screensize.X, (int)screensize.Y);

        spawn.Inflate(Margin, Margin);

        Vector2 position = Main.rand.NextVector2FromRectangle(spawn);

        if (!Main.gameMenu && (position.Y > Main.worldSurface * 16f || Collision.SolidCollision(position, 1, 1)))
            return;

        Winds[index] = WindParticle.CreateActive(position, Main.rand.NextBool(WindLoopChance));
    }

    #endregion
}
