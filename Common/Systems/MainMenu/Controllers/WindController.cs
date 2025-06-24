using Terraria;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.MainMenu.Elements;

namespace ZensSky.Common.Systems.MainMenu.Controllers;

public sealed class WindController : SliderController
{
    #region Properties

    public override float MaxRange => 1f;
    public override float MinRange => -1f;

    public override Color InnerColor => Color.Gray;

    public override ref float Modifying => ref MenuConfig.Instance.Wind;

    public override int Index => 3;

    public override string Name => "Mods.ZensSky.MenuController.Wind";

    #endregion

    public override void Refresh() 
    {
        if (!MenuConfig.Instance.UseWind)
            return;

        Main.windSpeedCurrent = MenuConfig.Instance.Wind;
        Main.windSpeedTarget = MenuConfig.Instance.Wind;
    }

    public override void OnSet() => MenuConfig.Instance.UseWind = true;
}
