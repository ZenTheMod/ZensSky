using Terraria.ModLoader.Config;

namespace ZensSky.Core.Config.Elements;

public sealed class LockedKeyAttribute : ConfigKeyAttribute
{
    public LockedKeyAttribute(string key)
        : base(key) {}
}
