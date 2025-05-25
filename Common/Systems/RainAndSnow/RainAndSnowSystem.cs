using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ZensSky.Common.Systems.RainAndSnow;

public sealed class RainAndSnowSystem : ModSystem
{
    #region Private Fields

    private const int Margin = 600;
    private const float MagicScreenWidth = 1920f;
    private const float WindOffset = 600f;

    #endregion

    #region Loading

    public override void Load()
    {
        Main.QueueMainThreadAction(() => {
            IL_Main.DoUpdate += SpawnMenuRain;
            On_Main.DrawBackgroundBlackFill += DrawMenuRain;
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => {
            IL_Main.DoUpdate -= SpawnMenuRain;
            On_Main.DrawBackgroundBlackFill -= DrawMenuRain;
        });
    }

    #endregion

    #region Updating

    private void SpawnMenuRain(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.Before,
                i => i.MatchCall<Main>(nameof(Main.UpdateMenu)),
                i => i.MatchLdsfld<Main>(nameof(Main.netMode)));

            c.EmitDelegate(() =>
            {
                    // Main.cloudAlpha = 1f;

                if (Main.cloudAlpha <= 0)
                    return;

                float num = Main.screenWidth / MagicScreenWidth;
                num *= 25f;
                num *= 0.25f + 1f * Main.cloudAlpha;

                Vector2 position = Main.screenPosition;

                for (int i = 0; i < num; i++)
                {
                    Vector2 vector = new(Main.rand.Next((int)position.X - Margin, (int)position.X + Main.screenWidth + Margin),
                        position.Y - Main.rand.Next(20, 100));

                    vector.X -= Main.WindForVisuals * WindOffset;

                    Vector2 rainFallVelocity = Rain.GetRainFallVelocity();
                    Rain.NewRain(vector, rainFallVelocity);
                }
            });
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error("Failed to patch \"Main.DoUpdate\".");

            throw new ILPatchFailureException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    #endregion

    private void DrawMenuRain(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
    {
        orig(self);

        if (!Main.gameMenu)
            return;

        self.DrawRain();
    }
}
