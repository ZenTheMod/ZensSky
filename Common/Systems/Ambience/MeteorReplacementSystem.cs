using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent.Skies;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ZensSky.Common.Systems.Ambience;

[Autoload(Side = ModSide.Client)]
public sealed class MeteorReplacementSystem : ModSystem
{
    public override void Load() => Main.QueueMainThreadAction(() => IL_AmbientSky.Spawn += ModifyMeteorSpawn);

    private void ModifyMeteorSpawn(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            int playerIndex = -1;
            int randomIndex = -1;

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(out playerIndex),
                i => i.MatchLdloc(out randomIndex),
                i => i.MatchNewobj<AmbientSky.MeteorSkyEntity>());

            c.EmitPop();

            c.EmitLdarg(playerIndex);
            c.EmitLdloc(randomIndex);

            c.EmitDelegate((Player player, FastRandom random) => new FancyMeteor(player, random));
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"AmbientSky.Spawn\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }
}
