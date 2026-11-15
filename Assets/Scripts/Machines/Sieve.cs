using UnityEngine;

/// <summary>
/// A sieve on the belt: small items that pass over it fall through the belt, into the waste below.
/// Needs a trigger collider over the belt. It is created inside a MachineSlot.
/// </summary>
public class Sieve : MonoBehaviour
{
    private Collider2D belt;

    // Awake and not Start: an item can touch the trigger in the same frame the sieve is installed, before Start.
    private void Awake()
    {
        belt = GetComponentInParent<MachineSlot>().Belt;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrashItem item = other.GetComponent<TrashItem>();
        if (item != null && item.Data.isSmall)
        {
            // From now on the belt doesn't hold this item, so gravity pulls it down through the holes.
            Physics2D.IgnoreCollision(other, belt);
        }
    }
}
