using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGeneration.DataStructures;

public class AliasedList<TKey, TElement>
    : List<(HashSet<TKey> Keys, List<TElement> Items)>
{
    public List<TElement> this[TKey name]
    {
        get
        {
            if (!TryFind(name, out List<TElement> elements))
                throw new KeyNotFoundException();

            return elements;
        }
    }

    public AliasedList()
        : base() { }

    public AliasedList(IEnumerable<TElement> items, Func<TElement, TKey> nameFunc)
        : base()
    {
        foreach (TElement item in items)
            Add(nameFunc(item), item);
    }

    public AliasedList(IEnumerable<TElement> items, Func<TElement, HashSet<TKey>> nameFunc)
        : base()
    {
        foreach (TElement item in items)
            Add(nameFunc(item), [item]);
    }

    /// <summary>
    /// Adds the item(s) at the key(s) given.
    /// </summary>
    /// <returns><see cref="true"/> if the list does not already contain the key(s).</returns>
    public bool Add(TKey key, TElement item)
    {
        int inList = FindIndex(e => e.Keys.Contains(key));

        if (inList != -1)
            this[inList].Items.Add(item);
        else
            Add(([key], [item]));

        return inList == -1;
    }

    /// <inheritdoc cref="Add(TKey, TElement)"/>
    public bool Add(TKey key, List<TElement> items) =>
       Add([key], items);

    /// <inheritdoc cref="Add(TKey, TElement)"/>
    public bool Add(IEnumerable<TKey> key, List<TElement> items) =>
        Add([.. key], items);

    /// <inheritdoc cref="Add(TKey, TElement)"/>
    public bool Add(HashSet<TKey> keys, TElement item) =>
        Add(keys, [item]);

    /// <inheritdoc cref="Add(TKey, TElement)"/>
    public bool Add(HashSet<TKey> keys, List<TElement> items)
    {
        int inList = FindIndex(e => keys.All(k => e.Keys.Contains(k)));

        if (inList != -1)
            this[inList].Items.AddRange(items);
        else
            Add((keys, items));

        return inList == -1;
    }

    public bool TryFind(TKey key, out List<TElement> items)
    {
        foreach ((HashSet<TKey> keys, List<TElement> elements) in this)
        {
            if (keys.Contains(key))
            {
                items = elements;

                return true;
            }
        }

        items = [];

        return false;
    }
}
