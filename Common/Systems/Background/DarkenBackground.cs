using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Stars;

namespace ZensSky.Common.Systems.Background;

[Autoload(Side = ModSide.Client)]
public sealed class DarkenBackground : ModSystem
{
    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (SkyConfig.Instance.PitchBlackBackground)
            backgroundColor = Color.Lerp(Main.ColorOfTheSkies, Color.Black, StarSystem.StarAlpha);
    }
}
