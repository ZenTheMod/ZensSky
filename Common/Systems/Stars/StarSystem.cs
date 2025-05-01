using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria;
using ZensSky.Common.DataStructures;
using static ZensSky.Common.DataStructures.InteractableStar;

namespace ZensSky.Common.Systems.Stars;

public enum SupernovaProgress : byte
{
    None = 0,
    Shrinking = 1,
    Exploding = 2,
    Destroyed = 3
}

public sealed class StarSystem : ModSystem
{
    public static bool CanDrawStars { get; private set; }
    public const int StarCount = 900;
    public static readonly InteractableStar[] Stars = new InteractableStar[StarCount];
    public static readonly byte[] Supernovae = new byte[StarCount];
    public static float StarRotation { get; private set; }
    public static float StarAlpha { get; private set; }
    private const string SupernovaeTagKey = "Supernovae";
    private const string RotationTagKey = "StarRotation";
    private const float DawnTime = 6700f;
    private const float DuskStartTime = 48000f;
    private const float DayLength = 54000f;
    private const float MainMenuDayRateDivisor = 10000f;
    private const float GameDayRateDivisor = 70000f;
    private const float GraveyardAlphaMultiplier = 1.4f;

    private const int DefaultStarGenerationSeed = 100;
    private static int StarGenerationSeed;

    public override void Load()
    {
        StarGenerationSeed = DefaultStarGenerationSeed;
        GenerateStars();
        On_Star.UpdateStars += UpdateStarFields;
    }

    public override void Unload() => On_Star.UpdateStars -= UpdateStarFields;

    private void UpdateStarFields(On_Star.orig_UpdateStars orig)
    {
        if (!CanDrawStars)
        {
            orig();
            return;
        }

        float dayRateDivisor = Main.gameMenu ? MainMenuDayRateDivisor : GameDayRateDivisor;
        StarRotation += (float)(Main.dayRate / dayRateDivisor);

        StarAlpha = CalculateStarAlpha();

            // ShootingStarSystem.Update();
    }

    public override void PostSetupContent() => CanDrawStars = true;

    public override void OnWorldLoad()
    {
        StarGenerationSeed = Main.worldID;
        GenerateStars();
    }

    public override void OnWorldUnload()
    {
        StarGenerationSeed = DefaultStarGenerationSeed;
        GenerateStars();
    }

    public override void ClearWorld()
    {
        StarGenerationSeed = DefaultStarGenerationSeed;
        GenerateStars();
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag[SupernovaeTagKey] = Supernovae;
        tag[RotationTagKey] = StarRotation;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (tag.TryGet(SupernovaeTagKey, out byte[] supernovaeData) && supernovaeData.Length == StarCount)
            Array.Copy(supernovaeData, Supernovae, StarCount);

        if (tag.TryGet(RotationTagKey, out float rotation))
            StarRotation = rotation;
        else
            StarRotation = 0f;
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Supernovae);
        writer.Write(StarRotation);
    }

    public override void NetReceive(BinaryReader reader)
    {
        try
        {
            byte[] supernovaeData = reader.ReadBytes(StarCount);

            if (supernovaeData.Length == StarCount)
                Array.Copy(supernovaeData, Supernovae, StarCount);

            StarRotation = reader.ReadSingle();
        }
        catch (Exception ex)
        {
            ModContent.GetInstance<ZensSky>().Logger.Error($"error: {ex.Message}");
        }
    }

    private static void GenerateStars()
    {
        UnifiedRandom rand = new(StarGenerationSeed);

        ResetSky();

        for (int i = 0; i < StarCount; i++)
            Stars[i] = CreateRandom(rand);
    }

    private static void ResetSky()
    {
        StarRotation = 0f;

        if (Supernovae != null)
            Array.Clear(Supernovae, 0, StarCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateStarAlpha()
    {
        float alpha;

        if (Main.dayTime)
        {
            if (Main.time < DawnTime)
                alpha = (float)(1f - (Main.time / DawnTime));
            else if (Main.time > DuskStartTime)
                alpha = (float)((Main.time - DuskStartTime) / (DayLength - DuskStartTime));
            else
                alpha = 0f;
        }
        else
            alpha = 1f;

        if (Main.shimmerAlpha > 0f)
            alpha *= 1f - Main.shimmerAlpha;

        if (Main.GraveyardVisualIntensity > 0f)
            alpha *= 1f - Main.GraveyardVisualIntensity * GraveyardAlphaMultiplier;

        float atmosphericBoost = MathF.Pow(1f - Main.atmo, 3f);

        return MathHelper.Clamp(alpha + atmosphericBoost, 0f, 1f);
    }
}
