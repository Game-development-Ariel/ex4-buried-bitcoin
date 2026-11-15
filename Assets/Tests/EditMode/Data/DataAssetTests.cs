using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks the real assets in Assets/Data and Assets/Prefabs: they load, nothing required is empty,
/// and the numbers make sense. These catch a broken asset before the game is played.
/// </summary>
public class DataAssetTests
{
    private const string DataFolder = "Assets/Data";
    private const string SettingsPath = "Assets/Data/Day1Settings.asset";

    private static LandfillDaySettings Day => AssetDatabase.LoadAssetAtPath<LandfillDaySettings>(SettingsPath);

    private static IEnumerable<string> DataAssetPaths()
    {
        return AssetDatabase.FindAssets("", new[] { DataFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".asset"))
            .OrderBy(path => path);
    }

    private static T[] All<T>() where T : Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name)
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .ToArray();
    }

    private static IEnumerable<TrashItemData> TruckItems()
    {
        return Day.trucks.SelectMany(truck => truck.entries).Select(entry => entry.item);
    }

    [TestCaseSource(nameof(DataAssetPaths))]
    public void EveryDataAsset_LoadsWithItsScript(string path)
    {
        Object asset = AssetDatabase.LoadMainAssetAtPath(path);

        Assert.IsNotNull(asset, path + " doesn't load");
        Assert.IsInstanceOf<ScriptableObject>(asset);
    }

    [Test]
    public void TheDataFolder_HasTheAssetsTheReadmeLists()
    {
        Assert.AreEqual(1, All<LandfillDaySettings>().Length, "One day settings asset");
        Assert.AreEqual(6, All<GameEvent>().Length, "TableFull, TableHasRoom, TargetFound, TargetLost, WrongPick, DayWon");
        Assert.AreEqual(1, All<DayLostEvent>().Length);
        Assert.AreEqual(7, All<TrashItemData>().Length);
        Assert.AreEqual(2, All<MachineData>().Length);
    }

    // ---------- Day settings ----------

    [Test]
    public void DaySettings_RulesAreSensible()
    {
        LandfillDaySettings day = Day;

        Assert.GreaterOrEqual(day.startMoney, 0);
        Assert.Greater(day.timeLimitSeconds, 0f);
        Assert.That(day.warningSeconds, Is.GreaterThan(0f).And.LessThan(day.timeLimitSeconds));
        Assert.That(day.wrongPickPenaltySeconds, Is.GreaterThan(0f).And.LessThan(day.timeLimitSeconds));
        Assert.Greater(day.tableCapacity, 0);
        Assert.That(day.freePlacesToRestart, Is.InRange(1, day.tableCapacity));
    }

    [Test]
    public void DaySettings_LineNumbersArePositive()
    {
        LandfillDaySettings day = Day;

        Assert.Greater(day.beltSpeed, 0f);
        Assert.Greater(day.blowerForce, 0f);
        Assert.GreaterOrEqual(day.secondsBeforeTruck, 0f);
        Assert.Greater(day.dumpInterval, 0f);
        Assert.GreaterOrEqual(day.throwSpin, 0f);
    }

    [Test]
    public void DaySettings_HasTrucksAndEveryEntryHasAnItem()
    {
        LandfillDaySettings day = Day;

        Assert.IsNotEmpty(day.trucks);
        for (int truck = 0; truck < day.trucks.Length; truck++)
        {
            Assert.IsNotEmpty(day.trucks[truck].entries, $"Truck {truck + 1} is empty");
            foreach (TruckLoad.Entry entry in day.trucks[truck].entries)
            {
                Assert.IsNotNull(entry.item, $"Truck {truck + 1} has an entry with no item");
                Assert.Greater(entry.count, 0, $"Truck {truck + 1}: {entry.item.name} has count {entry.count}");
            }
        }
    }

    [Test]
    public void DaySettings_TargetIsAnItemWithADescriptionForTheCard()
    {
        TrashItemData target = Day.target;

        Assert.IsNotNull(target);
        Assert.IsInstanceOf<TrashItemData>(target);
        Assert.IsNotEmpty(target.description);
    }

    [Test]
    public void Regression_TheTargetIsInTheTrucksExactlyOnce()
    {
        int count = Day.trucks.SelectMany(truck => truck.entries).Where(entry => entry.item == Day.target).Sum(entry => entry.count);

        Assert.AreEqual(1, count, "The day can only be won if the target arrives, and there is only one target");
    }

    [Test]
    public void TheDaysPlan_ShuffledTrucksBringTheTargetExactlyOnce()
    {
        for (int seed = 0; seed < 5; seed++)
        {
            Random.InitState(seed);
            List<TrashItemData> all = Day.trucks.SelectMany(truck => truck.ShuffledItems()).ToList();

            Assert.AreEqual(1, all.Count(item => item == Day.target));
            Assert.AreEqual(Day.trucks.Sum(truck => truck.entries.Sum(entry => entry.count)), all.Count);
        }
    }

    [Test]
    public void TheDaysPlan_BringsMoreItemsThanTheTableHolds()
    {
        int total = Day.trucks.Sum(truck => truck.entries.Sum(entry => entry.count));

        Assert.Greater(total, Day.tableCapacity, "Otherwise the table never fills and the machines are pointless");
    }

    [Test]
    public void TheDaysPlan_UsesEveryItemAsset()
    {
        CollectionAssert.IsSubsetOf(All<TrashItemData>(), TruckItems().Distinct().ToArray());
    }

    // ---------- Items ----------

    [TestCaseSource(nameof(ItemPaths))]
    public void EveryItem_HasANameAndAPositiveSizeAndMass(string path)
    {
        TrashItemData item = AssetDatabase.LoadAssetAtPath<TrashItemData>(path);

        Assert.IsNotEmpty(item.displayName);
        Assert.Greater(item.size, 0f);
        Assert.Greater(item.mass, 0f);
        Assert.GreaterOrEqual(item.sellPrice, 0);
        Assert.AreEqual(1f, item.color.a, 0.001f, "A see-through item is hard to find");
    }

    private static IEnumerable<string> ItemPaths()
    {
        return AssetDatabase.FindAssets("t:TrashItemData", new[] { DataFolder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path);
    }

    [Test]
    public void TheTarget_IsNotSmall_SoTheSieveNeverDropsIt()
    {
        Assert.IsFalse(Day.target.isSmall);
    }

    [Test]
    public void SomeItemsAreSmall_SoTheSieveHasWorkToDo()
    {
        Assert.IsTrue(All<TrashItemData>().Any(item => item.isSmall));
    }

    [Test]
    public void TheBlower_LiftsTheLightestItemButNotTheTarget()
    {
        float gravity = -Physics2D.gravity.y;
        float lightest = All<TrashItemData>().Min(item => item.mass);

        Assert.Greater(Day.blowerForce, lightest * gravity, "The blower must lift the lightest item");
        Assert.Less(Day.blowerForce, Day.target.mass * gravity, "The blower must not lift the target");
    }

    // ---------- Machines ----------

    [Test]
    public void EveryMachine_HasANamePricePrefabAndDescription()
    {
        foreach (MachineData machine in All<MachineData>())
        {
            Assert.IsNotEmpty(machine.displayName, machine.name);
            Assert.Greater(machine.price, 0, machine.name);
            Assert.IsNotNull(machine.prefab, machine.name);
            Assert.IsNotEmpty(machine.description, machine.name);
        }
    }

    [Test]
    public void TheStartMoney_BuysAtLeastOneMachine()
    {
        Assert.LessOrEqual(All<MachineData>().Min(machine => machine.price), Day.startMoney);
    }

    [Test]
    public void AllMachines_FindsEveryMachineAsset()
    {
        CollectionAssert.AreEquivalent(All<MachineData>(), LandfillAssets.AllMachines());
    }

    [Test]
    public void Regression_AllMachines_IsMostExpensiveFirst_SoTheSieveComesBeforeTheBlower()
    {
        MachineData[] machines = LandfillAssets.AllMachines();

        for (int i = 1; i < machines.Length; i++)
        {
            Assert.GreaterOrEqual(machines[i - 1].price, machines[i].price);
        }
        Assert.AreEqual("Sieve", machines[0].displayName);
    }

    [Test]
    public void TheSievePrefab_HasASieveAndATrigger()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Sieve.prefab");

        Assert.IsNotNull(prefab.GetComponent<Sieve>());
        Assert.IsTrue(prefab.GetComponent<Collider2D>().isTrigger);
    }

    [Test]
    public void Regression_TheBlowerPrefabForce_ComesFromTheDaySettings()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Blower.prefab");
        AreaEffector2D wind = prefab.GetComponent<AreaEffector2D>();

        Assert.AreEqual(Day.blowerForce, wind.forceMagnitude, 0.0001f);
        Assert.AreEqual(90f, wind.forceAngle, 0.0001f, "The blower blows straight up");
        Assert.IsTrue(wind.useGlobalAngle);
        Assert.IsTrue(prefab.GetComponent<Collider2D>().usedByEffector);
    }

    [Test]
    public void TheItemPrefab_IsBuiltForTrashItem()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrashItem.prefab");
        TrashItem item = prefab.GetComponent<TrashItem>();

        Assert.IsNotNull(item);
        Assert.IsNotNull(prefab.GetComponent<Rigidbody2D>());
        Assert.IsNotNull(prefab.GetComponent<Collider2D>());
        Assert.IsFalse(prefab.GetComponent<Collider2D>().isTrigger);
        GameObject outline = TestUtils.GetField<GameObject>(item, "outline");
        Assert.IsNotNull(outline);
        Assert.IsFalse(outline.activeSelf, "Items start without the highlight");
    }

    // ---------- Project settings the code relies on ----------

    [Test]
    public void Physics2D_SendsCallbacksOnDisable_SoADestroyedItemLeavesTheTable()
    {
        Assert.IsTrue(Physics2D.callbacksOnDisable, "TableContents relies on OnTriggerExit2D for destroyed items (see the README)");
    }

    [Test]
    public void TheLandfillScene_IsTheFirstSceneInTheBuild()
    {
        Assert.IsNotEmpty(EditorBuildSettings.scenes);
        Assert.AreEqual("Assets/Scenes/Landfill.unity", EditorBuildSettings.scenes[0].path);
        Assert.IsTrue(EditorBuildSettings.scenes[0].enabled);
    }
}
