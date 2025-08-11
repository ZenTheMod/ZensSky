using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZensSky.Common.Config;
using ZensSky.Common.Systems.Menu.Elements;
using ZensSky.Core.Exceptions;
using ZensSky.Core.Systems;

namespace ZensSky.Common.Systems.Menu.Controllers;

public sealed class TimeController : SliderController
{
    #region Properties

    public override float MaxRange => 20f;
    public override float MinRange => -20f;

    public override Color InnerColor => Color.MediumPurple;

    public override ref float Modifying => ref MenuConfig.Instance.TimeMultiplier;

    public override int Index => 6;

    public override string Name => "Mods.ZensSky.MenuController.Time";

    #endregion

    #region Loading

    public override void OnLoad() => 
        MainThreadSystem.Enqueue(() => IL_Main.UpdateMenu += ModifyMenuTime);

    public override void OnUnload() => 
        MainThreadSystem.Enqueue(() => IL_Main.UpdateMenu -= ModifyMenuTime);

    private void ModifyMenuTime(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

                // Fixes moon type changing mid day, which can look weird during a solar eclipse.
            ILLabel skipRandomMoonType = c.DefineLabel();

            c.GotoNext(MoveType.Before,
                i => i.MatchCall<Main>($"get_{nameof(Main.rand)}"),
                i => i.MatchLdcI4(9));

            c.EmitBr(skipRandomMoonType);

            c.GotoNext(MoveType.After,
                i => i.MatchStsfld<Main>(nameof(Main.moonType)));

            c.MarkLabel(skipRandomMoonType);

                // Handle negative time.
            c.GotoNext(MoveType.AfterLabel,
                i => i.MatchBrfalse(out _),
                i => i.MatchRet());

            c.EmitDelegate(() =>
            {
                if (Main.time >= 0)
                    return;

                if (Main.dayTime)
                    Main.moonPhase = (Main.moonPhase - 1) % 8;

                Main.time = (Main.dayTime ? Main.nightLength : Main.dayLength) - 1;
                Main.dayTime = !Main.dayTime;

                    // Prevent changing moon type while loading into a world.
                if (!Main.lockMenuBGChange)
                    Main.moonType = Main.rand.Next(TextureAssets.Moon.Length);
            });

                // Change the speed of time.
            c.GotoNext(MoveType.After,
                i => i.MatchLdcR8(33.88235294117647)); // I shit you not this is the hardcoded value.

            c.EmitDelegate((double time) => time * MenuConfig.Instance.TimeMultiplier);

            c.GotoNext(MoveType.After,
                i => i.MatchLdcR8(30.857142857142858));

            c.EmitDelegate((double time) => time * MenuConfig.Instance.TimeMultiplier);

                // Refresh the moon type when the time is set to night.
            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Main>(nameof(Main.moonPhase)),
                i => i.MatchLdcI4(1));

                // No MoveType.BeforeLabels :pensive:.
            c.MoveAfterLabels();

            c.EmitDelegate(() => 
            {
                    // Prevent changing moon type while loading into a world.
                if (!Main.lockMenuBGChange)
                    Main.moonType = Main.rand.Next(TextureAssets.Moon.Length);
            });

                // Vanilla moon cycles through moon phases 1-7. :agony:
            c.GotoNext(MoveType.Before,
                i => i.MatchBlt(out _),
                i => i.MatchLdcI4(0));

            c.EmitPop();

            c.EmitLdcI4(8);
        }
        catch (Exception e)
        {
            throw new ILEditException(ModContent.GetInstance<ZensSky>(), il, e);
        }
    }

    #endregion
}
