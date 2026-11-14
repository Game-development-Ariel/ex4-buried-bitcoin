using UnityEngine;

/// <summary>
/// The moving belt. A SurfaceEffector2D pushes everything that lies on it.
/// The belt stops while the sorting table is full.
/// </summary>
[RequireComponent(typeof(SurfaceEffector2D))]
public class ConveyorBelt : MonoBehaviour
{
    [Tooltip("World units per second")]
    [SerializeField] private float speed = 2.5f;

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

    private void OnDisable()
    {
        tableFull.Unsubscribe(Stop);
        tableHasRoom.Unsubscribe(Run);
    }

    private void Run()
    {
        effector.speed = speed;
    }

    private void Stop()
    {
        effector.speed = 0f;
    }
}
