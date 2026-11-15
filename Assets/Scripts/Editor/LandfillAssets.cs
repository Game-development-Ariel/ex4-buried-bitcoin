using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The data assets of the landfill game: events, the day's settings, the kinds of items and the machines.
/// Each one is created the first time it is asked for. Values are written only when the asset is new,
/// so tuning an asset in the Inspector is not overwritten by building the scene again.
/// </summary>
public static class LandfillAssets
{
    private const string Folder = "Assets/Data/";

    public static GameEvent TableFull => SetupTools.Asset<GameEvent>(Folder + "Events/TableFull.asset");
    public static GameEvent TableHasRoom => SetupTools.Asset<GameEvent>(Folder + "Events/TableHasRoom.asset");
    public static GameEvent TargetFound => SetupTools.Asset<GameEvent>(Folder + "Events/TargetFound.asset");
    public static GameEvent TargetLost => SetupTools.Asset<GameEvent>(Folder + "Events/TargetLost.asset");
    public static GameEvent WrongPick => SetupTools.Asset<GameEvent>(Folder + "Events/WrongPick.asset");
    public static GameEvent DayWon => SetupTools.Asset<GameEvent>(Folder + "Events/DayWon.asset");
    public static DayLostEvent DayLost => SetupTools.Asset<DayLostEvent>(Folder + "Events/DayLost.asset");

    public static TrashItemData Item(string name, Color color, float size, float mass, bool isSmall, int sellPrice, string description = "")
    {
        string path = Folder + "Items/" + name.Replace(" ", "") + ".asset";
        bool isNew = !File.Exists(path);
        TrashItemData item = SetupTools.Asset<TrashItemData>(path);
        if (isNew)
        {
            item.displayName = name;
            item.color = color;
            item.size = size;
            item.mass = mass;
            item.isSmall = isSmall;
            item.sellPrice = sellPrice;
            item.description = description;
            EditorUtility.SetDirty(item);
        }
        return item;
    }

    public static MachineData Machine(string name, int price, string description, GameObject prefab)
    {
        string path = Folder + "Machines/" + name + ".asset";
        bool isNew = !File.Exists(path);
        MachineData machine = SetupTools.Asset<MachineData>(path);
        if (isNew)
        {
            machine.displayName = name;
            machine.price = price;
            machine.description = description;
        }
        machine.prefab = prefab;
        EditorUtility.SetDirty(machine);
        return machine;
    }

    /// <summary>
    /// Every MachineData asset in the project, cheapest first. The shop shows all of them,
    /// so a new machine is only a new asset and its prefab.
    /// </summary>
    public static MachineData[] AllMachines()
    {
        return AssetDatabase.FindAssets("t:MachineData")
            .Select(guid => AssetDatabase.LoadAssetAtPath<MachineData>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(machine => machine.price)
            .ThenBy(machine => machine.displayName)
            .ToArray();
    }

    /// <summary>Day 1 from the level design, made shorter: two trucks, and the red toolbox is in the second.</summary>
    public static LandfillDaySettings DaySettings()
    {
        string path = Folder + "Day1Settings.asset";
        bool isNew = !File.Exists(path);
        LandfillDaySettings day = SetupTools.Asset<LandfillDaySettings>(path);
        if (!isNew)
        {
            return day;
        }

        TrashItemData dirt = Item("Dirt clod", new Color(0.42f, 0.3f, 0.2f), 0.35f, 1f, true, 0);
        TrashItemData bag = Item("Plastic bag", new Color(0.88f, 0.9f, 0.92f), 0.6f, 0.2f, false, 0);
        TrashItemData bottle = Item("Glass bottle", new Color(0.3f, 0.6f, 0.4f), 0.5f, 1f, false, 0);
        TrashItemData can = Item("Tin can", new Color(0.72f, 0.74f, 0.78f), 0.45f, 1f, false, 10);
        TrashItemData scrap = Item("Copper scrap", new Color(0.82f, 0.48f, 0.25f), 0.6f, 2f, false, 30);
        TrashItemData rustyBox = Item("Rusty box", new Color(0.55f, 0.27f, 0.16f), 0.85f, 2f, false, 5);
        TrashItemData toolbox = Item("Red toolbox", new Color(0.85f, 0.12f, 0.1f), 0.95f, 3f, false, 0, "A red toolbox. Big, heavy, metal.");

        day.trucks = new[]
        {
            Load((dirt, 9), (bag, 3), (bottle, 1), (can, 2), (scrap, 1)),
            Load((dirt, 13), (bag, 4), (bottle, 2), (can, 2), (scrap, 1), (rustyBox, 1), (toolbox, 1)),
        };
        day.target = toolbox;
        EditorUtility.SetDirty(day);
        return day;
    }

    private static TruckLoad Load(params (TrashItemData item, int count)[] entries)
    {
        var load = new TruckLoad { entries = new TruckLoad.Entry[entries.Length] };
        for (int i = 0; i < entries.Length; i++)
        {
            load.entries[i] = new TruckLoad.Entry { item = entries[i].item, count = entries[i].count };
        }
        return load;
    }
}
