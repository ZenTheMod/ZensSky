using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using ZensSky.Core.Systems;
using ZensSky.Core.Systems.ModCall;
using ZensSky.Core.Utils;
using static ZensSky.Common.DataStructures.Star;
using Star = ZensSky.Common.DataStructures.Star;

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

    #endregion

    #region Public Fields

    public const int StarCount = 1200;
    public static readonly Star[] Stars = new Star[StarCount];

    #endregion

    #region Public Properties

    public static float StarRotation
    {
        [ModCall(nameof(StarRotation), $"Get{nameof(StarRotation)}")]
        get; 
        private set; 
    }

    public static float StarAlpha
    {
        [ModCall(nameof(StarAlpha), $"Get{nameof(StarAlpha)}")]
        get; 
        private set; 
    }

    public static float StarAlphaOverride
    {
        get;
        [ModCall($"Set{nameof(StarAlpha)}")]
        set;
    } = -1;

    #endregion

    #region Loading

    public override void Load()
    {
        GenerateStars();

        MainThreadSystem.Enqueue(() =>
            On_Star.UpdateStars += UpdateStars);
    }

    public override void Unload()
    {
        MainThreadSystem.Enqueue(() =>
            On_Star.UpdateStars -= UpdateStars);

        StarHooks.Clear();
    }

    #endregion

    #region Updating

    private void UpdateStars(On_Star.orig_UpdateStars orig)
    {
        if (!ZensSky.CanDrawSky)
        {
            orig();
            return;
        }

        float dayRateDivisor = Main.gameMenu ? MainMenuDayRateDivisor : GameDayRateDivisor;

        StarRotation += (float)(Main.dayRate / dayRateDivisor);

        StarRotation %= MathHelper.TwoPi;

        StarHooks.InvokeUpdateStars();
    }

    #endregion

    #region Saving and Syncing

    public override void OnWorldLoad() =>
        GenerateStars(Main.worldID);

    public override void OnWorldUnload() =>
        GenerateStars();

    public override void ClearWorld() =>
        GenerateStars();

    public override void SaveWorldData(TagCompound tag)
    {
        tag[nameof(StarRotation)] = StarRotation;

        int count = Stars.Count(s => s.Disabled);
        tag[$"{nameof(Star.Disabled)}Count"] = count;

        int index = 0;

        ReadOnlySpan<Star> starSpan = Stars.AsSpan();

        for (int i = 0; i < starSpan.Length; i++)
        {
            Star star = starSpan[i];

            if (!star.Disabled)
                continue;

            tag[nameof(Stars) + index] = i;

            index++;
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        try
        {
            StarRotation = tag.Get<float>(nameof(StarRotation));

            int count = tag.Get<int>($"{nameof(Star.Disabled)}Count");

            for (int i = 0; i < count; i++)
            {
                int index = tag.Get<int>(nameof(Stars) + i);

                Stars[index].Disabled = true;
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"Failed to load stars: {ex.Message}");
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        if (!Mod.IsNetSynced)
            return;

        writer.Write(StarRotation);

        int count = Stars.Count(s => s.Disabled);
        writer.Write7BitEncodedInt(count);

        ReadOnlySpan<Star> starSpan = Stars.AsSpan();

        for (int i = 0; i < starSpan.Length; i++)
        {
            Star star = starSpan[i];

            if (!star.Disabled)
                continue;

            writer.Write7BitEncodedInt(i);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        if (!Mod.IsNetSynced)
            return;
        
        try
        {
            StarRotation = reader.ReadSingle();

            int count = reader.Read7BitEncodedInt();

            for (int i = 0; i < count; i++)
            {
                int index = reader.Read7BitEncodedInt();

                Stars[index].Disabled = true;
            }
        }
        catch (Exception ex)
        {
            Main.NewText($"Failed to sync stars: {ex.Message}", Color.Red);
            Mod.Logger.Error($"Failed to sync stars: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    [MethodImpl(MethodImplOptions.NoInlining)]
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

        if (Main.gameMenu)
            Main.shimmerAlpha = 0f;

        alpha += Main.shimmerAlpha;

        if (Main.GraveyardVisualIntensity > 0f)
            alpha *= 1f - Main.GraveyardVisualIntensity * GraveyardAlphaMultiplier;

        float atmosphericBoost = Easings.InPolynomial(1f - Main.atmo, 3);

        return Utilities.Saturate(Easings.InPolynomial(alpha + atmosphericBoost, 3));
    }

    #endregion

    #region Public Methods

    [ModCall("RegenStars", "RegenerateStars")]
    public static void GenerateStars(int seed = DefaultStarGenerationSeed)
    {
        if (Main.dedServ)
        {
            Array.Clear(Stars, 0, Stars.Length);
            return; 
        }

        UnifiedRandom rand = new(seed);

        StarRotation = 0f;

        for (int i = 0; i < StarCount; i++)
            Stars[i] = new(rand);

        StarHooks.InvokeGenerateStars(rand, seed);
    }

    public static void UpdateStarAlpha()
    {
        StarAlpha = StarAlphaOverride == -1 ?
            CalculateStarAlpha() : StarAlphaOverride;

        StarAlphaOverride = -1;
    }

    #endregion
}
