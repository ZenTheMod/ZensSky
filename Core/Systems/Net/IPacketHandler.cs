using System.IO;
using Terraria.ModLoader;

namespace ZensSky.Core.Systems.Net;

/// <summary>
/// Allows for the quick implementation of <see cref="ModPacket"/>s.<br/>
/// Use <see cref="PacketSystem.Send{T}(int, int)"/> — where T is the type of the class with IPacketHandler — to send packets.
/// </summary>
public interface IPacketHandler : ILoadable
{
    public void Write(BinaryWriter writer);

    public void Receive(BinaryReader reader);

    void ILoadable.Load(Mod mod) =>
        PacketSystem.Handlers.Add(this);
}
