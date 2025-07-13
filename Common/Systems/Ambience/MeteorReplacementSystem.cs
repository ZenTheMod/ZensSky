using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent.Skies;
using Terraria.ModLoader;
using Terraria.Utilities;
using ZensSky.Core.Exceptions;

namespace ZensSky.Common.Systems.Ambience;

[Autoload(Side = ModSide.Client)]
public sealed class MeteorReplacementSystem : ModSystem
{
    #region Loading

    public override void Load() => IL_AmbientSky.Spawn += ModifyMeteorSpawn;

    public override void Unload() => IL_AmbientSky.Spawn -= ModifyMeteorSpawn;

    #endregion

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
            throw new ILEditException(Mod, il, e);
        }
    }
}
