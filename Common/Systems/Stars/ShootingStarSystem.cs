using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using ZensSky.Common.DataStructures;
using ZensSky.Common.Utilities;
using ZensSky.Core;
using ZensSky.Core.Exceptions;

namespace ZensSky.Common.Systems.Stars;

[Autoload(Side = ModSide.Client)]
public sealed class ShootingStarSystem : ModSystem
{
    #region Private Fields

    private const int Margin = 140;

    #endregion

    #region Public Fields

    public const int ShootingStarCount = 70;
    public static readonly ShootingStar[] ShootingStars = new ShootingStar[ShootingStarCount];

    #endregion

    #region Public Properties

    public static bool ShowShootingStars {  get; set; }

    #endregion

    #region Loading

    public override void Load()
    {
        Array.Clear(ShootingStars);

        MainThreadSystem.Enqueue(() => {
            IL_Main.DoUpdate += ModifyInGameStarFall;
            IL_Main.UpdateMenu += ModifyMenuStarFall;
            On_Star.StarFall += ModifyFallingStarSpawn;
        });
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() => {
            IL_Main.DoUpdate -= ModifyInGameStarFall;
            IL_Main.UpdateMenu -= ModifyMenuStarFall;
            On_Star.StarFall -= ModifyFallingStarSpawn;
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
            throw new ILEditException(Mod, il, e);
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
            throw new ILEditException(Mod, il, e);
        }
    }


    private void ModifyFallingStarSpawn(On_Star.orig_StarFall orig, float positionX)
    {
        if (!ZensSky.CanDrawSky || !ShowShootingStars)
        {
            orig(positionX);
            return;
        }

        Terraria.Star.starFallCount++;
        SpawnShootingStar();
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

    public static void Update()
    {
        for (int i = 0; i < ShootingStarCount; i++)
            if (ShootingStars[i].IsActive)
                ShootingStars[i].Update();
    }

    public static void StarGameUpdate()
    {
        for (int i = 0; i < ShootingStarCount; i++)
            if (ShootingStars[i].IsActive)
                ShootingStars[i].StarGameUpdate();
    }

    #endregion
}
