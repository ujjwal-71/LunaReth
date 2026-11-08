using UnityEngine;

/// <summary>
/// Place this trigger on hazardous objects like vines, spikes, or death pits.
/// When the player touches it, they take hazard damage and respawn at the last safe place.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HazardZone : MonoBehaviour
{
    private void Reset()
    {
        // Automatically ensure the collider is a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHazard(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleHazard(other);
    }

    private void HandleHazard(Collider2D other)
    {
        // Look for PlayerRespawn on the collided player
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.TriggerHazardRespawn();
        }
    }
}

