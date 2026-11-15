using UnityEngine;

/// <summary>
/// The numbers of one work day at the sorting plant, saved as an asset so they can be tuned without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Landfill/Day Settings")]
public class LandfillDaySettings : ScriptableObject
{
    [Header("Rules")]
    [Tooltip("Dollars the player has when the day starts")]
    public int startMoney = 100;

    [Tooltip("Seconds the player has to find the target")]
    public float timeLimitSeconds = 150f;

    [Tooltip("The clock turns red when this many seconds are left")]
    public float warningSeconds = 20f;

    [Tooltip("Seconds lost for saying \"this is the target\" about the wrong item")]
    public float wrongPickPenaltySeconds = 20f;

    [Tooltip("When this many items are on the sorting table, the belt and the truck stop")]
    public int tableCapacity = 14;

    [Tooltip("After the table was full, the belt and the truck start again when this many places are free")]
    public int freePlacesToRestart = 2;

    [Header("Line")]
    [Tooltip("How fast the belt moves, in world units per second")]
    public float beltSpeed = 2.5f;

    // The blower has no script, so nothing reads this while playing. The scene builder writes it into the prefab.
    [Tooltip("How hard the blower pushes up. Rebuild the scene after changing it")]
    public float blowerForce = 6f;

    [Header("Trucks")]
    public TruckLoad[] trucks;

    [Tooltip("Seconds of waiting before each truck starts dumping, the first one too")]
    public float secondsBeforeTruck = 6f;

    [Tooltip("Seconds between two items falling from the truck")]
    public float dumpInterval = 0.6f;

    [Tooltip("The speed an item gets when it is dumped")]
    public Vector2 throwVelocity = new Vector2(1.5f, 0f);

    [Tooltip("The biggest random spin push a dumped item gets (a torque impulse per unit of mass). Smaller items spin faster")]
    public float throwSpin = 0.3f;

    [Header("Target")]
    [Tooltip("The item the city asked us to find today. Its description is shown on the target card")]
    public TrashItemData target;
}
