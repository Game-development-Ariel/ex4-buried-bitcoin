using System;
using UnityEngine;

/// <summary>
/// A marked place on the belt where one machine can be installed.
/// </summary>
public class MachineSlot : MonoBehaviour, IClickable
{
    [Tooltip("The belt under the slot. Machines like the sieve need it")]
    [SerializeField] private Collider2D belt;

    [Tooltip("Shown only while the slot is empty")]
    [SerializeField] private GameObject emptySign;

    private GameObject machineObject;

    public Collider2D Belt => belt;
    public MachineData Machine { get; private set; }
    public bool IsEmpty => Machine == null;

    /// <summary>Happens when the player clicks the slot. The shop listens.</summary>
    public event Action<MachineSlot> Clicked;

    public void OnClicked()
    {
        Clicked?.Invoke(this);
    }

    public void Install(MachineData machine)
    {
        Machine = machine;
        machineObject = Instantiate(machine.prefab, transform);
        emptySign.SetActive(false);
    }

    public void Clear()
    {
        Destroy(machineObject);
        Machine = null;
        emptySign.SetActive(true);
    }
}
