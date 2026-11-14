using UnityEngine;

/// <summary>
/// The waste pile: every item that hits it is buried again and gone.
/// If the day's target is buried, it raises the "target lost" event.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WastePile : MonoBehaviour
{
    [SerializeField] private GameEvent targetLost;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TrashItem item = collision.collider.GetComponent<TrashItem>();
        if (item == null)
        {
            return;
        }

        if (item.Data.isTarget)
        {
            targetLost.Raise();
        }
        Destroy(item.gameObject);
    }
}
