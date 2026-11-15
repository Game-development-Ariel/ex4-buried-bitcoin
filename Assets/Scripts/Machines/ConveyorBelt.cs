using UnityEngine;

/// <summary>
/// The moving belt. A SurfaceEffector2D pushes everything that lies on it.
/// The belt stops while the sorting table is full, and when the component is turned off.
/// </summary>
[RequireComponent(typeof(SurfaceEffector2D))]
public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] private LandfillDaySettings settings;

    [Header("Listens to")]
    [SerializeField] private GameEvent tableFull;
    [SerializeField] private GameEvent tableHasRoom;

    private SurfaceEffector2D effector;

    private void Awake()
    {
        effector = GetComponent<SurfaceEffector2D>();
        Run();
    }

    private void OnEnable()
    {
        tableFull.Subscribe(Stop);
        tableHasRoom.Subscribe(Run);
    }

    // The round turns the belt off when the day ends, and a turned-off belt must not keep pushing.
    private void OnDisable()
    {
        tableFull.Unsubscribe(Stop);
        tableHasRoom.Unsubscribe(Run);
        Stop();
    }

    private void Run()
    {
        effector.speed = settings.beltSpeed;
    }

    private void Stop()
    {
        effector.speed = 0f;
    }
}
