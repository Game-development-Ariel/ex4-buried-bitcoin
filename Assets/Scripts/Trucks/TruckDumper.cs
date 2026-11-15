using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Dumps the trucks of the day onto the conveyor belt, item after item.
/// It waits while the sorting table is full.
/// </summary>
public class TruckDumper : MonoBehaviour
{
    // A dumped item lies at a random angle, anywhere in one full turn (degrees).
    private const float FullTurnDegrees = 360f;

    [SerializeField] private LandfillDaySettings settings;
    [SerializeField] private TrashItem itemPrefab;

    [Header("Listens to")]
    [SerializeField] private GameEvent tableFull;
    [SerializeField] private GameEvent tableHasRoom;

    private bool isWaiting;

    public int TruckCount => settings.trucks.Length;

    /// <summary>Happens when a truck starts dumping. Gives the truck's number, from 1.</summary>
    public event Action<int> TruckStarted;

    /// <summary>Happens once, after the last item of the last truck was dumped.</summary>
    public event Action AllTrucksDone;

    private void OnEnable()
    {
        tableFull.Subscribe(Wait);
        tableHasRoom.Subscribe(Continue);
    }

    private void OnDisable()
    {
        tableFull.Unsubscribe(Wait);
        tableHasRoom.Unsubscribe(Continue);
        StopAllCoroutines(); // A coroutine keeps running on a disabled component unless it is stopped.
    }

    private IEnumerator Start()
    {
        for (int truck = 0; truck < settings.trucks.Length; truck++)
        {
            yield return new WaitForSeconds(settings.secondsBeforeTruck);
            TruckStarted?.Invoke(truck + 1);

            foreach (TrashItemData data in settings.trucks[truck].ShuffledItems())
            {
                while (isWaiting)
                {
                    yield return null;
                }
                Dump(data);
                yield return new WaitForSeconds(settings.dumpInterval);
            }
        }
        AllTrucksDone?.Invoke();
    }

    private void Dump(TrashItemData data)
    {
        Quaternion tilt = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, FullTurnDegrees));
        TrashItem item = Instantiate(itemPrefab, transform.position, tilt);
        item.Setup(data);

        // An impulse is a single push. Multiplying by the mass gives every item the same starting speed.
        Rigidbody2D body = item.GetComponent<Rigidbody2D>();
        body.AddForce(settings.throwVelocity * body.mass, ForceMode2D.Impulse);
        body.AddTorque(UnityEngine.Random.Range(-settings.throwSpin, settings.throwSpin) * body.mass, ForceMode2D.Impulse);
    }

    private void Wait()
    {
        isWaiting = true;
    }

    private void Continue()
    {
        isWaiting = false;
    }
}
