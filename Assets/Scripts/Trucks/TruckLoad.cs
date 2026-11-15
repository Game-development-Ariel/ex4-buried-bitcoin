using System;
using System.Collections.Generic;

/// <summary>
/// What one truck carries: how many of each kind of item.
/// </summary>
[Serializable]
public class TruckLoad
{
    [Serializable]
    public class Entry
    {
        public TrashItemData item;
        public int count = 1;
    }

    public Entry[] entries;

    /// <summary>All the items of the truck in a random order, so the target is never in the same place.</summary>
    public List<TrashItemData> ShuffledItems()
    {
        var items = new List<TrashItemData>();
        foreach (Entry entry in entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                items.Add(entry.item);
            }
        }

        for (int i = items.Count - 1; i > 0; i--)
        {
            int other = UnityEngine.Random.Range(0, i + 1);
            (items[i], items[other]) = (items[other], items[i]);
        }
        return items;
    }
}
