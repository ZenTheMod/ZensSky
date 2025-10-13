using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ZensSky.Core.Config;

[Autoload(Side = ModSide.Client)]
public sealed class HideConfigSystem : ModSystem
{
    public override void PostSetupContent() =>
        ConfigManager.Configs[Mod].RemoveAll(m => m.GetType().IsDefined(typeof(HideConfigAttribute)));
}
