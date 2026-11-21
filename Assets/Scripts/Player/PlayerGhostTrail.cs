using UnityEngine;

/// <summary>
/// Triggers after-image ghost silhouettes through the central VFXManager during high-speed actions (Dash).
/// Dual-color alternation matches the Samurai's Crimson Armor and Silver Mask.
/// </summary>
public class PlayerGhostTrail : MonoBehaviour
{
    [Header("Ghost Trail Settings")]
    [Tooltip("Time in seconds between each ghost after-image spawn.")]
    [Range(0.01f, 0.1f)] public float spawnInterval = 0.035f;
    [Tooltip("How long each ghost stays visible before fully fading.")]
    [Range(0.1f, 0.5f)] public float fadeDuration = 0.22f;

    [Header("Color Palette (Matches Samurai Character Design)")]
    [Tooltip("Primary ghost tint (Crimson red matching the samurai haori).")]
    public Color primaryColor = new Color(0.85f, 0.18f, 0.18f, 0.65f);
    [Tooltip("Secondary ghost tint (Silver-white matching hair and blade).")]
    public Color secondaryColor = new Color(0.95f, 0.95f, 0.98f, 0.55f);
    [Tooltip("Alternate between crimson and silver ghost shadows during dash.")]
    public bool alternateColors = true;

    private SpriteRenderer playerSprite;
    private MasterState masterState;
    private float nextSpawnTime;
    private bool isEmitting;
    private int spawnCount;

    private void Awake()
    {
        playerSprite = GetComponent<SpriteRenderer>();
        masterState = GetComponent<MasterState>();
    }

    private void OnEnable()
    {
        if (masterState != null)
        {
            masterState.OnDashStarted += StartTrail;
            masterState.OnDashEnded += StopTrail;
        }
    }

    private void OnDisable()
    {
        if (masterState != null)
        {
            masterState.OnDashStarted -= StartTrail;
            masterState.OnDashEnded -= StopTrail;
        }
        StopTrail();
    }

    private void Update()
    {
        if (isEmitting && Time.time >= nextSpawnTime)
        {
            SpawnGhost();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void StartTrail()
    {
        isEmitting = true;
        nextSpawnTime = Time.time;
        spawnCount = 0;
    }

    public void StopTrail()
    {
        isEmitting = false;
    }

    private void SpawnGhost()
    {
        if (playerSprite == null || playerSprite.sprite == null || VFXManager.Instance == null) return;

        Color chosenColor = primaryColor;
        if (alternateColors && (spawnCount % 2 == 1))
        {
            chosenColor = secondaryColor;
        }
        spawnCount++;

        VFXManager.Instance.SpawnGhost(playerSprite.sprite, transform.position, transform.localScale, chosenColor, fadeDuration);
    }
}
