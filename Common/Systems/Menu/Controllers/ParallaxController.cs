using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Menu.Elements;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Menu.Controllers;

public sealed class ParallaxController : SliderController
{
    #region Properties

    public override float MaxRange => 5f;
    public override float MinRange => -5f;

    public override Color InnerColor => Color.CornflowerBlue;

    public override ref float Modifying => ref MenuConfig.Instance.Parallax;

    public override int Index => 2;
    public override string Name => "Mods.ZensSky.MenuController.Parallax";

    #endregion

    #region Loading

    public override void OnLoad() => 
        MainThreadSystem.Enqueue(() => IL_Main.DrawMenu += ChangeParallaxDirection);

    public override void OnUnload() => 
        MainThreadSystem.Enqueue(() => IL_Main.DrawMenu -= ChangeParallaxDirection);

    private void ChangeParallaxDirection(ILContext il)
    {
        try 
        { 
            ILCursor c = new(il);

            c.GotoNext(MoveType.Before,
                i => i.MatchStsfld<Main>(nameof(Main.MenuXMovement)));

            c.EmitPop();

            c.EmitDelegate(() => MenuConfig.Instance.Parallax);
        }
        catch (Exception e)
        {
            throw new ILEditException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    #endregion
}
