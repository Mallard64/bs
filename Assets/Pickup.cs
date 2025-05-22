// Pickup.cs
using UnityEngine;
using Mirror;

/// <summary>
/// Detects client-side trigger with the player and asks the server to grant the pickup.
/// </summary>
public class Pickup : NetworkBehaviour
{
    [Tooltip("Index into MouseShooting.weapons array.")]
    public int weaponIndex;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only the local player should request a pickup
        if (!other.CompareTag("Player")) return;
        
        var shooter = other.GetComponent<MouseShooting>();
        if (shooter != null && shooter.isLocalPlayer && shooter.wantsPickup)
        {
            other.gameObject.GetComponent<MouseShooting>().wantsPickup = false;
            //Tell server "I touched this pickup"
            shooter.CmdRequestPickup(weaponIndex, netId);
        }
        else if (shooter != null && shooter.isLocalPlayer)
        {
            shooter.ActivatePickupButton();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Only the local player should request a pickup
        if (!other.CompareTag("Player")) return;
        var shooter = other.GetComponent<MouseShooting>();
        if (shooter != null && shooter.isLocalPlayer && shooter.wantsPickup)
        {
            other.gameObject.GetComponent<MouseShooting>().wantsPickup = false;
            //Tell server "I touched this pickup"
            shooter.CmdRequestPickup(weaponIndex, netId);
        }
        else if (shooter != null && shooter.isLocalPlayer)
        {
            shooter.ActivatePickupButton();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var shooter = other.GetComponent<MouseShooting>();
        if (shooter != null && shooter.isLocalPlayer)
        {
            shooter.DeActivatePickupButton();
        }
    }
}


