using System.Collections.Generic;

namespace ZensSky.Common.DataStructures;

/// <summary>
/// A tree data structure for fast spatial operations.<br/>
/// Loosely inspired by <see href="https://github.com/BadEcho/game/blob/master/src/Game/Quadtree.cs"/>.
/// </summary>
public sealed class QuadTree<T> where T : ISpatial
{
    #region Private Fields

    private readonly List<T> Objects = [];

    private readonly int ObjectsPerNode;
    private readonly int Depth;

    private readonly QuadTree<T>?[] Neighbors = new QuadTree<T>[4];

    #endregion

    #region Public Properties

    public Rectangle Bounds { get; init; }

    public int Level { get; init; }

    #endregion

    #region Public Constructors

    public QuadTree(Rectangle bounds, int level, int maxObjectsPerNode = 5, int maxDepth = 32)
    {
        Bounds = bounds;
        Level = level;

        ObjectsPerNode = maxObjectsPerNode;
        Depth = maxDepth;
    }

    #endregion

    public void Insert(T obj)
    {
        if (Neighbors[0] != null)
        {
            QuadTree<T>? area = GetContainer(obj.Bounds);
            if (area is not null)
            {
                area.Insert(obj);
                return;
            }
        }

        Objects.Add(obj);

            // Don't split if either the max depth or min objects has been reached.
        if (Objects.Count <= ObjectsPerNode || 
            Level >= Depth)
            return;

        if (Neighbors[0] is null)
            Split();

        int i = 0;

        while (i < Objects.Count)
        {
            QuadTree<T>? area = GetContainer(obj.Bounds);
            if (area is not null)
            {
                area.Insert(obj);
                Objects.RemoveAt(i);
            }
            else
                i++;
        }
    }

    public List<T> Query(Rectangle query)
    {
        List<T> result = [];
        QuadTree<T>? area = GetContainer(query);

        if (area is not null)
            result.AddRange(area.Query(query));
        else
            result.AddRange(Objects);

        return result;
    }

    #region Private Methods

    private QuadTree<T>? GetContainer(Rectangle area)
    {
        if (Neighbors[0] is null)
            return null;

        if (Neighbors[0]?.Bounds.Contains(area) ?? false)
            return Neighbors[0];

        if (Neighbors[1]?.Bounds.Contains(area) ?? false)
            return Neighbors[1];

        if (Neighbors[2]?.Bounds.Contains(area) ?? false)
            return Neighbors[2];

        return Neighbors[3];
    }

    private void Split()
    {
        int x = Bounds.X;
        int y = Bounds.Y;

        int subWidth = Bounds.Width / 2;
        int subHeight = Bounds.Height / 2;

        Neighbors[0] = new(new(x + subWidth, y, subWidth, subHeight), Level + 1, ObjectsPerNode, Depth);
        Neighbors[1] = new(new(x, y, subWidth, subHeight), Level + 1, ObjectsPerNode, Depth);
        Neighbors[2] = new(new(x, y + subHeight, subWidth, subHeight), Level + 1, ObjectsPerNode, Depth);
        Neighbors[3] = new(new(x + subWidth, y + subHeight, subWidth, subHeight), Level + 1, ObjectsPerNode, Depth);
    }

    #endregion
}
