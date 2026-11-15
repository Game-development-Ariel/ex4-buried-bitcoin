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

    // The camera: half the screen height in world units, and a spot a bit up,
    // so the plant sits under the top bar of the UI.
    private const float CameraSize = 5.4f;
    private static readonly Vector3 CameraPosition = new Vector3(0f, 0.5f, SceneParts.CameraZ);

    // Sorting orders of the boxes, from back to front: the pit background; the pit floor, hood, walls, sign frames and the wind;
    // the belt and the table top; machine parts, the truck and the plus signs; an item's outline, then the item.
    // World labels are drawn over all of them (SceneParts).
    private const int BackgroundLayer = 0;
    private const int BackLayer = 1;
    private const int MiddleLayer = 2;
    private const int FrontLayer = 3;
    private const int OutlineLayer = 5;
    private const int ItemLayer = 6;

    private static readonly Color Sky = new Color(0.16f, 0.18f, 0.22f);
    private static readonly Color Metal = new Color(0.3f, 0.32f, 0.36f);
    private static readonly Color BeltColor = new Color(0.44f, 0.46f, 0.5f);
    private static readonly Color Dirt = new Color(0.23f, 0.17f, 0.12f);
    private static readonly Color SignColor = new Color(1f, 0.85f, 0.4f, 0.55f);
    private static readonly Color LabelColor = new Color(0.8f, 0.78f, 0.72f);

    // The waste pit and the hood above the belt (center and size in world units), and the walls around the plant.
    private static readonly Vector2 PitBackgroundCenter = new Vector2(-2.4f, -1f);
    private static readonly Vector2 PitBackgroundSize = new Vector2(12.2f, 2.9f);
    private static readonly Vector2 PitFloorCenter = new Vector2(-2.4f, -2.45f);
    private static readonly Vector2 PitFloorSize = new Vector2(12.2f, 0.3f);
    private static readonly Color PitFloorColor = Dirt * 0.7f; // A darker dirt.
    private static readonly Vector2 HoodCenter = new Vector2(-2.45f, 4.25f);
    private static readonly Vector2 HoodSize = new Vector2(11.9f, 0.25f);
    private static readonly Vector2 WasteLabelPosition = new Vector2(-2.4f, -1.5f);
    private const float WasteLabelHeight = 0.5f;
    private static readonly Color WasteLabelColor = LabelColor * 0.7f; // A dimmer label, so it stays in the background.
    private static readonly Vector2 LeftWallCenter = new Vector2(-8.5f, 1f);
    private static readonly Vector2 LeftWallSize = new Vector2(0.2f, 7f);
    private static readonly Vector2 PitWallCenter = new Vector2(3.65f, -0.95f);
    private static readonly Vector2 PitWallSize = new Vector2(0.3f, 3f);
    private static readonly Vector2 TableWallCenter = new Vector2(8.4f, 0.4f);
    private static readonly Vector2 TableWallSize = new Vector2(0.2f, 4.3f);

    // The invisible borders far outside the screen, in world units: the y of the bottom and top ones,
    // the x of the side ones (left is negative) and their center y, the length of the lying
    // and standing ones, and how thick all of them are.
    private const float BorderBottomY = -9f;
    private const float BorderTopY = 11f;
    private const float BorderSideX = 16f;
    private const float BorderSideCenterY = 1f;
    private const float LyingBorderLength = 40f;
    private const float StandingBorderLength = 20f;
    private const float BorderThickness = 1f;

    // The sign above each slot and machine, relative to the slot (on the belt top), in world units.
    // The "slot" text sits this far from the sign's center.
    private static readonly Vector2 SignCenter = new Vector2(0f, 2.3f);
    private static readonly Vector2 SignSize = new Vector2(1.5f, 1.1f);
    private static readonly Color SignFrameColor = new Color(1f, 1f, 1f, 0.08f);
    private const float PlusLength = 0.6f;
    private const float PlusThickness = 0.14f;
    private static readonly Vector2 SlotLabelBelowSign = new Vector2(0f, -0.38f);
    private const float SlotLabelHeight = 0.22f;

    // The truck, in world units. The bed and the chute are placed relative to the truck.
    private static readonly Vector3 TruckPosition = new Vector3(-7.3f, 2.6f, 0f);
    private static readonly Vector2 TruckBedCenter = new Vector2(-0.3f, 1.1f);
    private static readonly Vector2 TruckBedSize = new Vector2(1.7f, 1f);
    private static readonly Color TruckColor = new Color(0.85f, 0.7f, 0.2f);
    private static readonly Vector2 ChuteCenter = new Vector2(0.1f, 0.45f);
    private static readonly Vector2 ChuteSize = new Vector2(0.9f, 0.2f);

    // The sorting table, in world units. The area is the trigger above the table top.
    private static readonly Vector2 TableTopCenter = new Vector2(6.02f, -1.6f);
    private static readonly Vector2 TableTopSize = new Vector2(4.55f, 0.3f);
    private static readonly Color TableColor = new Color(0.45f, 0.36f, 0.26f);
    private static readonly Vector2 TableLabelPosition = new Vector2(6f, -2.2f);
    private const float TableLabelHeight = 0.4f;
    private static readonly Vector2 TableAreaCenter = new Vector2(6.05f, 0.6f);
    private static readonly Vector2 TableAreaSize = new Vector2(4.4f, 4.1f);

    // The outline of a selected item is this many times the size of the item.
    private const float OutlineScale = 1.3f;

    // The machines, in world units: how wide a stretch of belt each one works on (the sieve trigger, the wind column),
    // and how tall its label is.
    private const float MachineAreaWidth = 1.6f;
    private const float MachineLabelHeight = 0.3f;

    // The sieve: its trigger just above the belt, and the dark bars across the belt (first center, center to center, width).
    private static readonly Vector2 SieveTriggerCenter = new Vector2(0f, 0.3f);
    private static readonly Vector2 SieveTriggerSize = new Vector2(MachineAreaWidth, 0.6f);
    private const int SieveBarCount = 6;
    private const float FirstBarCenterX = -0.7f;
    private const float BarSpacing = 0.28f;
    private const float BarWidth = 0.1f;

    // The blower: the column of wind it pushes up, and the fan under the belt.
    private const float StraightUpDegrees = 90f;
    private static readonly Vector2 WindCenter = new Vector2(0f, 1.5f);
    private static readonly Vector2 WindSize = new Vector2(MachineAreaWidth, 3f);
    private static readonly Color WindColor = new Color(0.6f, 0.8f, 1f, 0.1f);
    private static readonly Vector2 FanCenter = new Vector2(0f, -0.65f);
    private static readonly Vector2 FanSize = new Vector2(1.4f, 0.6f);
    private static readonly Color FanColor = new Color(0.35f, 0.5f, 0.65f);

    // What the machines cost in the shop, in dollars. Written only when the machine asset is new.
    private const int SievePrice = 100;
    private const int BlowerPrice = 50;

    public static void Build()
    {
        Directory.CreateDirectory(PrefabFolder);
        Scene scene = SceneParts.NewScene();
        Camera camera = SceneParts.CreateCamera(Sky, CameraSize);
        camera.transform.position = CameraPosition;
        SceneParts.CreateGlobalLight();
        LandfillDaySettings day = LandfillAssets.DaySettings();

        ConveyorBelt belt = CreateBelt(day);
        CreateWasteAndWalls(day);
        CreateBorders(day);
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
        GameObject belt = SceneParts.CreateBox("Belt", null, center, new Vector2(width, BeltThickness), BeltColor, MiddleLayer);

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

        SceneParts.CreateBox("Pit Background", parent, PitBackgroundCenter, PitBackgroundSize, Dirt, BackgroundLayer);
        AddWastePile(day, SceneParts.CreateBox("Waste Pit", parent, PitFloorCenter, PitFloorSize, PitFloorColor, BackLayer));
        AddWastePile(day, SceneParts.CreateBox("Waste Hood", parent, HoodCenter, HoodSize, Metal, BackLayer));
        SceneParts.CreateLabel("WASTE", parent, WasteLabelPosition, WasteLabelHeight, WasteLabelColor);

        AddWall(parent, "Left Wall", LeftWallCenter, LeftWallSize);
        AddWall(parent, "Pit Wall", PitWallCenter, PitWallSize);
        AddWall(parent, "Table Wall", TableWallCenter, TableWallSize);
    }

    /// <summary>
    /// Invisible waste piles far outside the screen. An item that flies over a wall would fall forever,
    /// and if it is the target the day could never be won. Here it is buried like any other item.
    /// </summary>
    private static void CreateBorders(LandfillDaySettings day)
    {
        var parent = new GameObject("Borders").transform;
        var lying = new Vector2(LyingBorderLength, BorderThickness);
        var standing = new Vector2(BorderThickness, StandingBorderLength);
        AddBorder(day, parent, "Bottom", new Vector2(0f, BorderBottomY), lying);
        AddBorder(day, parent, "Top", new Vector2(0f, BorderTopY), lying);
        AddBorder(day, parent, "Left", new Vector2(-BorderSideX, BorderSideCenterY), standing);
        AddBorder(day, parent, "Right", new Vector2(BorderSideX, BorderSideCenterY), standing);
    }

    private static void AddBorder(LandfillDaySettings day, Transform parent, string name, Vector2 center, Vector2 size)
    {
        var border = new GameObject(name + " Border");
        border.transform.SetParent(parent, false);
        border.transform.localPosition = center;
        border.transform.localScale = new Vector3(size.x, size.y, 1f);
        AddWastePile(day, border);
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
        SceneParts.CreateBox(name, parent, center, size, Metal, BackLayer).AddComponent<BoxCollider2D>();
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
            BoxCollider2D clickArea = slotObject.AddComponent<BoxCollider2D>();
            clickArea.isTrigger = true;
            clickArea.offset = SignCenter;
            clickArea.size = SignSize;
            SceneParts.CreateBox("Frame", slotObject.transform, SignCenter, SignSize, SignFrameColor, BackLayer);

            var emptySign = new GameObject("Empty Sign");
            emptySign.transform.SetParent(slotObject.transform, false);
            SceneParts.CreateBox("Plus A", emptySign.transform, SignCenter, new Vector2(PlusLength, PlusThickness), SignColor, FrontLayer);
            SceneParts.CreateBox("Plus B", emptySign.transform, SignCenter, new Vector2(PlusThickness, PlusLength), SignColor, FrontLayer);
            SceneParts.CreateLabel("slot", emptySign.transform, SignCenter + SlotLabelBelowSign, SlotLabelHeight, SignColor);

            slots[i] = slotObject.AddComponent<MachineSlot>();
            SetupTools.SetField(slots[i], "belt", belt);
            SetupTools.SetField(slots[i], "emptySign", emptySign);
        }
        return slots;
    }

    private static TruckDumper CreateDumper(LandfillDaySettings day)
    {
        var parent = new GameObject("Truck").transform;
        parent.position = TruckPosition;
        SceneParts.CreateBox("Truck Bed", parent, TruckBedCenter, TruckBedSize, TruckColor, FrontLayer);
        SceneParts.CreateBox("Chute", parent, ChuteCenter, ChuteSize, Metal, FrontLayer);

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
        SceneParts.CreateBox("Table Top", parent, TableTopCenter, TableTopSize, TableColor, MiddleLayer)
            .AddComponent<BoxCollider2D>();
        SceneParts.CreateLabel("SORTING TABLE", parent, TableLabelPosition, TableLabelHeight, LabelColor);

        // The trigger covers the space above the table. Everything inside it counts as "on the table".
        BoxCollider2D area = parent.gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.offset = TableAreaCenter;
        area.size = TableAreaSize;

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
        GameObject item = SceneParts.CreateBox("TrashItem", null, Vector2.zero, Vector2.one, Color.white, ItemLayer);
        item.AddComponent<Rigidbody2D>();
        item.AddComponent<BoxCollider2D>();

        GameObject outline = SceneParts.CreateBox("Outline", item.transform, Vector2.zero, Vector2.one * OutlineScale, Color.white, OutlineLayer);
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
        trigger.offset = SieveTriggerCenter;
        trigger.size = SieveTriggerSize;
        sieve.AddComponent<Sieve>();

        for (int i = 0; i < SieveBarCount; i++)
        {
            float x = FirstBarCenterX + i * BarSpacing;
            SceneParts.CreateBox("Bar", sieve.transform, new Vector2(x, -BeltThickness / 2f), new Vector2(BarWidth, BeltThickness), Sky, FrontLayer);
        }
        SceneParts.CreateLabel("SIEVE", sieve.transform, SignCenter, MachineLabelHeight, LabelColor);
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
        column.offset = WindCenter;
        column.size = WindSize;

        AreaEffector2D wind = blower.AddComponent<AreaEffector2D>();
        wind.useGlobalAngle = true;
        wind.forceAngle = StraightUpDegrees;
        wind.forceMagnitude = day.blowerForce;

        SceneParts.CreateBox("Wind", blower.transform, WindCenter, WindSize, WindColor, BackLayer);
        SceneParts.CreateBox("Fan", blower.transform, FanCenter, FanSize, FanColor, FrontLayer);
        SceneParts.CreateLabel("BLOWER", blower.transform, SignCenter, MachineLabelHeight, LabelColor);
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
        LandfillAssets.Machine("Sieve", SievePrice, "small items fall to the waste", CreateSievePrefab());
        LandfillAssets.Machine("Blower", BlowerPrice, "light items fly to the waste", CreateBlowerPrefab(day));
        SetupTools.SetArray(shopBar, "machines", LandfillAssets.AllMachines());

        TablePanel tablePanel = ui.AddComponent<TablePanel>();
        SetupTools.SetField(tablePanel, "table", table.GetComponent<TableContents>());
        SetupTools.SetField(tablePanel, "decisions", table.GetComponent<SortingDecisions>());

        LandfillEndPanel end = ui.AddComponent<LandfillEndPanel>();
        SetupTools.SetField(end, "dayWon", LandfillAssets.DayWon);
        SetupTools.SetField(end, "dayLost", LandfillAssets.DayLost);
    }
}
