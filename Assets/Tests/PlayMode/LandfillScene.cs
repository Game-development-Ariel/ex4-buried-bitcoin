using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Loads the real Landfill scene for a PlayMode test and gives the test its parts,
/// so a test reads like the game: Table.ItemCount, Shop.Select(...), Round, Timer.
/// </summary>
public class LandfillScene
{
    public const string ScenePath = "Assets/Scenes/Landfill.unity";

    // The table area from LandfillSceneBuilder: the top of the table and the middle of the space above it.
    public static readonly Vector2 OnTheTable = new Vector2(6f, -1.1f);

    public LandfillDaySettings Settings { get; private set; }
    public LandfillRound Round { get; private set; }
    public DayTimer Timer { get; private set; }
    public Wallet Wallet { get; private set; }
    public MachineShop Shop { get; private set; }
    public MachineSlot[] Slots { get; private set; }
    public TableContents Table { get; private set; }
    public SortingDecisions Decisions { get; private set; }
    public ConveyorBelt Belt { get; private set; }
    public SurfaceEffector2D BeltSurface { get; private set; }
    public TruckDumper Dumper { get; private set; }
    public WorldClicker Clicker { get; private set; }
    public TrashItem ItemPrefab { get; private set; }

    public GameEvent DayWon => TestUtils.GetField<GameEvent>(Round, "dayWon");
    public DayLostEvent DayLost => TestUtils.GetField<DayLostEvent>(Round, "dayLost");
    public GameEvent TableFull => TestUtils.GetField<GameEvent>(Table, "tableFull");
    public GameEvent TableHasRoom => TestUtils.GetField<GameEvent>(Table, "tableHasRoom");
    public bool TruckIsWaiting => TestUtils.GetField<bool>(Dumper, "isWaiting");

    /// <summary>Loads the scene and waits one frame, so every Awake, OnEnable and Start has run.</summary>
    public static IEnumerator Load(LandfillScene scene, int seed = 7)
    {
        UnityEngine.Random.InitState(seed);
#if UNITY_EDITOR
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        yield return SceneManager.LoadSceneAsync("Landfill");
#endif
        yield return null;
        scene.FindParts();
    }

    public void FindParts()
    {
        Round = One<LandfillRound>();
        Timer = One<DayTimer>();
        Wallet = One<Wallet>();
        Shop = One<MachineShop>();
        Table = One<TableContents>();
        Decisions = One<SortingDecisions>();
        Belt = One<ConveyorBelt>();
        BeltSurface = Belt.GetComponent<SurfaceEffector2D>();
        Dumper = One<TruckDumper>();
        Clicker = One<WorldClicker>();
        Settings = TestUtils.GetField<LandfillDaySettings>(Round, "settings");
        Slots = TestUtils.GetField<MachineSlot[]>(Shop, "slots");
        ItemPrefab = TestUtils.GetField<TrashItem>(Dumper, "itemPrefab");
    }

    private static T One<T>() where T : UnityEngine.Object
    {
        T[] found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        Assert.AreEqual(1, found.Length, "The scene should have one " + typeof(T).Name);
        return found[0];
    }

    /// <summary>An item from the day's trucks that matches, like the first small one or the lightest one.</summary>
    public TrashItemData ItemFromTrucks(Func<TrashItemData, bool> match, Func<TrashItemData, float> orderBy = null)
    {
        var items = Settings.trucks.SelectMany(truck => truck.entries).Select(entry => entry.item).Distinct().Where(match);
        if (orderBy != null)
        {
            items = items.OrderBy(orderBy);
        }
        TrashItemData found = items.FirstOrDefault();
        Assert.IsNotNull(found, "No item in the trucks matches");
        return found;
    }

    /// <summary>Creates an item from the real prefab, like the truck does, but at a chosen place and without a throw.</summary>
    public TrashItem Spawn(TrashItemData data, Vector2 position)
    {
        TrashItem item = UnityEngine.Object.Instantiate(ItemPrefab, position, Quaternion.identity);
        item.Setup(data);
        return item;
    }

    /// <summary>Puts an item on the table and waits until the table counts it.</summary>
    public IEnumerator PutOnTable(TrashItem item, float timeout = 3f)
    {
        float end = Time.time + timeout;
        while (Time.time < end && !IsOnTable(item))
        {
            yield return new WaitForFixedUpdate();
        }
        Assert.IsTrue(IsOnTable(item), item.name + " never reached the table");
    }

    public bool IsOnTable(TrashItem item)
    {
        return TestUtils.GetField<System.Collections.Generic.List<TrashItem>>(Table, "items").Contains(item);
    }

    /// <summary>Waits game time (Time.timeScale makes it faster) until the condition is true, or fails after the timeout.</summary>
    public static IEnumerator WaitUntil(Func<bool> condition, float timeoutGameSeconds, string what)
    {
        float end = Time.time + timeoutGameSeconds;
        while (!condition() && Time.time < end)
        {
            yield return null;
        }
        Assert.IsTrue(condition(), "Timed out waiting for: " + what);
    }

    /// <summary>
    /// Unloads the Landfill scene, so the next test doesn't share the world with a running day.
    /// An empty scene is made first, because Unity always needs one loaded scene.
    /// </summary>
    public static IEnumerator Unload()
    {
        CleanUpLeftovers();
        Scene empty = SceneManager.CreateScene("Empty Test Scene " + Time.frameCount);
        SceneManager.SetActiveScene(empty);
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene != empty && scene.name != "DontDestroyOnLoad")
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
    }

    /// <summary>
    /// Removes what the scene left behind that survives a scene load (the fader and the sound player),
    /// and sets the clock back to normal speed with the fixed test frame.
    /// </summary>
    public static void CleanUpLeftovers()
    {
        Time.timeScale = 1f;
        Time.captureDeltaTime = PlayModeTestEnvironment.FrameSeconds;
        foreach (string name in new[] { "Screen Fader", "Sound Effects" })
        {
            GameObject leftover = GameObject.Find(name);
            if (leftover != null)
            {
                UnityEngine.Object.Destroy(leftover);
            }
        }
        TestUtils.SetStaticField(typeof(ScreenFader), "isFading", false);
    }
}
