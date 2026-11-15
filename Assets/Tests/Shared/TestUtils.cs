using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Helpers shared by the EditMode and the PlayMode tests: setting private serialized fields,
/// calling Unity messages by name, and creating small test versions of the game's data and objects.
/// Everything created through a TestUtils instance is destroyed by DestroyAll, so a test leaves nothing behind.
/// </summary>
public class TestUtils
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly List<Object> created = new List<Object>();

    // ---------- Reflection ----------

    /// <summary>Sets a private field, the same way dragging a value into the Inspector does.</summary>
    public static void SetField(object target, string field, object value)
    {
        FindField(target.GetType(), field, AnyInstance).SetValue(target, value);
    }

    public static T GetField<T>(object target, string field)
    {
        return (T)FindField(target.GetType(), field, AnyInstance).GetValue(target);
    }

    public static void SetStaticField(Type type, string field, object value)
    {
        FindField(type, field, AnyStatic).SetValue(null, value);
    }

    public static T GetStaticField<T>(Type type, string field)
    {
        return (T)FindField(type, field, AnyStatic).GetValue(null);
    }

    /// <summary>Calls a method by name, also a private one like a Unity message (Awake, OnEnable, OnTriggerEnter2D).</summary>
    public static object Call(object target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(method, AnyInstance);
        if (info == null)
        {
            throw new MissingMethodException(target.GetType().Name, method);
        }
        return info.Invoke(target, arguments);
    }

    public static object CallStatic(Type type, string method, params object[] arguments)
    {
        MethodInfo info = type.GetMethod(method, AnyStatic);
        if (info == null)
        {
            throw new MissingMethodException(type.Name, method);
        }
        return info.Invoke(null, arguments);
    }

    /// <summary>How many methods listen to a C# event, read from the field the compiler creates for it.</summary>
    public static int ListenerCount(object target, string eventName)
    {
        var handler = GetField<Delegate>(target, eventName);
        return handler == null ? 0 : handler.GetInvocationList().Length;
    }

    private static FieldInfo FindField(Type type, string field, BindingFlags flags)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo info = current.GetField(field, flags | BindingFlags.DeclaredOnly);
            if (info != null)
            {
                return info;
            }
        }
        throw new MissingFieldException(type.Name, field);
    }

    // ---------- Creating things ----------

    /// <summary>Remembers an object so DestroyAll destroys it.</summary>
    public T Track<T>(T obj) where T : Object
    {
        created.Add(obj);
        return obj;
    }

    public T Asset<T>() where T : ScriptableObject
    {
        return Track(ScriptableObject.CreateInstance<T>());
    }

    public GameObject NewObject(string name = "Test Object", bool active = true)
    {
        var obj = Track(new GameObject(name));
        obj.SetActive(active);
        return obj;
    }

    /// <summary>
    /// Adds a component to a new inactive object and fills its fields before Awake runs.
    /// In PlayMode, Activate(component) then runs Awake and OnEnable, like loading a scene does.
    /// </summary>
    public T Component<T>(params (string field, object value)[] fields) where T : Component
    {
        GameObject obj = NewObject(typeof(T).Name, false);
        T component = obj.AddComponent<T>();
        foreach ((string field, object value) in fields)
        {
            SetField(component, field, value);
        }
        return component;
    }

    public static T Activate<T>(T component) where T : Component
    {
        component.gameObject.SetActive(true);
        return component;
    }

    /// <summary>Day settings with small, round numbers. Tests change the ones they care about.</summary>
    public LandfillDaySettings Settings(TrashItemData target = null)
    {
        LandfillDaySettings settings = Asset<LandfillDaySettings>();
        settings.startMoney = 100;
        settings.timeLimitSeconds = 150f;
        settings.warningSeconds = 20f;
        settings.wrongPickPenaltySeconds = 20f;
        settings.tableCapacity = 4;
        settings.freePlacesToRestart = 2;
        settings.beltSpeed = 2.5f;
        settings.blowerForce = 6f;
        settings.trucks = new TruckLoad[0];
        settings.secondsBeforeTruck = 0f;
        settings.dumpInterval = 0f;
        settings.target = target;
        return settings;
    }

    public TrashItemData ItemData(string name, int sellPrice = 0, bool isSmall = false, float mass = 1f, float size = 0.5f)
    {
        TrashItemData data = Asset<TrashItemData>();
        data.name = name;
        data.displayName = name;
        data.sellPrice = sellPrice;
        data.isSmall = isSmall;
        data.mass = mass;
        data.size = size;
        data.color = Color.cyan;
        data.description = "A test item called " + name;
        return data;
    }

    public MachineData Machine(string name, int price, GameObject prefab)
    {
        MachineData machine = Asset<MachineData>();
        machine.name = name;
        machine.displayName = name;
        machine.price = price;
        machine.description = "test machine " + name;
        machine.prefab = prefab;
        return machine;
    }

    public static TruckLoad Load(params (TrashItemData item, int count)[] entries)
    {
        var load = new TruckLoad { entries = new TruckLoad.Entry[entries.Length] };
        for (int i = 0; i < entries.Length; i++)
        {
            load.entries[i] = new TruckLoad.Entry { item = entries[i].item, count = entries[i].count };
        }
        return load;
    }

    /// <summary>
    /// A trash item built like the TrashItem prefab: a body, a sprite, a box collider and a hidden outline.
    /// Without data it is a template for TruckDumper, which calls Setup by itself.
    /// </summary>
    public TrashItem Item(TrashItemData data = null, Vector2 position = default, float gravityScale = 1f)
    {
        GameObject obj = NewObject("Trash Item", false);
        obj.transform.position = position;
        obj.AddComponent<SpriteRenderer>();
        obj.AddComponent<Rigidbody2D>().gravityScale = gravityScale;
        // Without a sprite the collider would be sized to nothing, so it gets the prefab's size: one unit.
        obj.AddComponent<BoxCollider2D>().size = Vector2.one;

        var outline = new GameObject("Outline");
        outline.transform.SetParent(obj.transform, false);
        outline.SetActive(false);

        TrashItem item = obj.AddComponent<TrashItem>();
        SetField(item, "outline", outline);
        if (data != null)
        {
            item.Setup(data);
            obj.SetActive(true);
        }
        return item;
    }

    public GameEvent Event()
    {
        return Asset<GameEvent>();
    }

    /// <summary>Destroys everything this helper created. DestroyImmediate works in EditMode and in PlayMode.</summary>
    public void DestroyAll()
    {
        for (int i = created.Count - 1; i >= 0; i--)
        {
            if (created[i] != null)
            {
                Object.DestroyImmediate(created[i]);
            }
        }
        created.Clear();
    }
}
