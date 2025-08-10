using Terraria.ModLoader.Config;

namespace ZensSky.Core.Config.Elements;

public sealed class LockedArgsAttribute : ConfigArgsAttribute
{
    public LockedArgsAttribute(params object[] args)
        : base(args)
    {
    }
}
