using UnityEngine;

/// <summary>
/// One kind of machine the player can buy. A new machine is a new asset of this type and a prefab.
/// </summary>
[CreateAssetMenu(menuName = "Landfill/Machine")]
public class MachineData : ScriptableObject
{
    public string displayName = "Machine";
    public int price = 100;

    [Tooltip("One short line for the shop: what the machine removes from the belt")]
    public string description;

    [Tooltip("Created inside the slot when the machine is installed")]
    public GameObject prefab;
}
