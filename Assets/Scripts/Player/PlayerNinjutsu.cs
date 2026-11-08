using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the Flying Raijin (Minato Teleportation) Ninjutsu technique.
/// Throws a stickable beacon kunai into the world. Pressing again instantly
/// teleports the player to the kunai and unleashes an AoE shockwave.
///
/// Rule: Kunai can persist in the world independently, but teleportation is
/// strictly forbidden unless MasterState is Free.
/// </summary>
[RequireComponent(typeof(MasterState))]
[RequireComponent(typeof(_attributes))]
[RequireComponent(typeof(Animator))]
public class PlayerNinjutsu : MonoBehaviour
{
    [Header("Kunai Beacon")]
    public GameObject kunaiPrefab;
    [Tooltip("Optional throw point on hand. If unassigned, upper body position is used.")]
    public Transform kunaiThrowPoint;
    public LayerMask kunaiBlockingLayers;
    public int kunaiSpiritCost = 2;
    public float kunaiCooldown = 1.0f;

    [Header("Teleport Shockwave (AoE)")]
    public float aoeKnockbackRadius = 3f;
    public int aoeDamage = 10;
    public int aoeStunDamage = 10;

    private const float kunaiWallClearance = 0.08f;
    private const float kunaiThrowArc = 2f;
    private const float aoeKnockbackX = 8f;
    private const float aoeKnockbackY = 3f;
    private const float teleportRecoil = 8f;

    private MasterState masterState;
    private _attributes attributes;
    private Animator anim;

    private GameObject activeKunai;
    private float lastKunaiTime = -100f;

    public bool HasActiveKunai => activeKunai != null;

    private void Awake()
    {
        masterState = GetComponent<MasterState>();
        attributes = GetComponent<_attributes>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // Clean up reference if kunai despawned naturally after timer
        if (activeKunai == null && !ReferenceEquals(activeKunai, null))
        {
            activeKunai = null;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            HandleKunaiInput();
        }
    }

    private void HandleKunaiInput()
    {
        if (!HasActiveKunai)
        {
            ThrowKunai();
        }
        else
        {
            TeleportToKunai();
        }
    }

    private void ThrowKunai()
    {
        if (Time.time < lastKunaiTime + kunaiCooldown)
            return;

        if (attributes.currentSpirit < kunaiSpiritCost)
            return;

        if (kunaiPrefab == null)
            return;

        attributes.UseSpirit(kunaiSpiritCost);
        lastKunaiTime = Time.time;

        float direction = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 origin = kunaiThrowPoint != null
            ? (Vector2)kunaiThrowPoint.position
            : (Vector2)transform.position + new Vector2(0f, 0.8f);

        Vector2 forward = new Vector2(direction, 0f);
        Vector2 spawnPos = origin + forward * 0.15f;

        // Check if player is right next to a wall to avoid spawning kunai inside geometry
        if (kunaiBlockingLayers.value != 0 && kunaiWallClearance > 0f)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(origin, forward, kunaiWallClearance, kunaiBlockingLayers);
            if (wallHit.collider != null)
                spawnPos = wallHit.point - forward * 0.03f;
        }

        activeKunai = Instantiate(kunaiPrefab, spawnPos, Quaternion.identity);
        activeKunai.transform.localScale = new Vector3(direction, 1, 1);

        KunaiProjectile projectile = activeKunai.GetComponent<KunaiProjectile>();
        if (projectile != null)
        {
            projectile.Launch(forward, kunaiThrowArc);
        }
    }

    private void TeleportToKunai()
    {
        // User Mandate: Kunai beacon stays in the world, but teleportation is strictly forbidden unless MasterState is Free!
        if (masterState.CurrentState != MasterStateType.Free)
            return;

        Vector3 targetPos = activeKunai.transform.position;
        Destroy(activeKunai);
        activeKunai = null;

        transform.position = targetPos;
        anim.SetTrigger("specialAttack");

        // Unleash radial shockwave
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, aoeKnockbackRadius);
        HashSet<EnemyBase> affectedEnemies = new HashSet<EnemyBase>();

        foreach (Collider2D hit in hitColliders)
        {
            EnemyBase target = hit.GetComponentInParent<EnemyBase>();
            if (target == null || !affectedEnemies.Add(target))
                continue;

            target.TakeDamage(aoeDamage);
            target.TakePostureDamage(aoeStunDamage);

            float enemyDir = Mathf.Sign(target.transform.position.x - transform.position.x);
            if (enemyDir == 0f)
                enemyDir = transform.localScale.x > 0f ? 1f : -1f;

            target.ApplyKnockback(enemyDir * aoeKnockbackX, aoeKnockbackY, 0.2f);
        }

        // Apply light tactile recoil push to player
        float pushDir = transform.localScale.x > 0 ? -1f : 1f;
        masterState.ApplyRecoil(pushDir * teleportRecoil, 4f);
    }
}

