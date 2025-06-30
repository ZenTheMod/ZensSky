using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;

namespace ZensSky.Core.DataStructures;

public sealed class OBJModel : IDisposable
{
    #region Private Fields

    private VertexPositionColorTexture[]? Verticies;

    #endregion

    #region Public Fields

    public VertexBuffer? Buffer;

    #endregion

    #region Private Methods

    private void UpdateBuffer()
    {
        if (Verticies is null)
            return;

        if (Buffer is null || Buffer.IsDisposed || Buffer.graphicsDevice.IsDisposed)
        {
            Buffer?.Dispose();

            Buffer = new(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), Verticies.Length, BufferUsage.None);

            Buffer?.SetData(Verticies);
        }
    }

    #endregion

    #region Public Methods

    public void Dispose() =>
        Buffer?.Dispose();

    public static OBJModel Create(Stream stream)
    {
        OBJModel model = new();

        List<VertexPositionColorTexture> verts = [];

        using StreamReader reader = new(stream);

        string? text;
        while ((text = reader.ReadLine()) is not null)
        {
            string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (array.Length == 0)
                continue;

            switch (array[0])
            {
                
            }
        }

        model.Verticies = [.. verts];

        return model;
    }

    public void Draw()
    {
        UpdateBuffer();


    }

    #endregion
}
