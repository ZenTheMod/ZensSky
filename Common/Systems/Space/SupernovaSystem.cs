using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ZensSky.Core.Systems.ModCall;
using ZensSky.Core.Systems.Net;
using static ZensSky.Common.Systems.Space.StarHooks;
using static ZensSky.Common.Systems.Space.StarSystem;
using static ZensSky.Core.Systems.Net.NetMessageHooks;
using Star = ZensSky.Common.DataStructures.Star;
using Supernova = ZensSky.Common.DataStructures.Supernova;

namespace ZensSky.Common.Systems.Space;

public sealed class SupernovaSystem : ModSystem, IPacketHandler
{
    #region Private Fields

    private const string ActiveSupernovaeCountKey = "ActiveCount";

    #endregion

    #region Public Fields

    public const int SupernovaeCount = StarCount;

    public static readonly Supernova[] Supernovae = new Supernova[StarCount];

    #endregion

    #region Loading

    public override void Load()
    {
        Array.Clear(Supernovae);

        UpdateStars += Update;

        OnSyncWorldData += WorldDataSupernovae;
    }

    #endregion

    #region Spawning

    [ModCall("TriggerSupernova", "ExplodeStar", "SpawnSupernova")]
    public static void CreateSupernova(int index = -1, Color? supernovaColor = null, float nebulaHue = -1f, bool shouldSync = true)
    {
        if (index < 0 || index >= SupernovaeCount)
            index = Main.rand.Next(SupernovaeCount);

        if (Supernovae[index].IsActive)
            return;

        unsafe
        {
            fixed (Star* star = &Stars[index])
                Supernovae[index] = new(star, supernovaColor, nebulaHue);
        }

        if (shouldSync)
            PacketSystem.Send<SupernovaSystem>(ignoreClient: Main.myPlayer);
    }

    #endregion

    #region Updating

    public static void Update()
    {
        for (int i = 0; i < Supernovae.Length; i++)
            if (Supernovae[i].IsActive)
                Supernovae[i].Update();
    }

    #endregion

    #region Saving and Syncing

    public override void OnWorldLoad() =>
        ResetSupernovae();

    public override void OnWorldUnload() =>
        ResetSupernovae();

    public override void ClearWorld() =>
        ResetSupernovae();

    #region World Data

    public override void SaveWorldData(TagCompound tag)
    {
        int count = Supernovae.Count(s => s.IsActive);
        tag[ActiveSupernovaeCountKey] = count;

        int index = 0;

        ReadOnlySpan<Supernova> supernovaeSpan = Supernovae;

        for (int i = 0; i < supernovaeSpan.Length; i++)
        {
            Supernova supernova = supernovaeSpan[i];

            if (!supernova.IsActive)
                continue;

            tag[nameof(Supernovae) + index] = i;

                // Store color as its packed value to save space.
            tag[nameof(Supernova.SupernovaColor) + index] = supernova.SupernovaColor.PackedValue;

            tag[nameof(Supernova.NebulaHue) + index] = supernova.NebulaHue;

            tag[nameof(Supernova.Contract) + index] = supernova.Contract;
            tag[nameof(Supernova.Expand) + index] = supernova.Expand;
            tag[nameof(Supernova.Decay) + index] = supernova.Decay;

            index++;
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        try
        {
            int count = tag.Get<int>(ActiveSupernovaeCountKey);

            for (int i = 0; i < count; i++)
            {
                int index = tag.Get<int>(nameof(Supernovae) + i);

                    // Load the color from the packed value.
                Color supernovaColor = new(tag.Get<uint>(nameof(Supernova.SupernovaColor) + i));

                float nebulaHue = tag.Get<float>(nameof(Supernova.NebulaHue) + i);

                    // Create a new active supernova.
                CreateSupernova(index, supernovaColor, nebulaHue);

                float contract = tag.Get<float>(nameof(Supernova.Contract) + i);
                float expand = tag.Get<float>(nameof(Supernova.Expand) + i);
                float decay = tag.Get<float>(nameof(Supernova.Decay) + i);

                Supernovae[index].Contract = contract;
                Supernovae[index].Expand = expand;
                Supernovae[index].Decay = decay;
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"Failed to load supernovae: {ex.Message}");
        }
    }

    #endregion

    #region Net Syncing

    private void WorldDataSupernovae(int toClient, int ignoreClient) =>
        PacketSystem.Send<SupernovaSystem>(toClient, ignoreClient);

    void IPacketHandler.Write(BinaryWriter writer)
    {
        if (!Mod.IsNetSynced)
            return;

        int count = Supernovae.Count(s => s.IsActive);
        writer.Write7BitEncodedInt(count);

        ReadOnlySpan<Supernova> supernovaeSpan = Supernovae;

        for (int i = 0; i < supernovaeSpan.Length; i++)
        {
            Supernova supernova = supernovaeSpan[i];

            if (!supernova.IsActive)
                continue;

            writer.Write7BitEncodedInt(i);

                // Store color as its packed value to save space.
            writer.Write(supernova.SupernovaColor.PackedValue);

            writer.Write(supernova.NebulaHue);

            writer.Write(supernova.Contract);
            writer.Write(supernova.Expand);
            writer.Write(supernova.Decay);
        }
    }

    void IPacketHandler.Receive(BinaryReader reader)
    {
        if (!Mod.IsNetSynced)
            return;

        try
        {
            int count = reader.Read7BitEncodedInt();

            for (int i = 0; i < count; i++)
            {
                int index = reader.Read7BitEncodedInt();

                    // Load the color from the packed value.
                Color supernovaColor = new(reader.ReadUInt32());

                float nebulaHue = reader.ReadSingle();

                    // Create a new active supernova.
                CreateSupernova(index, supernovaColor, nebulaHue);

                float contract = reader.ReadSingle();
                float expand = reader.ReadSingle();
                float decay = reader.ReadSingle();

                Supernovae[index].Contract = contract;
                Supernovae[index].Expand = expand;
                Supernovae[index].Decay = decay;
            }
        }
        catch (Exception ex)
        {
            Main.NewText($"Failed to sync stars: {ex.Message}", Color.Red);
            Mod.Logger.Error($"Failed to sync stars: {ex.Message}");
        }
    }

    #endregion

    #endregion

    #region Public Methods

    [ModCall("ResetSupernova", "ResetSupernovas",
        "ClearSupernova", "ClearSupernovas", "ClearSupernovae")]
    public static void ResetSupernovae() =>
        Array.Clear(Supernovae);

    #endregion
}
