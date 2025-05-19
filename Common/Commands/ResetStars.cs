using Terraria.ModLoader;
using ZensSky.Common.Systems.Stars;

namespace ZensSky.Common.Commands;

public sealed class ResetStars : ModCommand
{
    public override CommandType Type => CommandType.World;

    public override string Command => "resetStars";

    public override string Usage => string.Empty;

    public override string Description => string.Empty;

    public override void Action(CommandCaller caller, string input, string[] args) => StarSystem.GenerateStars();
}
