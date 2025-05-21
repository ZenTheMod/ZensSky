using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria;
using ZensSky.Common.DataStructures;
using System.Linq;
using static ZensSky.Common.DataStructures.InteractableStar;

namespace ZensSky.Common.Systems.Stars;

public sealed class StarSystem : ModSystem
{
    #region Private Fields

    private const float DawnTime = 6700f;
    private const float DuskStartTime = 48000f;
    private const float DayLength = 54000f;
    private const float MainMenuDayRateDivisor = 10000f;
    private const float GameDayRateDivisor = 70000f;
    private const float GraveyardAlphaMultiplier = 1.4f;

    private const int DefaultStarGenerationSeed = 100;
    private static int StarGenerationSeed;

    private const string SupernovaeCount = "SupernovaeCount";

    private const float CompressionIncrement = 0.002f;

    private const float ExplosionIncrement = 0.00004f;

    #endregion

    #region Public Fields

    public static float TemporaryStarAlpha { get; set; }

    public static bool CanDrawStars { get; private set; }

    public const int StarCount = 1200;
    public static readonly InteractableStar[] Stars = new InteractableStar[StarCount];

    public static float StarRotation { get; private set; }
    public static float StarAlpha { get; private set; }

    #endregion

    #region Loading

    public override void Load()
    {
        StarGenerationSeed = DefaultStarGenerationSeed;
        GenerateStars();
        On_Star.UpdateStars += UpdateStars;
    }

    public override void Unload() => On_Star.UpdateStars -= UpdateStars;

    public override void PostSetupContent() => CanDrawStars = true;

    #endregion

    #region Updating

    private void UpdateStars(On_Star.orig_UpdateStars orig)
    {
        if (!CanDrawStars)
        {
            orig();
            return;
        }

        float dayRateDivisor = Main.gameMenu ? MainMenuDayRateDivisor : GameDayRateDivisor;
        StarRotation += (float)(Main.dayRate / dayRateDivisor);

        if (TemporaryStarAlpha != -1)
            StarAlpha = TemporaryStarAlpha;
        else
            StarAlpha = CalculateStarAlpha();

        TemporaryStarAlpha = -1;

        UpdateSupernovae();

            // ShootingStarSystem.Update();
    }

    private static void UpdateSupernovae()
    {
        if (!Stars.Any(s => s.SupernovaProgress > SupernovaProgress.None))
            return;

        for (int i = 0; i < StarCount; i++)
        {
            InteractableStar star = Stars[i];

            if (star.SupernovaProgress == SupernovaProgress.None)
                continue;

            switch (star.SupernovaProgress)
            {
                case SupernovaProgress.Shrinking:
                    {
                        Stars[i].SupernovaTimer += CompressionIncrement;

                        if (Stars[i].SupernovaTimer < 1f)
                            break;

                        Stars[i].SupernovaTimer = 0f;
                        Stars[i].SupernovaProgress = SupernovaProgress.Exploding;

                        break;
                    }
                case SupernovaProgress.Exploding:
                    {
                        Stars[i].SupernovaTimer += ExplosionIncrement;

                        if (Stars[i].SupernovaTimer < 1f)
                            break;

                        Stars[i].SupernovaTimer = 0f;
                        Stars[i].SupernovaProgress = SupernovaProgress.Regenerating;

                        break;
                    }
                case SupernovaProgress.Regenerating:
                    break;  // TODO: Logic for this.
                default:
                    break;
            }
        }
    }

    #endregion

    #region Saving and Syncing

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
        tag[nameof(StarRotation)] = StarRotation;

        int count = Stars.Count(s => s.SupernovaProgress > SupernovaProgress.None);
        tag[SupernovaeCount] = count;

        int index = 0;

        ReadOnlySpan<InteractableStar> starSpan = Stars.AsSpan();
        for (int i = 0; i < starSpan.Length; i++)
        {
            InteractableStar star = starSpan[i];

            if (star.SupernovaProgress == SupernovaProgress.None)
                continue;

            tag[nameof(Stars) + index] = i;
            tag[nameof(InteractableStar.SupernovaProgress) + index] = (byte)star.SupernovaProgress;
            tag[nameof(InteractableStar.SupernovaTimer) + index] = star.SupernovaTimer;

            index++;
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        try
        {
            StarRotation = tag.Get<float>(nameof(StarRotation));

            int count = tag.Get<int>(SupernovaeCount);

            for (int i = 0; i < count; i++)
            {
                int index = tag.Get<int>(nameof(Stars) + i);

                Stars[index].SupernovaProgress = (SupernovaProgress)tag.Get<byte>(nameof(InteractableStar.SupernovaProgress) + i);
                Stars[index].SupernovaTimer = tag.Get<float>(nameof(InteractableStar.SupernovaTimer) + i);
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"Failed to load stars: {ex.Message}");
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
            // Because this mod uses 'side = NoSync' in the build.txt file we have to account for it.
        if (!ModContent.GetInstance<ZensSky>().IsNetSynced)
            return;

        writer.Write(StarRotation);

        int count = Stars.Count(s => s.SupernovaProgress > SupernovaProgress.None);
        writer.Write7BitEncodedInt(count);

        ReadOnlySpan<InteractableStar> starSpan = Stars.AsSpan();
        for (int i = 0; i < starSpan.Length; i++)
        {
            InteractableStar star = starSpan[i];

            if (star.SupernovaProgress == SupernovaProgress.None)
                continue;

            writer.Write7BitEncodedInt(i);
            writer.Write((byte)star.SupernovaProgress);
            writer.Write(star.SupernovaTimer);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        try
        {
            StarRotation = reader.ReadSingle();

            int count = reader.Read7BitEncodedInt();

            for (int i = 0; i < count; i++)
            {
                int index = reader.Read7BitEncodedInt();

                Stars[index].SupernovaProgress = (SupernovaProgress)reader.ReadByte();
                Stars[index].SupernovaTimer = reader.ReadSingle();
            }
        }
        catch (Exception ex)
        {
            Main.NewText($"Failed to sync stars: {ex.Message}", Color.Red);
            Mod.Logger.Error($"Failed to sync stars: {ex.Message}");
        }
    }

    #endregion

    public static void ExplodeStar(int index) => 
        Stars[index].SupernovaProgress |= SupernovaProgress.Shrinking;

    public static void GenerateStars()
    {
        if (Main.dedServ)
        {
            Array.Clear(Stars, 0, Stars.Length);
            return; 
        }

        UnifiedRandom rand = new(StarGenerationSeed);

        ResetSky();

        for (int i = 0; i < StarCount; i++)
            Stars[i] = CreateRandom(rand);
    }

    private static void ResetSky()
    {
        StarRotation = 0f;

        if (Stars != null)
        {
            for (int i = 0; i < StarCount; i++)
            {
                Stars[i].SupernovaProgress = SupernovaProgress.None;
                Stars[i].SupernovaTimer = 0f;
            }
        }
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
