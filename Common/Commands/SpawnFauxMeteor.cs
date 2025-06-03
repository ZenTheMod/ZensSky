using Terraria;
using Terraria.GameContent.Ambience;
using Terraria.GameContent.Skies;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace ZensSky.Common.Commands;

public sealed class SpawnFauxMeteor : ModCommand
{
    public override CommandType Type => CommandType.World;

    public override string Command => "spawnFauxMeteor";

    public override string Usage => string.Empty;

    public override string Description => string.Empty;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        ((AmbientSky)SkyManager.Instance["Ambience"]).Spawn(Main.LocalPlayer, SkyEntityType.Meteor, Main.rand.Next(700));
    }
}
