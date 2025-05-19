using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Systems.Stars;

namespace ZensSky.Common.Commands;

public sealed class ExplodeStars : ModCommand
{
    public override CommandType Type => CommandType.World;

    public override string Command => "explodeStars";

    public override string Usage => string.Empty;

    public override string Description => string.Empty;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0)
            return;

        if (int.TryParse(args[0], out int amount))
            for(int i = 0; i < amount; i++)
                StarSystem.ExplodeStar(Main.rand.Next(StarSystem.StarCount));
    }
}
