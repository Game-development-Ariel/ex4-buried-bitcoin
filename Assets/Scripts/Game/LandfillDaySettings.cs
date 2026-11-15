using UnityEngine;

/// <summary>
/// The numbers of one work day at the sorting plant, saved as an asset so they can be tuned without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Landfill/Day Settings")]
public class LandfillDaySettings : ScriptableObject
{
    [Header("Rules")]
    public int startMoney = 100;
    public float timeLimitSeconds = 150f;

    [Tooltip("Seconds lost for saying \"this is the target\" about the wrong item")]
    public float wrongPickPenaltySeconds = 20f;

    [Tooltip("When this many items are on the sorting table, the belt and the truck stop")]
    public int tableCapacity = 14;

    [Header("Trucks")]
    public TruckLoad[] trucks;
    public float secondsBeforeTruck = 6f;

    [Tooltip("Seconds between two items falling from the truck")]
    public float dumpInterval = 0.6f;

    [Tooltip("The speed an item gets when it is dumped")]
    public Vector2 throwVelocity = new Vector2(1.5f, 0f);
    public float throwSpin = 0.3f;

    [Header("Target card")]
    [TextArea]
    public string targetDescription = "A red toolbox. Big, heavy, metal.";
}
