using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ZensSky.Core.DataStructures;

/// <summary>
/// A tree data structure for fast spatial queries.<br/>
/// Loosely inspired by <see href="https://github.com/BadEcho/game/blob/master/src/Game/Quadtree.cs"/> and <see href="https://github.com/Auios/Auios.QuadTree/blob/main/Auios.QuadTree/QuadTree.cs"/>. 
/// </summary>
public sealed class QuadTree<T> where T : ISpatial
{
    #region Private Fields

    private readonly HashSet<T> Objects = [];

    private readonly int MaxObjects;
    private readonly int MaxLevel;

    private QuadTree<T>?
        TopLeft,
        TopRight,
        BotLeft,
        BotRight;

        // No branches should ever be null when this element has children.
    [MemberNotNullWhen(true, nameof(TopLeft), nameof(TopRight), nameof(BotLeft), nameof(BotRight))]
    private bool HasChildren { get; set; } = false;

    #endregion

    #region Public Properties

    public Rectangle Bounds { get; init; }

    public int Level { get; init; }

    #endregion

    #region Public Constructors

    /// <param name="maxObjects">Max number of items intended per branch.</param>
    public QuadTree(Rectangle bounds, int level, int maxObjects = 5, int maxDepth = 10)
    {
        Bounds = bounds;
        Level = level;

        MaxObjects = maxObjects;
        MaxLevel = maxDepth;
    }

    #endregion

    #region Public Methods

    public void Insert(IEnumerable<T> objects)
    {
        foreach (T obj in objects)
            Insert(obj);
    }

    public bool Insert(T obj)
    {
        if (!IsObjectInside(obj))
            return false;

        if (HasChildren)
        {
            if (TopLeft.Insert(obj))
                return true;
            if (TopRight.Insert(obj))
                return true;
            if (BotLeft.Insert(obj))
                return true;
            if (BotRight.Insert(obj))
                return true;
        }
        else
        {
            Objects.Add(obj);

            if (Objects.Count > MaxObjects)
                Split();
        }

        return true;
    }

    /// <summary>
    /// Grabs all objects within <paramref name="queryArea"/>.
    /// </summary>
    /// <param name="queryArea">The area to search.</param>
    public T[] Query(Rectangle queryArea)
    {
        List<T> foundObjects = [];
        if (HasChildren)
        {
            foundObjects.AddRange(TopLeft.Query(queryArea));
            foundObjects.AddRange(TopRight.Query(queryArea));
            foundObjects.AddRange(BotLeft.Query(queryArea));
            foundObjects.AddRange(BotRight.Query(queryArea));
        }
        else if (Bounds.Intersects(queryArea))
            foundObjects.AddRange(Objects);

        HashSet<T> result = [];
        result.UnionWith(foundObjects);

        return [.. result];
    }

    #endregion

    #region Private Methods

    private bool IsObjectInside(T obj) =>
        Bounds.Contains(obj.Bounds);

    private void Split()
    {
        if (Level >= MaxLevel) 
            return;

        int x = Bounds.X;
        int y = Bounds.Y;

        int subWidth = Bounds.Width / 2;
        int subHeight = Bounds.Height / 2;

        int nextLevel = Level + 1;

        HasChildren = true;

        TopLeft = new(new(x, y, subWidth, subHeight), nextLevel, MaxObjects, MaxLevel);
        TopRight = new(new(x + subWidth, y, subWidth, subHeight), nextLevel, MaxObjects, MaxLevel);
        BotLeft = new(new(x, y + subHeight, subWidth, subHeight), nextLevel, MaxObjects, MaxLevel);
        BotRight = new(new(x + subWidth, y + subHeight, subWidth, subHeight), nextLevel, MaxObjects, MaxLevel);

        foreach (T obj in Objects)
            Insert(obj);

        Objects.Clear();
    }

    #endregion
}
