using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Opens the real Landfill scene (without playing it) and checks that everything is connected.
/// </summary>
public class SceneWiringTests
{
    private const string ScenePath = "Assets/Scenes/Landfill.unity";

    // Fields that may stay empty on purpose. Empty today: nothing.
    private static readonly HashSet<string> AllowedEmpty = new HashSet<string>();

    private Scene scene;

    [OneTimeSetUp]
    public void OpenScene()
    {
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
    }

    [OneTimeTearDown]
    public void CloseScene()
    {
        EditorSceneManager.CloseScene(scene, true);
    }

    private T[] Find<T>() where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    private T One<T>() where T : Component
    {
        T[] found = Find<T>();
        Assert.AreEqual(1, found.Length, $"The scene should have exactly one {typeof(T).Name}");
        return found[0];
    }

    [Test]
    public void TheScene_HasNoMissingScripts()
    {
        CollectionAssert.IsEmpty(WiringCheck.MissingScripts(scene.GetRootGameObjects()));
    }

    [Test]
    public void TheScene_HasNoEmptyObjectFieldsOnOurComponents()
    {
        CollectionAssert.IsEmpty(WiringCheck.EmptyReferences(scene.GetRootGameObjects(), AllowedEmpty));
    }

    [TestCase("Assets/Prefabs/TrashItem.prefab")]
    [TestCase("Assets/Prefabs/Sieve.prefab")]
    [TestCase("Assets/Prefabs/Blower.prefab")]
    public void EveryPrefab_HasNoMissingScriptsOrEmptyFields(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        CollectionAssert.IsEmpty(WiringCheck.MissingScripts(new[] { prefab }));
        CollectionAssert.IsEmpty(WiringCheck.EmptyReferences(new[] { prefab }, AllowedEmpty));
    }

    [TestCase(typeof(LandfillRound))]
    [TestCase(typeof(DayTimer))]
    [TestCase(typeof(Wallet))]
    [TestCase(typeof(MachineShop))]
    [TestCase(typeof(TableContents))]
    [TestCase(typeof(SortingDecisions))]
    [TestCase(typeof(TruckDumper))]
    [TestCase(typeof(ConveyorBelt))]
    [TestCase(typeof(WorldClicker))]
    [TestCase(typeof(HudStats))]
    [TestCase(typeof(HudMessageLine))]
    [TestCase(typeof(TargetCard))]
    [TestCase(typeof(ShopBar))]
    [TestCase(typeof(TablePanel))]
    [TestCase(typeof(LandfillEndPanel))]
    public void TheScene_HasExactlyOne(System.Type type)
    {
        int count = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren(type, true).Length);

        Assert.AreEqual(1, count);
    }

    [Test]
    public void TheScene_HasThreeEmptyMachineSlotsOnTheBelt()
    {
        MachineSlot[] slots = Find<MachineSlot>();
        Collider2D belt = One<ConveyorBelt>().GetComponent<Collider2D>();

        Assert.AreEqual(3, slots.Length);
        CollectionAssert.AreEquivalent(slots, TestUtils.GetField<MachineSlot[]>(One<MachineShop>(), "slots"), "The shop listens to every slot");
        foreach (MachineSlot slot in slots)
        {
            Assert.AreSame(belt, slot.Belt);
        }
    }

    [Test]
    public void Regression_TheDayEndStopsTheBeltTheTruckTheTimerAndTheClicks()
    {
        Behaviour[] parts = TestUtils.GetField<Behaviour[]>(One<LandfillRound>(), "stopWhenDayEnds");

        CollectionAssert.AreEquivalent(new Behaviour[] { One<ConveyorBelt>(), One<TruckDumper>(), One<DayTimer>(), One<WorldClicker>() }, parts);
    }

    [Test]
    public void Regression_TheShopBarListsEveryMachine_MostExpensiveFirst()
    {
        MachineData[] machines = TestUtils.GetField<MachineData[]>(One<ShopBar>(), "machines");

        CollectionAssert.AreEqual(LandfillAssets.AllMachines(), machines);
    }

    [Test]
    public void EveryComponent_UsesTheSameDaySettings()
    {
        LandfillDaySettings day = AssetDatabase.LoadAssetAtPath<LandfillDaySettings>("Assets/Data/Day1Settings.asset");

        foreach (MonoBehaviour component in Find<MonoBehaviour>())
        {
            var fields = new SerializedObject(component);
            SerializedProperty settings = fields.FindProperty("settings");
            if (settings != null)
            {
                Assert.AreSame(day, settings.objectReferenceValue, component.GetType().Name);
            }
        }
    }

    [Test]
    public void TheTableAndTheBelt_ShareTheSameTableEvents()
    {
        TableContents table = One<TableContents>();
        var full = TestUtils.GetField<GameEvent>(table, "tableFull");
        var room = TestUtils.GetField<GameEvent>(table, "tableHasRoom");

        Assert.AreSame(full, TestUtils.GetField<GameEvent>(One<ConveyorBelt>(), "tableFull"));
        Assert.AreSame(room, TestUtils.GetField<GameEvent>(One<ConveyorBelt>(), "tableHasRoom"));
        Assert.AreSame(full, TestUtils.GetField<GameEvent>(One<TruckDumper>(), "tableFull"));
        Assert.AreSame(room, TestUtils.GetField<GameEvent>(One<TruckDumper>(), "tableHasRoom"));
        Assert.AreNotSame(full, room);
    }

    [Test]
    public void TheTable_HasATriggerOverIt()
    {
        Collider2D area = One<TableContents>().GetComponents<Collider2D>().Single(collider => collider.isTrigger);

        Assert.Greater(area.bounds.size.x, 1f);
    }

    [Test]
    public void Regression_FourBordersOutsideTheScreenBuryItemsThatEscape()
    {
        WastePile[] piles = Find<WastePile>();
        WastePile[] borders = piles.Where(pile => pile.name.EndsWith("Border")).ToArray();

        Assert.AreEqual(4, borders.Length);
        Assert.AreEqual(6, piles.Length, "The pit, the hood and the four borders");
        foreach (WastePile border in borders)
        {
            Assert.IsFalse(border.GetComponent<Collider2D>().isTrigger, border.name);
        }
    }

    [Test]
    public void TheCamera_IsWhatWorldClickerUses()
    {
        WorldClicker clicker = One<WorldClicker>();

        Assert.AreSame(clicker.GetComponent<Camera>(), TestUtils.GetField<Camera>(clicker, "worldCamera"));
    }

    [Test]
    public void TheBeltSurface_IsUsedByItsEffector()
    {
        Assert.IsTrue(One<ConveyorBelt>().GetComponent<Collider2D>().usedByEffector);
    }
}
