using Terraria;
using ZensSky.Common.Config;

namespace ZensSky.Common.Systems.MainMenu.Elements;

public sealed class RainController : SliderController
{
    #region Properties

    public override float MaxRange => 1f;
    public override float MinRange => 0f;

    public override Color InnerColor => Color.Blue;

    public override ref float Modifying => ref MenuConfig.Instance.Rain;

    public override int Index => 4;

    public override string Name => "Mods.ZensSky.MenuController.Rain";

    #endregion

    public override void Refresh() 
    {
        Main.maxRaining = MenuConfig.Instance.Rain;

        Main.cloudAlpha = MenuConfig.Instance.Rain;
    }
}
