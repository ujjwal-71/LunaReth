using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Attached to the Enemy's single AttackHitbox child GameObject.
/// In animations, simply keyframe the Collider2D on/off during swing frames.
/// Before each swing, EnemyBase updates this component with the active attack's stats.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("Active Attack Stats")]
    public int attackPower = 10;
    public float knockbackForceX = 8f;
    public float knockbackForceY = 3f;
    
    [Header("Parry Reaction")]
    [FormerlySerializedAs("stunPowerToSelfIfParried")]
    public int parryStunDamage = 20;

    /// <summary>
    /// Called by EnemyBase before executing an attack to configure this hitbox dynamically.
    /// </summary>
    public void SetAttackStats(int damage, float knockX, float knockY, int parryDamage)
    {
        attackPower = damage;
        knockbackForceX = knockX;
        knockbackForceY = knockY;
        parryStunDamage = parryDamage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore the player's weapon collider; it is not a damage target.
        if (other.CompareTag("PlayerWeapons"))
            return;

        // Find the player's attributes component up the hierarchy.
        _attributes player = other.GetComponentInParent<_attributes>();
        EnemyBase enemy = GetComponentInParent<EnemyBase>();

        if (player == null)
            return;

        // Push the player away from the enemy's body center
        float enemyX = enemy != null ? enemy.transform.position.x : transform.position.x;
        float pushDirection = Mathf.Sign(player.transform.position.x - enemyX);
        if (pushDirection == 0f)
            pushDirection = 1f;

        Vector2 knockback = new Vector2(pushDirection * knockbackForceX, knockbackForceY);

        // Player's attributes script decides whether this attack hits, is blocked, or is deflected.
        _attributes.DefenseOutcome outcome = player.TakeDamage(attackPower, knockback);

        if (outcome == _attributes.DefenseOutcome.Deflected)
        {
            if (enemy != null)
            {
                // A clean deflect damages the attacker's posture and knocks them back.
                enemy.TakePostureDamage(parryStunDamage);
                enemy.ApplyKnockback(-pushDirection * 8f, 3f, 0.4f);
            }
        }
    }
}
