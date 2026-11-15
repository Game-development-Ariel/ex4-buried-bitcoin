using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the whole sorting plant scene from code, with its prefabs and data. Run it with Tools > Rebuild Scene.
/// The view is from the side, so gravity does part of the sorting. Positions are in world units.
/// Running it again replaces the scene and the prefabs. The data assets keep their values (see LandfillAssets).
/// </summary>
public static class LandfillSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Landfill.unity";
    private const string PrefabFolder = "Assets/Prefabs/";

    // The top of the belt. Machines and slots are placed on this line.
    private const float BeltTop = 0.95f;
    private const float BeltThickness = 0.3f;
    private const float BeltLeft = -8.4f;
    private const float BeltRight = 3.5f;

    private static readonly float[] SlotPositions = { -4.5f, -2f, 0.5f };

    private static readonly Color Sky = new Color(0.16f, 0.18f, 0.22f);
    private static readonly Color Metal = new Color(0.3f, 0.32f, 0.36f);
    private static readonly Color BeltColor = new Color(0.44f, 0.46f, 0.5f);
    private static readonly Color Dirt = new Color(0.23f, 0.17f, 0.12f);
    private static readonly Color SignColor = new Color(1f, 0.85f, 0.4f, 0.55f);
    private static readonly Color LabelColor = new Color(0.8f, 0.78f, 0.72f);

    public static void Build()
    {
        Directory.CreateDirectory(PrefabFolder);
        Scene scene = SceneParts.NewScene();
        Camera camera = SceneParts.CreateCamera(Sky, 5.4f);
        camera.transform.position = new Vector3(0f, 0.5f, -10f); // A bit up, so the plant sits under the top bar of the UI.
        SceneParts.CreateGlobalLight();
        LandfillDaySettings day = LandfillAssets.DaySettings();

        ConveyorBelt belt = CreateBelt(day);
        CreateWasteAndWalls(day);
        MachineSlot[] slots = CreateSlots(belt.GetComponent<Collider2D>());
        TruckDumper dumper = CreateDumper(day);

        // The game logic sits on one object, each part in its own small component.
        var game = new GameObject("Game");
        Wallet wallet = game.AddComponent<Wallet>();
        SetupTools.SetField(wallet, "settings", day);
        DayTimer timer = game.AddComponent<DayTimer>();
        SetupTools.SetField(timer, "settings", day);
        MachineShop shop = game.AddComponent<MachineShop>();
        SetupTools.SetField(shop, "wallet", wallet);
        SetupTools.SetArray(shop, "slots", slots);

        GameObject table = CreateTable(day, wallet);
        WorldClicker clicker = camera.gameObject.AddComponent<WorldClicker>();
        SetupTools.SetField(clicker, "worldCamera", camera);

        LandfillRound round = game.AddComponent<LandfillRound>();
        SetupTools.SetField(round, "settings", day);
        SetupTools.SetField(round, "timer", timer);
        SetupTools.SetArray(round, "stopWhenDayEnds", new Object[] { belt, dumper, timer, clicker });
        SetupTools.SetField(round, "targetFound", LandfillAssets.TargetFound);
        SetupTools.SetField(round, "targetLost", LandfillAssets.TargetLost);
        SetupTools.SetField(round, "wrongPick", LandfillAssets.WrongPick);
        SetupTools.SetField(round, "dayWon", LandfillAssets.DayWon);
        SetupTools.SetField(round, "dayLost", LandfillAssets.DayLost);

        CreateUi(day, wallet, timer, dumper, shop, table);
        SceneParts.Save(scene, ScenePath);
    }

    // ---------- The line ----------

    private static ConveyorBelt CreateBelt(LandfillDaySettings day)
    {
        float width = BeltRight - BeltLeft;
        Vector2 center = new Vector2((BeltLeft + BeltRight) / 2f, BeltTop - BeltThickness / 2f);
        GameObject belt = SceneParts.CreateBox("Belt", null, center, new Vector2(width, BeltThickness), BeltColor, 2);

        BoxCollider2D box = belt.AddComponent<BoxCollider2D>();
        box.usedByEffector = true;
        belt.AddComponent<SurfaceEffector2D>();
        ConveyorBelt conveyor = belt.AddComponent<ConveyorBelt>();
        SetupTools.SetField(conveyor, "settings", day);
        SetupTools.SetField(conveyor, "tableFull", LandfillAssets.TableFull);
        SetupTools.SetField(conveyor, "tableHasRoom", LandfillAssets.TableHasRoom);
        return conveyor;
    }

    /// <summary>
    /// The waste pit under the belt, the hood above it (where the blower throws light items),
    /// and the walls that keep the items inside the plant.
    /// </summary>
    private static void CreateWasteAndWalls(LandfillDaySettings day)
    {
        var parent = new GameObject("Plant").transform;

        SceneParts.CreateBox("Pit Background", parent, new Vector2(-2.4f, -1f), new Vector2(12.2f, 2.9f), Dirt, 0);
        AddWastePile(day, SceneParts.CreateBox("Waste Pit", parent, new Vector2(-2.4f, -2.45f), new Vector2(12.2f, 0.3f), Dirt * 0.7f, 1));
        AddWastePile(day, SceneParts.CreateBox("Waste Hood", parent, new Vector2(-2.45f, 4.25f), new Vector2(11.9f, 0.25f), Metal, 1));
        SceneParts.CreateLabel("WASTE", parent, new Vector2(-2.4f, -1.5f), 0.5f, LabelColor * 0.7f);

        AddWall(parent, "Left Wall", new Vector2(-8.5f, 1f), new Vector2(0.2f, 7f));
        AddWall(parent, "Pit Wall", new Vector2(3.65f, -0.95f), new Vector2(0.3f, 3f));
        AddWall(parent, "Table Wall", new Vector2(8.4f, 0.4f), new Vector2(0.2f, 4.3f));
    }

    private static void AddWastePile(LandfillDaySettings day, GameObject box)
    {
        box.AddComponent<BoxCollider2D>();
        WastePile pile = box.AddComponent<WastePile>();
        SetupTools.SetField(pile, "settings", day);
        SetupTools.SetField(pile, "targetLost", LandfillAssets.TargetLost);
    }

    private static void AddWall(Transform parent, string name, Vector2 center, Vector2 size)
    {
        SceneParts.CreateBox(name, parent, center, size, Metal, 1).AddComponent<BoxCollider2D>();
    }

    private static MachineSlot[] CreateSlots(Collider2D belt)
    {
        var parent = new GameObject("Machine Slots").transform;
        var slots = new MachineSlot[SlotPositions.Length];
        for (int i = 0; i < SlotPositions.Length; i++)
        {
            var slotObject = new GameObject("Slot " + (i + 1));
            slotObject.transform.SetParent(parent, false);
            slotObject.transform.position = new Vector3(SlotPositions[i], BeltTop, 0f);

            // The sign hangs above the belt. It is what the player clicks.
            Vector2 signCenter = new Vector2(0f, 2.3f);
            BoxCollider2D clickArea = slotObject.AddComponent<BoxCollider2D>();
            clickArea.isTrigger = true;
            clickArea.offset = signCenter;
            clickArea.size = new Vector2(1.5f, 1.1f);
            SceneParts.CreateBox("Frame", slotObject.transform, signCenter, new Vector2(1.5f, 1.1f), new Color(1f, 1f, 1f, 0.08f), 1);

            var emptySign = new GameObject("Empty Sign");
            emptySign.transform.SetParent(slotObject.transform, false);
            SceneParts.CreateBox("Plus A", emptySign.transform, signCenter, new Vector2(0.6f, 0.14f), SignColor, 3);
            SceneParts.CreateBox("Plus B", emptySign.transform, signCenter, new Vector2(0.14f, 0.6f), SignColor, 3);
            SceneParts.CreateLabel("slot", emptySign.transform, signCenter + new Vector2(0f, -0.38f), 0.22f, SignColor);

            slots[i] = slotObject.AddComponent<MachineSlot>();
            SetupTools.SetField(slots[i], "belt", belt);
            SetupTools.SetField(slots[i], "emptySign", emptySign);
        }
        return slots;
    }

    private static TruckDumper CreateDumper(LandfillDaySettings day)
    {
        var parent = new GameObject("Truck").transform;
        parent.position = new Vector3(-7.3f, 2.6f, 0f);
        SceneParts.CreateBox("Truck Bed", parent, new Vector2(-0.3f, 1.1f), new Vector2(1.7f, 1f), new Color(0.85f, 0.7f, 0.2f), 3);
        SceneParts.CreateBox("Chute", parent, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.2f), Metal, 3);

        TruckDumper dumper = parent.gameObject.AddComponent<TruckDumper>();
        SetupTools.SetField(dumper, "settings", day);
        SetupTools.SetField(dumper, "itemPrefab", CreateItemPrefab());
        SetupTools.SetField(dumper, "tableFull", LandfillAssets.TableFull);
        SetupTools.SetField(dumper, "tableHasRoom", LandfillAssets.TableHasRoom);
        return dumper;
    }

    /// <summary>The table, with the two parts of its logic: what lies on it, and the player's decisions.</summary>
    private static GameObject CreateTable(LandfillDaySettings day, Wallet wallet)
    {
        var parent = new GameObject("Sorting Table").transform;
        SceneParts.CreateBox("Table Top", parent, new Vector2(6.02f, -1.6f), new Vector2(4.55f, 0.3f), new Color(0.45f, 0.36f, 0.26f), 2)
            .AddComponent<BoxCollider2D>();
        SceneParts.CreateLabel("SORTING TABLE", parent, new Vector2(6f, -2.2f), 0.4f, LabelColor);

        // The trigger covers the space above the table. Everything inside it counts as "on the table".
        BoxCollider2D area = parent.gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.offset = new Vector2(6.05f, 0.6f);
        area.size = new Vector2(4.4f, 4.1f);

        TableContents contents = parent.gameObject.AddComponent<TableContents>();
        SetupTools.SetField(contents, "settings", day);
        SetupTools.SetField(contents, "tableFull", LandfillAssets.TableFull);
        SetupTools.SetField(contents, "tableHasRoom", LandfillAssets.TableHasRoom);

        SortingDecisions decisions = parent.gameObject.AddComponent<SortingDecisions>();
        SetupTools.SetField(decisions, "settings", day);
        SetupTools.SetField(decisions, "table", contents);
        SetupTools.SetField(decisions, "wallet", wallet);
        SetupTools.SetField(decisions, "targetFound", LandfillAssets.TargetFound);
        SetupTools.SetField(decisions, "targetLost", LandfillAssets.TargetLost);
        SetupTools.SetField(decisions, "wrongPick", LandfillAssets.WrongPick);
        return parent.gameObject;
    }

    // ---------- Prefabs ----------

    private static TrashItem CreateItemPrefab()
    {
        GameObject item = SceneParts.CreateBox("TrashItem", null, Vector2.zero, Vector2.one, Color.white, 6);
        item.AddComponent<Rigidbody2D>();
        item.AddComponent<BoxCollider2D>();

        GameObject outline = SceneParts.CreateBox("Outline", item.transform, Vector2.zero, Vector2.one * 1.3f, Color.white, 5);
        outline.SetActive(false);
        SetupTools.SetField(item.AddComponent<TrashItem>(), "outline", outline);
        return SavePrefab(item, "TrashItem").GetComponent<TrashItem>();
    }

    /// <summary>The sieve: a trigger just above the belt, drawn as dark bars across it.</summary>
    private static GameObject CreateSievePrefab()
    {
        var sieve = new GameObject("Sieve");
        BoxCollider2D trigger = sieve.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = new Vector2(0f, 0.3f);
        trigger.size = new Vector2(1.6f, 0.6f);
        sieve.AddComponent<Sieve>();

        for (int i = 0; i < 6; i++)
        {
            float x = -0.7f + i * 0.28f;
            SceneParts.CreateBox("Bar", sieve.transform, new Vector2(x, -BeltThickness / 2f), new Vector2(0.1f, BeltThickness), Sky, 3);
        }
        SceneParts.CreateLabel("SIEVE", sieve.transform, new Vector2(0f, 2.3f), 0.3f, LabelColor);
        return SavePrefab(sieve, "Sieve");
    }

    /// <summary>
    /// The blower has no script at all: an AreaEffector2D pushes up everything inside its column.
    /// The force is the same for every item, so only the light ones are lifted.
    /// </summary>
    private static GameObject CreateBlowerPrefab(LandfillDaySettings day)
    {
        var blower = new GameObject("Blower");
        BoxCollider2D column = blower.AddComponent<BoxCollider2D>();
        column.isTrigger = true;
        column.usedByEffector = true;
        column.offset = new Vector2(0f, 1.5f);
        column.size = new Vector2(1.6f, 3f);

        AreaEffector2D wind = blower.AddComponent<AreaEffector2D>();
        wind.useGlobalAngle = true;
        wind.forceAngle = 90f; // Straight up.
        wind.forceMagnitude = day.blowerForce;

        SceneParts.CreateBox("Wind", blower.transform, new Vector2(0f, 1.5f), new Vector2(1.6f, 3f), new Color(0.6f, 0.8f, 1f, 0.1f), 1);
        SceneParts.CreateBox("Fan", blower.transform, new Vector2(0f, -0.65f), new Vector2(1.4f, 0.6f), new Color(0.35f, 0.5f, 0.65f), 3);
        SceneParts.CreateLabel("BLOWER", blower.transform, new Vector2(0f, 2.3f), 0.3f, LabelColor);
        return SavePrefab(blower, "Blower");
    }

    private static GameObject SavePrefab(GameObject instance, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabFolder + name + ".prefab");
        Object.DestroyImmediate(instance);
        return prefab;
    }

    // ---------- UI ----------

    private static void CreateUi(LandfillDaySettings day, Wallet wallet, DayTimer timer, TruckDumper dumper, MachineShop shop, GameObject table)
    {
        var ui = new GameObject("UI");

        HudStats stats = ui.AddComponent<HudStats>();
        SetupTools.SetField(stats, "settings", day);
        SetupTools.SetField(stats, "wallet", wallet);
        SetupTools.SetField(stats, "timer", timer);
        SetupTools.SetField(stats, "dumper", dumper);

        HudMessageLine messages = ui.AddComponent<HudMessageLine>();
        SetupTools.SetField(messages, "settings", day);
        SetupTools.SetField(messages, "shop", shop);
        SetupTools.SetField(messages, "wrongPick", LandfillAssets.WrongPick);
        SetupTools.SetField(messages, "tableFull", LandfillAssets.TableFull);

        SetupTools.SetField(ui.AddComponent<TargetCard>(), "settings", day);

        ShopBar shopBar = ui.AddComponent<ShopBar>();
        SetupTools.SetField(shopBar, "shop", shop);
        LandfillAssets.Machine("Sieve", 100, "small items fall to the waste", CreateSievePrefab());
        LandfillAssets.Machine("Blower", 50, "light items fly to the waste", CreateBlowerPrefab(day));
        SetupTools.SetArray(shopBar, "machines", LandfillAssets.AllMachines());

        TablePanel tablePanel = ui.AddComponent<TablePanel>();
        SetupTools.SetField(tablePanel, "table", table.GetComponent<TableContents>());
        SetupTools.SetField(tablePanel, "decisions", table.GetComponent<SortingDecisions>());

        LandfillEndPanel end = ui.AddComponent<LandfillEndPanel>();
        SetupTools.SetField(end, "dayWon", LandfillAssets.DayWon);
        SetupTools.SetField(end, "dayLost", LandfillAssets.DayLost);
    }
}
