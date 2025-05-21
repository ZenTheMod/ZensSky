using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.DataStructures;

namespace ZensSky.Common.Systems.Wind;

public sealed class WindRenderingSystem : ModSystem
{
    #region Loading

    public override void Load() => On_Main.DrawInfernoRings += DrawWind;

    public override void Unload() => On_Main.DrawInfernoRings -= DrawWind;

    #endregion

    private static void DrawWind(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        orig(self);

        if (!SkyConfig.Instance.WindParticles)
            return;

        foreach (WindParticle wind in WindSystem.Winds.Where(w => w.IsActive))
        {
            for (int i = 0; i < wind.OldPositions.Length - 1; i++)
            {
                if (wind.OldPositions[i + 1] == default)
                    continue;
                Utils.DrawLine(Main.spriteBatch, wind.OldPositions[i], wind.OldPositions[i + 1], Color.White, Color.White, 2f);
            }
        }
    }
}
