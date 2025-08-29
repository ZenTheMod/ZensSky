using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZensSky.Core.Systems.Net;

public sealed class PacketSystem : ModSystem
{
    #region Public Properties

    public static PacketSystem Instance => ModContent.GetInstance<PacketSystem>();

    public static List<IPacketHandler> Handlers { get; } = [];

    #endregion

    #region Public Methods

    /// <summary>
    /// Writes this <see cref="IPacketHandler"/> over the network to clients or the server.<br/>
    /// When called on a client, the data will be sent to the server and the optional parameters are ignored.<br/>
    /// When called on a server, the data will be sent to either all clients, all clients except a specific client, or just a specific client:<br/><br/>
    ///
    /// <code>
    ///     // Sends to all connected clients.
    /// PacketSystem.Send&lt;<typeparamref name="T"/>&gt;();
    ///     // Sends to <paramref name="toClient"/> only.
    /// PacketSystem.Send&lt;<typeparamref name="T"/>&gt;(<paramref name="toClient"/>: somePlayer.whoAmI);
    ///     // Sends to all other clients excluding <paramref name="ignoreClient"/>.
    /// PacketSystem.Send&lt;<typeparamref name="T"/>&gt;(<paramref name="ignoreClient"/>: somePlayer.whoAmI);
    /// </code>
    ///
    /// Typically if data is sent from a client to the server, the server will then need to relay this to all other clients to keep them in sync.<br/>
    /// This is when the <paramref name="ignoreClient"/> option will be used.
    /// </summary>
    /// <exception cref="KeyNotFoundException"></exception>
    public static void Send<T>(int toClient = -1, int ignoreClient = -1) where T : class, IPacketHandler
    {
        int index = Handlers.FindIndex(h => h == ModContent.GetInstance<T>());

        if (index == -1)
            throw new KeyNotFoundException($"Could not find '{typeof(T).FullName}' in '{nameof(Handlers)}!'");

        Send(Instance.Mod, index, toClient, ignoreClient);
    }

    public static void Handle(Mod mod, BinaryReader reader, int whoAmI)
    {
        if (Main.netMode == NetmodeID.SinglePlayer ||
            !mod.IsNetSynced)
            return;

        int index = reader.ReadInt32();

        IPacketHandler handler = Handlers[index];

        handler.Receive(reader);

        if (Main.netMode == NetmodeID.Server)
            Send(mod, index, ignoreClient: whoAmI);
    }

    #endregion

    #region Private Methods

    private static void Send(Mod mod, int index, int toClient = -1, int ignoreClient = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer ||
            !mod.IsNetSynced)
            return;

        ModPacket packet = mod.GetPacket();

        packet.Write(index);

        Handlers[index].Write(packet);

        packet.Send(toClient, ignoreClient);
    }

    #endregion
}
