using UnityEngine;

/// <summary>
/// Place this on Benches, Shrines, or Lanterns.
/// When the player enters or interacts, it saves the permanent Checkpoint for death respawns.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Specific position the player appears at when respawning. If unassigned, checkpoint center is used.")]
    public Transform spawnPoint;

    [Tooltip("If true, automatically saves when the player walks into it. If false, requires pressing 'Submit' / interaction.")]
    public bool activateOnTouch = true;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activateOnTouch)
        {
            RegisterCheckpoint(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!activateOnTouch && Input.GetButtonDown("Submit"))
        {
            RegisterCheckpoint(other);
        }
    }

    private void RegisterCheckpoint(Collider2D other)
    {
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
        {
            Vector2 targetPos = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
            respawn.SetCheckpoint(targetPos);
        }
    }
}

