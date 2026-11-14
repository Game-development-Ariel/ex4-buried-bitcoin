using UnityEngine;

/// <summary>
/// One kind of item that comes out of the landfill: how it looks, how heavy it is,
/// and the properties the machines sort by. A new kind of item is a new asset of this type.
/// </summary>
[CreateAssetMenu(menuName = "Landfill/Trash Item")]
public class TrashItemData : ScriptableObject
{
    public string displayName = "Item";
    public Color color = Color.gray;

    [Tooltip("Width and height in world units")]
    public float size = 0.5f;
    public float mass = 1f;

    [Tooltip("Small items fall through the sieve")]
    public bool isSmall;

    [Tooltip("Dollars for selling it at the sorting table")]
    public int sellPrice;

    [Tooltip("The thing the city asked us to find today")]
    public bool isTarget;
}
