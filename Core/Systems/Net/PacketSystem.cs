using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZensSky.Core.Systems.Net;

public sealed class PacketSystem : ModSystem
{
    #region Private Fields

    private static IPacketHandler?[] Handlers = [];

    #endregion

    #region Loading

    public override void Load()
    {
        Assembly assembly = Mod.Code;

        Handlers = [.. assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IPacketHandler)))
            .Select(t => t as IPacketHandler)];
    }

    #endregion

    #region Public Methods

    public static void Send(Mod mod, IPacketHandler handler, int toClient = -1, int ignoreClient = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer ||
            !mod.IsNetSynced)
            return;

        ModPacket packet = mod.GetPacket();

        int id = Array.IndexOf(Handlers, handler);

        if (id == -1)
            throw new KeyNotFoundException($"Could not find {nameof(handler)}, in {nameof(Handlers)}!");

        packet.Write(id);

        handler.Write(packet);

        packet.Send(toClient, ignoreClient);
    }

    public static void Handle(Mod mod, BinaryReader reader, int whoAmI)
    {
        if (Main.netMode == NetmodeID.SinglePlayer ||
            !mod.IsNetSynced)
            return;

        int id = reader.ReadInt32();

        Handlers[id]?.Receive(reader);

        if (Main.netMode == NetmodeID.Server)
            Handlers[id]?.Send(mod, ignoreClient: whoAmI);
    }

    #endregion
}
