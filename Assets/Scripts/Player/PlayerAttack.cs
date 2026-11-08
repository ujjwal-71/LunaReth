using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles hits from the player's active attack collider.
/// Attach it to the same object as the attack hitbox.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Damage")]
    public int attackDamage = 10;
    public int postureDamage = 10;

    [Header("Impulse & Feel")]
    public float enemyKnockbackX = 5f;
    public float enemyKnockbackY = 2f;
    public float playerRecoilX = 2f;
    public float hitPauseDuration = 0.05f;

    private const float enemyHitstun = 0.15f;
    private const float playerRecoilY = 0f;

    private readonly Dictionary<EnemyBase, HashSet<Collider2D>> hitsByEnemy =
        new Dictionary<EnemyBase, HashSet<Collider2D>>();

    private MasterState playerState;

    private static int activeHitPauses;

    private void Awake()
    {
        playerState = GetComponentInParent<MasterState>();
    }

    private void OnTriggerEnter2D(Collider2D targetCollider)
    {
        TryHit(targetCollider);
    }

    private void OnTriggerStay2D(Collider2D targetCollider)
    {
        // Stay checks also catch targets that were already touching when a swing began.
        TryHit(targetCollider);
    }

    private void TryHit(Collider2D targetCollider)
    {
        EnemyBase enemy = targetCollider.GetComponentInParent<EnemyBase>();
        if (enemy == null)
            return;

        if (!hitsByEnemy.TryGetValue(enemy, out HashSet<Collider2D> hits))
        {
            hits = new HashSet<Collider2D>();
            hitsByEnemy.Add(enemy, hits);
        }

        // One swing should hit an enemy once, even if several of its colliders overlap.
        if (!hits.Add(targetCollider) || hits.Count > 1)
            return;

        ApplyHit(enemy);
    }

    private void OnTriggerExit2D(Collider2D targetCollider)
    {
        EnemyBase enemy = targetCollider.GetComponentInParent<EnemyBase>();
        if (enemy == null || !hitsByEnemy.TryGetValue(enemy, out HashSet<Collider2D> hits))
            return;

        hits.Remove(targetCollider);
        if (hits.Count == 0)
            hitsByEnemy.Remove(enemy);
    }

    private void OnDisable()
    {
        // Attack hitboxes are often switched off between swings.
        hitsByEnemy.Clear();
        ResetTimeScale();
    }

    public void ResetHitTargets()
    {
        hitsByEnemy.Clear();
    }

    private void ApplyHit(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        enemy.TakeDamage(attackDamage);
        enemy.TakePostureDamage(postureDamage);

        float enemyDirection = Mathf.Sign(enemy.transform.position.x - playerState.transform.position.x);
        if (enemyDirection == 0f)
            enemyDirection = 1f;

        enemy.ApplyKnockback(
            enemyDirection * enemyKnockbackX,
            enemyKnockbackY,
            enemyHitstun);

        playerState.ApplyRecoil(
            -enemyDirection * playerRecoilX,
            playerRecoilY);

        // Soft separation pushback so character and enemy sprites don't awkwardly overlap
        float distance = Mathf.Abs(enemy.transform.position.x - playerState.transform.position.x);
        if (distance < 0.8f)
        {
            float push = (0.8f - distance) * 0.5f;
            enemy.transform.position += new Vector3(enemyDirection * push, 0f, 0f);
        }

        StartCoroutine(HitPause(hitPauseDuration));
    }

    private void OnDestroy()
    {
        ResetTimeScale();
    }

    public static void ResetTimeScale()
    {
        activeHitPauses = 0;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Briefly slows the action on a confirmed hit.
    /// </summary>
    public IEnumerator HitPause(float duration)
    {
        activeHitPauses++;
        Time.timeScale = 0.1f;

        try
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
        }
        finally
        {
            activeHitPauses = Mathf.Max(0, activeHitPauses - 1);
            if (activeHitPauses == 0)
                Time.timeScale = 1f;
        }
    }
}
