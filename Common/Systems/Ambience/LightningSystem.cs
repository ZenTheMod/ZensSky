using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Ambience;

public sealed class LightningSystem : ModSystem
{
    #region Private Fields

    private static bool ShouldBeStormy;

    #endregion

    #region Loading

    public override void Load() => 
        MainThreadSystem.Enqueue(() => IL_Main.UpdateMenu += UpdateLightning);

    public override void Unload() => 
        MainThreadSystem.Enqueue(() => IL_Main.UpdateMenu -= UpdateLightning);

    #endregion

    #region Updating

    private void UpdateLightning(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel skipLightningResets = c.DefineLabel();

            c.GotoNext(MoveType.Before,
                i => i.MatchLdcI4(0),
                i => i.MatchStsfld<Main>(nameof(Main.thunderDelay)));

            c.EmitDelegate(() => MenuConfig.Instance.Rain > 0);

            c.EmitBrtrue(skipLightningResets);

            c.GotoNext(MoveType.After,
                i => i.MatchLdcR4(0f),
                i => i.MatchStsfld<Main>(nameof(Main.lightningSpeed)));

            c.MarkLabel(skipLightningResets);

                // TODO: Simplify this logic if possible.
            c.EmitDelegate(() =>
            {
                if (MenuConfig.Instance.Rain <= 0)
                    return;

                float wind = Math.Abs(Main.windSpeedTarget);

                if (Main.cloudAlpha < Main._minRain || wind < Main._minWind)
                    ShouldBeStormy = false;
                else if (Main.cloudAlpha >= Main._maxRain && wind >= Main._maxWind)
                    ShouldBeStormy = true;

                if (Main.thunderDelay >= 0)
                    Main.thunderDelay--;
                if (Main.thunderDelay == 0)
                {
                        // Use the screen position rather than player position as the screen is used to calculate audio volume.
                    Vector2 position = Main.screenPosition;

                    float direction = Main.thunderDistance * 15;

                    if (Main.rand.NextBool())
                        direction *= -1f;

                    position.X += direction;

                    SoundEngine.PlaySound(SoundID.Thunder, position);
                }

                if (Main.lightningSpeed > 0f)
                {
                    Main.lightning += Main.lightningSpeed;
                    if (Main.lightning >= 1f)
                    {
                        Main.lightning = 1f;
                        Main.lightningSpeed = 0f;
                    }
                }
                else if (Main.lightning > 0f)
                    Main.lightning -= Main.lightningDecay;
                else if (Main.thunderDelay <= 0)
                {
                    if (ShouldBeStormy)
                    {
                        float chance = 600f * (1f - (Main.maxRaining * wind) + 1f);

                        if (Main.rand.NextBool((int)chance))
                            Main.NewLightning();
                    }
                }
            });
        }
        catch (Exception e)
        {
            throw new ILEditException(Mod, il, e);
        }
    }

    #endregion
}
