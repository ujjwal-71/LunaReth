using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Stats")]
    public int attackPower = 10;
    public float knockbackForceX = 15f;
    public float knockbackForceY = 5f;
    
    [Header("Parry Reaction")]
    public int stunPowerToSelfIfParried = 20;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ensure we only hit the Player
        if (!other.CompareTag("Player")) return;

        _attributes playerStats = other.GetComponent<_attributes>();
        EnemyBase enemyStats = GetComponentInParent<EnemyBase>();

        if (playerStats == null) return;

        // Calculate knockback direction (push player away from the enemy)
        float pushDirection = Mathf.Sign(playerStats.transform.position.x - transform.position.x);
        Vector2 knockback = new Vector2(pushDirection * knockbackForceX, knockbackForceY);

        // Attempt to damage the player. If they are Guarding, TakeDamage returns TRUE.
        bool wasParried = playerStats.TakeDamage(attackPower, knockback);

        if (wasParried)
        {
            Debug.Log("Enemy got Parried! Stun the Enemy!");
            if (enemyStats != null)
            {
                enemyStats.stunBar(stunPowerToSelfIfParried);
            }
        }
    }
}
