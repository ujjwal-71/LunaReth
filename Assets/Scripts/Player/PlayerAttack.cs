using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attributes")]
    public int attackDamage = 10;
    public float hitPauseDuration = 0.05f; // Hollow Knight style micro-pause on hit!

    private void OnTriggerEnter2D(Collider2D enemy)
    {
        // Use the exact tags from your image!
        if (enemy.CompareTag("Enemies") || enemy.CompareTag("EnemiesWeapon"))
        {
            // Try to find the EnemyBase script on the object we hit
            EnemyBase enemyStats = enemy.GetComponent<EnemyBase>();
            
            if (enemyStats != null)
            {
                // Deal damage
                enemyStats.healthBar(attackDamage);
                
                // Trigger the juicy impact pause
                StartCoroutine(HitPause(hitPauseDuration));

                // Hit Recoil! Push the player backwards slightly
                ApplyHitRecoil(enemy.transform);
            }
        }
    }

    private void ApplyHitRecoil(Transform enemyTransform)
    {
        Rigidbody2D playerRb = GetComponentInParent<Rigidbody2D>();
        MasterState master = GetComponentInParent<MasterState>();

        if (playerRb != null && master != null)
        {
            // Figure out which direction to push the player (away from the enemy)
            float pushDirection = playerRb.transform.position.x < enemyTransform.position.x ? -1f : 1f;
            
            // Use our brand new reusable Knockback routine for Hit Recoil!
            master.ApplyKnockback(pushDirection * 15f, 0f, 0.1f);
        }
    }

    /// <summary>
    /// Temporarily slows down time when a hit connects to make attacks feel incredibly impactful.
    /// </summary>
    private IEnumerator HitPause(float duration)
    {
        Time.timeScale = 0.1f; // Slow time to a crawl
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f; // Return to normal
    }
}