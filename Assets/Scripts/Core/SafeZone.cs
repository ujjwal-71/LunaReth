using UnityEngine;

/// <summary>
/// Place this trigger on stable platforms and safe ledges before dangerous platforming sections.
/// When the player passes through, it updates PlayerRespawn.safePlace.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SafeZone : MonoBehaviour
{
    [Tooltip("Optional spawn point offset. If unassigned, the center of this trigger is used.")]
    public Transform respawnPoint;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
        {
            Vector2 targetPoint = respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position;
            respawn.SetSafePlace(targetPoint);
        }
    }
}

