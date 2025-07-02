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

    private VertexPositionNormalTexture[]? Vertices;
    private Mesh[]? Meshes;

    #endregion

    #region Private Methods

    private VertexBuffer? ResetBuffer(GraphicsDevice device, int i)
    {
        if (Vertices is null || Meshes is null || !Meshes.IndexInRange(i))
            return null;

        return Meshes[i].ResetBuffer(device, Vertices);
    }

    private void ResetBuffers(GraphicsDevice device)
    {
        if (Vertices is null || Meshes is null)
            return;

        Array.ForEach(Meshes, m => m.ResetBuffer(device, Vertices));
    }

    #endregion

    #region Public Methods

    public void Dispose()
    {
        if (Meshes is not null)
            Array.ForEach(Meshes, m => m.Dispose());
    }

    #region Reading

        // TODO: Implement a reader/writer for a binary filetype, (Similar to Celeste's?) e.g. .obj.export.
    public static OBJModel Create(Stream stream)
    {
        OBJModel model = new();

        List<VertexPositionNormalTexture> vertices = [];

        List<Mesh> meshes = [];

        List<Vector3> positions = [];
        List<Vector2> textureCoordinates = [];
        List<Vector3> vertexNormals = [];

        string meshName = string.Empty;
        int startIndex = 0;

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
                case "o":
                    if (segments.Length < 2)
                        break;

                    if (vertices.Count > 3 && meshName != string.Empty)
                        meshes.Add(new Mesh(meshName, startIndex, vertices.Count));

                    meshName = segments[1];
                    startIndex = vertices.Count;
                    break;

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
                        VertexPositionNormalTexture vertex = new();

                        string[] components = segments[i].Split('/', StringSplitOptions.RemoveEmptyEntries);

                        if (components.Length != 3)
                            continue;

                        vertex.Position = positions[int.Parse(components[0]) - 1];

                            // Account for the inversed Y coordinate.
                        Vector2 coord = textureCoordinates[int.Parse(components[1]) - 1];
                        coord.Y = 1 - coord.Y;

                        vertex.TextureCoordinate = coord;

                        Vector3 normal = vertexNormals[int.Parse(components[2]) - 1];
                        vertex.Normal = normal;

                        vertices.Add(vertex);
                    }
                    break;
            }
        }

        if (vertices.Count > 3 && meshName != string.Empty)
            meshes.Add(new Mesh(meshName, startIndex, vertices.Count));

        if (meshes.Count > 0) 
            model.Meshes = [.. meshes];
        else
            throw new InvalidDataException($"{nameof(OBJModel)}: Model did not contain at least one object!");

        model.Vertices = [.. vertices];

        if (containsNonTriangularFaces)
            ModContent.GetInstance<ZensSky>().Logger.Warn($"{nameof(OBJModel)}: Model contained non triangular faces! These will not be drawn.");

        if (model.Vertices.Length < 3)
            throw new InvalidDataException($"{nameof(OBJModel)}: Not enough vertices to create vertex buffer!");

        model.ResetBuffers(Main.instance.GraphicsDevice);

        return model;
    }

    #endregion

    #region Drawing

    /// <summary>
    /// Draws the first <see cref="Mesh"/> where <see cref="Mesh.Name"/> is equal to <paramref name="name"/>.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="name"></param>
    public void Draw(GraphicsDevice device, string name)
    {
        if (Meshes is null)
            return;

        int i = Array.FindIndex(Meshes, m => m.Name == name);

        if (i != -1)
            Draw(device, i);
    }

    /// <summary>
    /// Draws the <see cref="Mesh"/> at index <paramref name="i"/> if within range.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="i"></param>
    public void Draw(GraphicsDevice device, int i = 0)
    {
        VertexBuffer? buffer = ResetBuffer(device, i);

        if (buffer is null)
            return;

        device.SetVertexBuffer(buffer);

        device.DrawPrimitives(PrimitiveType.TriangleList, 0, buffer.VertexCount / 3);
    }

    #endregion

    #endregion
}
