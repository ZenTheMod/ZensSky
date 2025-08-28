using System.IO;
using Terraria.ModLoader;

namespace ZensSky.Core.Systems.Net;

/// <summary>
/// Allows for the quick implementation of <see cref="ModPacket"/>s.
/// </summary>
public interface IPacketHandler
{
    public void Write(BinaryWriter writer);

    public void Receive(BinaryReader reader);

    public void Send(Mod mod, int toClient = -1, int ignoreClient = -1) =>
        PacketSystem.Send(mod, this, toClient, ignoreClient);
}
