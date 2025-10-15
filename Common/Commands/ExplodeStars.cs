using Terraria.ModLoader;
using ZensSky.Common.Systems.Sky.Space;

namespace ZensSky.Common.Commands;

public sealed class ExplodeStars : ModCommand
{
    public override CommandType Type => CommandType.World;

    public override string Command => "explodeStars";

    public override string Usage => string.Empty;

    public override string Description => string.Empty;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length != 1)
            return;

        if (!int.TryParse(args[0], out int count))
            return;

        SupernovaSystem.ResetSupernovae();

        StarSystem.GenerateStars();

        for (int i = 0; i < count; i++)
            SupernovaSystem.CreateSupernova();
    }
}
