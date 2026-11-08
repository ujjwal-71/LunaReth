using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages player respawns for both environmental hazards (vines/spikes -> safe place)
/// and full player death (HP = 0 -> checkpoint reload).
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [Header("Checkpoint (Death Respawn)")]
    private static Vector2 currentCheckpoint;
    private static string checkpointScene;
    private static bool hasCheckpoint = false;

    [Header("Hazard Settings")]
    [Tooltip("Health deducted when falling into hazards like vines or spikes.")]
    public int hazardDamage = 15;

    [HideInInspector] public Vector2 safePlace;

    [Header("Components")]
    private Rigidbody2D rb;
    private MasterState masterState;
    private _attributes attributes;

    private bool isRespawningHazard = false;
    private Coroutine deathRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        masterState = GetComponent<MasterState>();
        attributes = GetComponent<_attributes>();

        string currentScene = SceneManager.GetActiveScene().name;

        // If a checkpoint was saved for this scene, spawn the player there
        if (hasCheckpoint && checkpointScene == currentScene)
        {
            transform.position = currentCheckpoint;
        }
        else
        {
            // Default initial checkpoint is the starting position in this scene
            SetCheckpoint(transform.position);
        }

        // Initialize safe place to the starting position
        safePlace = transform.position;
    }

    /// <summary>
    /// Updates the main checkpoint (bench/lantern/shrine).
    /// </summary>
    public void SetCheckpoint(Vector2 point)
    {
        currentCheckpoint = point;
        checkpointScene = SceneManager.GetActiveScene().name;
        hasCheckpoint = true;
        safePlace = point;
    }

    /// <summary>
    /// Updates the local safe ledge (recorded when stepping inside SafeZone triggers).
    /// </summary>
    public void SetSafePlace(Vector2 point)
    {
        safePlace = point;
    }

    /// <summary>
    /// Called when the player falls into hazards (spikes, vines, death pits).
    /// Deals hazard damage and respawns the player on the last safe ledge.
    /// </summary>
    public void TriggerHazardRespawn()
    {
        if (isRespawningHazard)
            return;

        if (masterState.CurrentState == MasterStateType.Dead)
            return;

        StartCoroutine(HazardRespawnRoutine());
    }

    private IEnumerator HazardRespawnRoutine()
    {
        isRespawningHazard = true;

        // 1. Lock controls and cancel physics momentum
        masterState.SetState(MasterStateType.KnockedBack);
        rb.linearVelocity = Vector2.zero;

        // 2. Deal hazard damage (bypasses regular attack block)
        attributes.TakeDamage(hazardDamage, Vector2.zero, 0.2f);
        
        // If hazard damage was fatal, exit and let full death routine handle scene reload
        if (attributes.CurrentHealth <= 0)
        {
            isRespawningHazard = false;
            yield break;
        }

        // Brief blackout/freeze duration
        yield return new WaitForSeconds(0.2f);

        // 3. Move player to safe place
        transform.position = safePlace;
        rb.linearVelocity = Vector2.zero;

        // 4. Restore control
        masterState.SetState(MasterStateType.Free);

        isRespawningHazard = false;
    }

    /// <summary>
    /// Called when the player dies (HP = 0). Reloads the scene to reset the world cleanly.
    /// </summary>
    public void TriggerDeathRespawn(float delay = 2.0f)
    {
        if (deathRoutine != null)
            return;

        deathRoutine = StartCoroutine(DeathReloadRoutine(delay));
    }

    private IEnumerator DeathReloadRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Reload active scene cleanly (resets all room objects and enemies)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

