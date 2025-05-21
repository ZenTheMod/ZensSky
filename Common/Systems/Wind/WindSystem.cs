using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.Systems.Wind;

[Autoload(Side = ModSide.Client)]
public sealed class WindSystem : ModSystem
{
    #region Private Fields

    private const float MinWind = 0.06f;
    private const float WindSpawnChance = 180f;
    private const int WindLoopChance = 10;

    private const int Margin = 100;

    #endregion

        // This should be excessive.
    public const int WindCount = 40;
    public static readonly WindParticle[] Winds = new WindParticle[WindCount];

    #region Loading

    public override void Load()
    {
        Array.Clear(Winds);
        On_Main.UpdateWeather += UpdateWind;
    }

    public override void Unload() => On_Main.UpdateWeather -= UpdateWind;

    #endregion

    private void UpdateWind(On_Main.orig_UpdateWeather orig, Main self, GameTime gameTime, int currentDayRateIteration)
    {
        orig(self, gameTime, currentDayRateIteration);

        if (!SkyConfig.Instance.WindParticles)
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
        if (!Main.rand.NextBool((int)(50 / MathF.Abs(Main.WindForVisuals))))
            return;

        int index = Array.FindIndex(Winds, w => !w.IsActive);

        if (index == -1)
            return;

        Vector2 screensize = MiscUtils.ScreenSize;

        Rectangle spawn = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y,
            (int)(screensize.X - (screensize.X * Main.WindForVisuals)), (int)screensize.Y);

        spawn.Inflate(Margin, Margin);

        Vector2 position = Main.rand.NextVector2FromRectangle(spawn);

        Winds[index] = WindParticle.CreateActive(position, Main.rand.NextBool(WindLoopChance));
    }
}
