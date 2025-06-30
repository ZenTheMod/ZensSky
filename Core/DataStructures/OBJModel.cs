using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;

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

        // TODO: Implement a reader/writer for a binary filetype, (Similar to Celeste's?) e.g. .obj.export.
    public static OBJModel Create(Stream stream)
    {
        OBJModel model = new();

        List<VertexPositionColorTexture> verticies = [];

        List<Vector3> positions = [];
        List<Vector2> textureCoordinates = [];
        List<Vector3> vertexNormals = [];

        bool containsNonTriangularFaces = false;

        using StreamReader reader = new(stream);

        string? text;
        while ((text = reader.ReadLine()) is not null)
        {
            string[] segments = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                continue;

            switch (segments[0])
            {
                case "v":
                    if (segments.Length < 4)
                        break;

                    positions.Add(new(
                        float.Parse(segments[1]), 
                        float.Parse(segments[2]), 
                        float.Parse(segments[3])));
                    break;

                case "vt":
                    if (segments.Length < 3)
                        break;

                    textureCoordinates.Add(new(
                        float.Parse(segments[1]),
                        float.Parse(segments[2])));
                    break;

                case "vn":
                    if (segments.Length < 4)
                        break;

                    vertexNormals.Add(new(
                        float.Parse(segments[1]),
                        float.Parse(segments[2]),
                        float.Parse(segments[3])));
                    break;

                case "f":
                    if (segments.Length != 4)
                    {
                        containsNonTriangularFaces = true;
                        break;
                    }

                    for (int i = 1; i < segments.Length; i++) 
                    {
                        VertexPositionColorTexture vertex = new();

                        string[] components = segments[i].Split('/', StringSplitOptions.RemoveEmptyEntries);

                        if (components.Length != 3)
                            continue;

                        vertex.Position = positions[int.Parse(components[0]) - 1];

                        vertex.TextureCoordinate = textureCoordinates[int.Parse(components[1]) - 1];

                            // Pack normal data as a color input.
                        Vector3 normal = (Vector3.Normalize(vertexNormals[int.Parse(components[2]) - 1]) * .5f) + new Vector3(.5f);
                        vertex.Color = new(normal);

                        verticies.Add(vertex);
                    }
                    break;
            }
        }

        model.Verticies = [.. verticies];

        if (containsNonTriangularFaces)
            ModContent.GetInstance<ZensSky>().Logger.Warn($"{nameof(OBJModel)}: Model contained non triangular faces! These will not be drawn.");

        if (model.Verticies.Length < 3)
            throw new InvalidDataException($"{nameof(OBJModel)}: Not enough verticies to create vertex buffer.");

        model.UpdateBuffer();

        return model;
    }

    public void Draw(GraphicsDevice device)
    {
        UpdateBuffer();

        if (Buffer is null)
            return;

        device.SetVertexBuffer(Buffer);

        device.DrawPrimitives(PrimitiveType.TriangleList, 0, Buffer.VertexCount / 3);
    }

    #endregion
}
