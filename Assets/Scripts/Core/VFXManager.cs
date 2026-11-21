using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized, high-performance Visual Effects Manager for both Player and Enemies.
/// Enforces strict URP 2D sorting layer rules and uses pre-warmed object pools for zero GC stutter.
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("URP Materials")]
    [Tooltip("Unlit URP 2D material for clean rendering without lighting/magenta artifacts.")]
    public Material unlitVFXMaterial;

    [Header("Sorting Layer Rules")]
    [Tooltip("Sorting layer for combat sparks, blade glints, and hit impacts.")]
    public string combatSortingLayer = "AboveMain";
    public int combatSortingOrder = 5;

    [Tooltip("Sorting layer for footstep, jump, and landing dust (behind legs).")]
    public string dustSortingLayer = "BelowPlayers";
    public int dustSortingOrder = 2;

    [Tooltip("Sorting layer for after-image ghost silhouettes.")]
    public string ghostSortingLayer = "MainLayers";
    public int ghostSortingOrder = -1;

    [Header("VFX Sprites (From Assets/Sprites/Particles)")]
    [Tooltip("Dust puff sprite (e.g. 1/9.png).")]
    public Sprite dustPuffSprite;
    [Tooltip("Spark / impact burst sprite (e.g. 1/14.png or 1/28.png).")]
    public Sprite sparkSprite;
    [Tooltip("Slash line streak sprite (e.g. 1/25.png).")]
    public Sprite slashStreakSprite;

    [Header("Pool Sizes")]
    [Range(5, 30)] public int sparkPoolSize = 15;
    [Range(5, 30)] public int dustPoolSize = 15;
    [Range(5, 30)] public int ghostPoolSize = 12;

    // Internal Object Pools
    private List<PooledVFXItem> sparkPool;
    private List<PooledVFXItem> dustPool;
    private List<PooledGhostItem> ghostPool;

    private GameObject poolRoot;

    private class PooledVFXItem
    {
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public float spawnTime;
        public float lifetime;
        public Vector3 startScale;
        public Vector3 endScale;
        public Color startColor;
        public Color endColor;
        public Vector2 velocity;
        public bool active;
    }

    private class PooledGhostItem
    {
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public float spawnTime;
        public float lifetime;
        public Color startColor;
        public bool active;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePools();
    }

    private void InitializePools()
    {
        poolRoot = new GameObject("VFX_Pool_Container");
        poolRoot.transform.SetParent(transform);

        // Fallback material if not assigned in inspector
        if (unlitVFXMaterial == null)
        {
            unlitVFXMaterial = Resources.Load<Material>("Assets/Materials/VFX_Unlit.mat");
            if (unlitVFXMaterial == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (unlitShader != null) unlitVFXMaterial = new Material(unlitShader);
            }
        }

        // 1. Pre-warm Spark Pool (AboveMain)
        sparkPool = new List<PooledVFXItem>(sparkPoolSize);
        for (int i = 0; i < sparkPoolSize; i++)
        {
            GameObject obj = new GameObject("Pooled_Spark_" + i);
            obj.transform.SetParent(poolRoot.transform);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = unlitVFXMaterial;
            sr.sortingLayerName = combatSortingLayer;
            sr.sortingOrder = combatSortingOrder;
            obj.SetActive(false);

            sparkPool.Add(new PooledVFXItem { gameObject = obj, spriteRenderer = sr, active = false });
        }

        // 2. Pre-warm Dust Pool (BelowPlayers)
        dustPool = new List<PooledVFXItem>(dustPoolSize);
        for (int i = 0; i < dustPoolSize; i++)
        {
            GameObject obj = new GameObject("Pooled_Dust_" + i);
            obj.transform.SetParent(poolRoot.transform);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = unlitVFXMaterial;
            sr.sortingLayerName = dustSortingLayer;
            sr.sortingOrder = dustSortingOrder;
            obj.SetActive(false);

            dustPool.Add(new PooledVFXItem { gameObject = obj, spriteRenderer = sr, active = false });
        }

        // 3. Pre-warm Ghost Pool (MainLayers -1)
        ghostPool = new List<PooledGhostItem>(ghostPoolSize);
        for (int i = 0; i < ghostPoolSize; i++)
        {
            GameObject obj = new GameObject("Pooled_Ghost_" + i);
            obj.transform.SetParent(poolRoot.transform);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = unlitVFXMaterial;
            sr.sortingLayerName = ghostSortingLayer;
            sr.sortingOrder = ghostSortingOrder;
            obj.SetActive(false);

            ghostPool.Add(new PooledGhostItem { gameObject = obj, spriteRenderer = sr, active = false });
        }
    }

    private void Update()
    {
        float currentTime = Time.time;

        // Update active sparks
        for (int i = 0; i < sparkPool.Count; i++)
        {
            PooledVFXItem item = sparkPool[i];
            if (!item.active) continue;

            float elapsed = currentTime - item.spawnTime;
            if (elapsed >= item.lifetime)
            {
                item.active = false;
                item.gameObject.SetActive(false);
            }
            else
            {
                float t = elapsed / item.lifetime;
                item.gameObject.transform.position += (Vector3)(item.velocity * Time.deltaTime);
                item.gameObject.transform.localScale = Vector3.Lerp(item.startScale, item.endScale, t);
                item.spriteRenderer.color = Color.Lerp(item.startColor, item.endColor, t);
            }
        }

        // Update active dust puffs
        for (int i = 0; i < dustPool.Count; i++)
        {
            PooledVFXItem item = dustPool[i];
            if (!item.active) continue;

            float elapsed = currentTime - item.spawnTime;
            if (elapsed >= item.lifetime)
            {
                item.active = false;
                item.gameObject.SetActive(false);
            }
            else
            {
                float t = elapsed / item.lifetime;
                item.gameObject.transform.position += (Vector3)(item.velocity * Time.deltaTime);
                item.gameObject.transform.localScale = Vector3.Lerp(item.startScale, item.endScale, t);
                item.spriteRenderer.color = Color.Lerp(item.startColor, item.endColor, t);
            }
        }

        // Update active ghost silhouettes
        for (int i = 0; i < ghostPool.Count; i++)
        {
            PooledGhostItem ghost = ghostPool[i];
            if (!ghost.active) continue;

            float elapsed = currentTime - ghost.spawnTime;
            if (elapsed >= ghost.lifetime)
            {
                ghost.active = false;
                ghost.gameObject.SetActive(false);
            }
            else
            {
                float t = elapsed / ghost.lifetime;
                Color c = ghost.startColor;
                c.a = Mathf.Lerp(ghost.startColor.a, 0f, t);
                ghost.spriteRenderer.color = c;
            }
        }
    }

    // ==========================================
    // PUBLIC API (Callable by Player and Enemies)
    // ==========================================

    /// <summary>
    /// Spawns Sekiro deflect sparks at the contact position.
    /// </summary>
    public void SpawnDeflectSparks(Vector3 worldPos, int facingDirection = 1)
    {
        // 1. Central golden-white flash
        PooledVFXItem flash = GetAvailableItem(sparkPool);
        if (flash != null)
        {
            flash.gameObject.transform.position = worldPos;
            flash.gameObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            flash.spriteRenderer.sprite = sparkSprite != null ? sparkSprite : dustPuffSprite;
            flash.startScale = new Vector3(0.4f, 0.4f, 1f);
            flash.endScale = new Vector3(1.2f, 1.2f, 1f);
            flash.startColor = new Color(1f, 0.95f, 0.6f, 1f); // Bright gold
            flash.endColor = new Color(1f, 0.4f, 0.1f, 0f);    // Fiery orange fade
            flash.velocity = Vector2.zero;
            flash.lifetime = 0.12f;
            flash.spawnTime = Time.time;
            flash.active = true;
            flash.gameObject.SetActive(true);
        }

        // 2. High-speed spark lines shooting outward
        for (int i = 0; i < 3; i++)
        {
            PooledVFXItem spark = GetAvailableItem(sparkPool);
            if (spark == null) break;

            float angle = Random.Range(-40f, 40f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle) * facingDirection, Mathf.Sin(angle)).normalized;

            spark.gameObject.transform.position = worldPos + (Vector3)(dir * 0.15f);
            spark.gameObject.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            spark.spriteRenderer.sprite = slashStreakSprite != null ? slashStreakSprite : sparkSprite;
            spark.startScale = new Vector3(0.8f, 0.2f, 1f);
            spark.endScale = new Vector3(1.5f, 0.05f, 1f);
            spark.startColor = new Color(1f, 1f, 1f, 1f);
            spark.endColor = new Color(1f, 0.6f, 0.1f, 0f);
            spark.velocity = dir * Random.Range(6f, 12f);
            spark.lifetime = 0.10f;
            spark.spawnTime = Time.time;
            spark.active = true;
            spark.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Spawns a hit impact spark / blood burst when a sword lands on an enemy.
    /// </summary>
    public void SpawnHitImpact(Vector3 worldPos, bool isCritical = false)
    {
        PooledVFXItem hit = GetAvailableItem(sparkPool);
        if (hit == null) return;

        hit.gameObject.transform.position = worldPos;
        hit.gameObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        hit.spriteRenderer.sprite = sparkSprite != null ? sparkSprite : dustPuffSprite;
        hit.startScale = new Vector3(0.5f, 0.5f, 1f);
        hit.endScale = new Vector3(1.0f, 1.0f, 1f);
        hit.startColor = isCritical ? new Color(1f, 0.15f, 0.15f, 1f) : new Color(1f, 0.85f, 0.4f, 1f);
        hit.endColor = new Color(0.6f, 0f, 0f, 0f);
        hit.velocity = Vector2.up * 1.5f;
        hit.lifetime = 0.15f;
        hit.spawnTime = Time.time;
        hit.active = true;
        hit.gameObject.SetActive(true);
    }

    /// <summary>
    /// Spawns a ground dust puff upon jumping, walking, or landing.
    /// </summary>
    public void SpawnDustPuff(Vector3 worldPos, Vector2 pushVelocity, float scale = 0.8f, float duration = 0.25f)
    {
        PooledVFXItem dust = GetAvailableItem(dustPool);
        if (dust == null) return;

        dust.gameObject.transform.position = worldPos;
        dust.gameObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
        dust.spriteRenderer.sprite = dustPuffSprite;
        dust.startScale = new Vector3(scale * 0.4f, scale * 0.4f, 1f);
        dust.endScale = new Vector3(scale * 1.1f, scale * 1.1f, 1f);
        dust.startColor = new Color(0.85f, 0.85f, 0.82f, 0.7f); // Soft warm earth
        dust.endColor = new Color(0.85f, 0.85f, 0.82f, 0f);
        dust.velocity = pushVelocity;
        dust.lifetime = duration;
        dust.spawnTime = Time.time;
        dust.active = true;
        dust.gameObject.SetActive(true);
    }

    /// <summary>
    /// Spawns a silhouette ghost after-image for dash or dodge moves.
    /// </summary>
    public void SpawnGhost(Sprite sprite, Vector3 pos, Vector3 scale, Color color, float duration = 0.22f)
    {
        if (sprite == null) return;

        PooledGhostItem ghost = GetAvailableGhost();
        if (ghost == null) return;

        ghost.gameObject.transform.position = pos;
        ghost.gameObject.transform.localScale = scale;
        ghost.spriteRenderer.sprite = sprite;
        ghost.startColor = color;
        ghost.spriteRenderer.color = color;
        ghost.lifetime = duration;
        ghost.spawnTime = Time.time;
        ghost.active = true;
        ghost.gameObject.SetActive(true);
    }

    private PooledVFXItem GetAvailableItem(List<PooledVFXItem> pool)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].active) return pool[i];
        }
        // Fallback: recycle oldest
        PooledVFXItem oldest = pool[0];
        for (int i = 1; i < pool.Count; i++)
        {
            if (pool[i].spawnTime < oldest.spawnTime) oldest = pool[i];
        }
        return oldest;
    }

    private PooledGhostItem GetAvailableGhost()
    {
        for (int i = 0; i < ghostPool.Count; i++)
        {
            if (!ghostPool[i].active) return ghostPool[i];
        }
        PooledGhostItem oldest = ghostPool[0];
        for (int i = 1; i < ghostPool.Count; i++)
        {
            if (ghostPool[i].spawnTime < oldest.spawnTime) oldest = ghostPool[i];
        }
        return oldest;
    }
}
