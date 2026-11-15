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

    // The items of day 1. Each has a color, a size (width and height in world units), a mass (Rigidbody2D mass)
    // and a sell price (dollars, 0 = it can't be sold). The blower lifts the light ones.
    private const float LightMass = 0.2f;
    private const float NormalMass = 1f;
    private const float HeavyMass = 2f;
    private const float VeryHeavyMass = 3f;
    private const int NoSellPrice = 0;

    private static readonly Color DirtColor = new Color(0.42f, 0.3f, 0.2f);
    private const float DirtSize = 0.35f;
    private static readonly Color BagColor = new Color(0.88f, 0.9f, 0.92f);
    private const float BagSize = 0.6f;
    private static readonly Color BottleColor = new Color(0.3f, 0.6f, 0.4f);
    private const float BottleSize = 0.5f;
    private static readonly Color CanColor = new Color(0.72f, 0.74f, 0.78f);
    private const float CanSize = 0.45f;
    private const int CanPrice = 10;
    private static readonly Color ScrapColor = new Color(0.82f, 0.48f, 0.25f);
    private const float ScrapSize = 0.6f;
    private const int ScrapPrice = 30;
    private static readonly Color RustyBoxColor = new Color(0.55f, 0.27f, 0.16f);
    private const float RustyBoxSize = 0.85f;
    private const int RustyBoxPrice = 5;
    private static readonly Color ToolboxColor = new Color(0.85f, 0.12f, 0.1f);
    private const float ToolboxSize = 0.95f;

    // How many of each item the two trucks bring (when it is more than one).
    private const int FirstTruckDirt = 9;
    private const int FirstTruckBags = 3;
    private const int FirstTruckCans = 2;
    private const int SecondTruckDirt = 13;
    private const int SecondTruckBags = 4;
    private const int SecondTruckBottles = 2;
    private const int SecondTruckCans = 2;

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
    /// Every MachineData asset in the project, most expensive first. The shop shows all of them,
    /// so a new machine is only a new asset and its prefab.
    /// </summary>
    public static MachineData[] AllMachines()
    {
        return AssetDatabase.FindAssets("t:MachineData")
            .Select(guid => AssetDatabase.LoadAssetAtPath<MachineData>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderByDescending(machine => machine.price) // The sieve first, like in the level design.
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
            ReportMissingItems(day, path);
            return day;
        }

        TrashItemData dirt = Item("Dirt clod", DirtColor, DirtSize, NormalMass, true, NoSellPrice);
        TrashItemData bag = Item("Plastic bag", BagColor, BagSize, LightMass, false, NoSellPrice);
        TrashItemData bottle = Item("Glass bottle", BottleColor, BottleSize, NormalMass, false, NoSellPrice);
        TrashItemData can = Item("Tin can", CanColor, CanSize, NormalMass, false, CanPrice);
        TrashItemData scrap = Item("Copper scrap", ScrapColor, ScrapSize, HeavyMass, false, ScrapPrice);
        TrashItemData rustyBox = Item("Rusty box", RustyBoxColor, RustyBoxSize, HeavyMass, false, RustyBoxPrice);
        TrashItemData toolbox = Item("Red toolbox", ToolboxColor, ToolboxSize, VeryHeavyMass, false, NoSellPrice, "A red toolbox. Big, heavy, metal.");

        day.trucks = new[]
        {
            Load((dirt, FirstTruckDirt), (bag, FirstTruckBags), (bottle, 1), (can, FirstTruckCans), (scrap, 1)),
            Load((dirt, SecondTruckDirt), (bag, SecondTruckBags), (bottle, SecondTruckBottles), (can, SecondTruckCans), (scrap, 1), (rustyBox, 1), (toolbox, 1)),
        };
        day.target = toolbox;
        EditorUtility.SetDirty(day);
        return day;
    }

    /// <summary>
    /// The settings already exist, so they are not written again. If an item asset they point to was deleted,
    /// the reference is empty and the game would break while playing, so say it now.
    /// </summary>
    private static void ReportMissingItems(LandfillDaySettings day, string path)
    {
        string fix = $"Set it in the Inspector, or delete {path} to create the default day again.";
        if (day.target == null)
        {
            Debug.LogError($"{day.name} has no target item. {fix}", day);
        }
        for (int truck = 0; truck < day.trucks.Length; truck++)
        {
            foreach (TruckLoad.Entry entry in day.trucks[truck].entries)
            {
                if (entry.item == null)
                {
                    Debug.LogError($"Truck {truck + 1} in {day.name} has an entry with no item. {fix}", day);
                }
            }
        }
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
