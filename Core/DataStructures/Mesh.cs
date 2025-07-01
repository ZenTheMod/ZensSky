using Microsoft.Xna.Framework.Graphics;
using System;

namespace ZensSky.Core.DataStructures;

public record struct Mesh : IDisposable
{
    #region Public Properties

    public string Name { get; init; }

    public int StartIndex { get; init; }

    public int EndIndex { get; init; }

    public VertexBuffer? Buffer {  get; set; }

    #endregion

    #region Public Constructors

    public Mesh(string name, int startIndex, int endIndex)
    {
        Name = name;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Resets <see cref="Buffer"/> if necessary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="device"></param>
    /// <param name="verticies"></param>
    /// <returns><see cref="Buffer"/></returns>
    public VertexBuffer? ResetBuffer<T>(GraphicsDevice device, T[] verticies) where T : struct, IVertexType
    {
        if (Buffer is not null && !Buffer.IsDisposed)
            return Buffer;

        if (verticies.Length < 3)
            throw new InvalidOperationException($"{nameof(Mesh)}: Not enough verticies to generate {nameof(VertexBuffer)}!");

        Buffer = new(device, typeof(T), EndIndex - StartIndex, BufferUsage.None);
        Buffer.SetData(verticies, StartIndex, EndIndex - StartIndex);

        return Buffer;
    }

    public readonly void Dispose() => 
        Buffer?.Dispose();

    #endregion
}
