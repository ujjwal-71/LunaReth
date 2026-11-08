using UnityEngine;

/// <summary>
/// Deals a brief body-contact hit when an enemy touches the player.
/// Keep this on body colliders, separate from the enemy's weapon hitbox.
/// </summary>
public class EnemyContactDamage : MonoBehaviour
{
    [Header("Damage Stats")]
    [Min(0)] public int contactDamage = 10;
    [Min(0f)] public float knockbackX = 7f;
    public float knockbackY = 5f;
    [Min(0f)] public float stunDuration = 0.25f;

    [Tooltip("Minimum time before this enemy can hurt the player again by touch.")]
    [Min(0f)] public float contactCooldown = 1.0f;
    private float nextContactTime;

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckContact(other);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckContact(collision.collider);
        CheckContact(collision.otherCollider);
    }

    private void CheckContact(Collider2D other)
    {
        if (other == null || other.CompareTag("PlayerWeapons") || Time.time < nextContactTime)
            return;

        // Player colliders are often on child objects, so find the player component up the hierarchy.
        _attributes player = other.GetComponentInParent<_attributes>();
        if (player == null)
            return;

        float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
        if (direction == 0f)
            direction = 1f;

        Vector2 knockback = new Vector2(direction * knockbackX, knockbackY);
        _attributes.DefenseOutcome outcome = player.TakeDamage(contactDamage, knockback, stunDuration);

        // Don't start a cooldown for hits ignored by player invulnerability.
        if (outcome != _attributes.DefenseOutcome.Ignored)
            nextContactTime = Time.time + contactCooldown;
    }
}

