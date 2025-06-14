using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Utilities;

namespace ZensSky.Common.Systems.Stars;

[Autoload(Side = ModSide.Client)]
public sealed class ShootingStarSystem : ModSystem
{
    #region Private Fields

    private const int Margin = 140;

    #endregion

    #region Public Fields

        // This should be excessive.
    public const int ShootingStarCount = 100;
    public static readonly ShootingStar[] ShootingStars = new ShootingStar[ShootingStarCount];

    #endregion

    #region Loading

    public override void Load()
    {
        Array.Clear(ShootingStars);

        Main.QueueMainThreadAction(() => {
            IL_Main.DoUpdate += ModifyInGameStarFall;
            IL_Main.UpdateMenu += ModifyMenuStarFall;
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => {
            IL_Main.DoUpdate -= ModifyInGameStarFall;
            IL_Main.UpdateMenu -= ModifyMenuStarFall;
        });
    }

    #endregion

    #region Spawning

    private void ModifyInGameStarFall(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? starFallSkipTarget = c.DefineLabel();

            c.GotoNext(MoveType.After,
                i => i.MatchLdcI4(90),
                i => i.MatchCallvirt<UnifiedRandom>(nameof(UnifiedRandom.Next)),
                i => i.MatchBrtrue(out starFallSkipTarget));

            c.MoveAfterLabels();

            c.EmitDelegate(SpawnShootingStar);

            c.EmitBr(starFallSkipTarget);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"Main.UpdateMenu\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    private void ModifyMenuStarFall(ILContext il)
    {
        try
        {
            ILCursor c = new(il);

            ILLabel? starFallSkipTarget = c.DefineLabel();

            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<WorldGen>(nameof(WorldGen.drunkWorldGen)),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdsfld<Main>(nameof(Main.remixWorld)),
                i => i.MatchBrtrue(out starFallSkipTarget));

            c.MoveAfterLabels();

            c.EmitDelegate(SpawnShootingStar);

            c.EmitBr(starFallSkipTarget);
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to patch \"Main.UpdateMenu\".");

            throw new ILPatchFailureException(Mod, il, e);
        }
    }

    public static void SpawnShootingStar()
    {
        int index = Array.FindIndex(ShootingStars, s => !s.IsActive);

        if (index == -1)
            return;

        Vector2 screensize = MiscUtils.ScreenSize;

        Rectangle spawn = new(0, 0,
            (int)screensize.X, (int)screensize.Y);

        spawn.Inflate(Margin, Margin);

        Vector2 position = Main.rand.NextVector2FromRectangle(spawn);

        ShootingStars[index] = ShootingStar.CreateActive(position, Main.rand);
    }

    #endregion

    #region Updating

    public static void UpdateShootingStars()
    {
        for (int i = 0; i < ShootingStarCount; i++)
            if (ShootingStars[i].IsActive)
                ShootingStars[i].Update();
    }

    #endregion
}
