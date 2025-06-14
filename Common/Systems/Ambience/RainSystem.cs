using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ZensSky.Common.Systems.Ambience;

[Autoload(Side = ModSide.Client)]
public sealed class RainSystem : ModSystem
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
            IL_Main.DoDraw += DontDegradeRain;
            IL_Main.UpdateAudio += RainWindAmbience;

            On_Main.DrawBackgroundBlackFill += DrawMenuRain;
        });
    }
    public override void Unload()
    {
        Main.QueueMainThreadAction(() => {
            IL_Main.DoUpdate -= SpawnMenuRain;
            IL_Main.DoDraw -= DontDegradeRain;
            IL_Main.UpdateAudio -= RainWindAmbience;

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
                if (Main.cloudAlpha <= 0)
                    return;

                float num = Main.screenWidth / MagicScreenWidth;
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
            Mod.Logger.Error("Failed to patch \"Main.DoUpdate\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    private void DontDegradeRain(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdloc2(),
                i => i.MatchLdcR4(20));

            c.EmitPop();

            c.EmitLdcR4(0f);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"Main.DoDraw\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    #endregion

    #region Menu Touchups

    private void RainWindAmbience(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? jumpMenuCheck = c.DefineLabel();
            
                // Rain.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)),
                i => i.MatchBrfalse(out jumpMenuCheck),
                i => i.MatchLdcR4(0));

            c.EmitBr(jumpMenuCheck);

                // Wind.
            c.GotoNext(MoveType.After,
                i => i.MatchStelemR4(),
                i => i.MatchBr(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.gameMenu)));

            c.EmitPop();
            c.EmitLdcI4(0);
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error("Failed to patch \"Main.UpdateAudio\".");

            throw new ILPatchFailureException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    private void DrawMenuRain(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
    {
        orig(self);

        if (!Main.gameMenu)
            return;

        self.DrawRain();
    }

    #endregion
}
