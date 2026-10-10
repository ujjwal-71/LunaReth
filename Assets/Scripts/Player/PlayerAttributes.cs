using UnityEngine;

public class _attributes : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Spirit (Combat Arts)")]
    public int maxSpirit = 20;
    public int currentSpirit;

    [Header("Components")]
    private MasterState masterState;
    private Animator anim;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentSpirit = maxSpirit;
        masterState = GetComponent<MasterState>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Returns TRUE if the attack was blocked/parried, FALSE if it hit successfully.
    /// </summary>
    public bool TakeDamage(int damageAmount, Vector2 knockbackForce, float stunDuration = 0.5f)
    {
        // 1. Invincibility Frames (I-Frames)
        // If we are already dead, or if we are dashing (Ghost layer/state), ignore the damage!
        if (masterState.CurrentState == MasterStateType.Dead || masterState.CurrentState == MasterStateType.Dashing)
        {
            return false;
        }

        // 2. Guarding Check
        // If the player is currently holding the guard button, block the attack!
        if (anim != null && anim.GetBool("isGAURDING"))
        {
            Debug.Log("Attack Parried!");
            // Apply a tiny pushback when blocking, but no damage or stun
            masterState.ApplyKnockback(knockbackForce.x * 0.2f, 0); 
            return true; // TELL THE ENEMY THEY WERE PARRIED
        }

        // 3. Take Damage
        currentHealth -= damageAmount;
        Debug.Log("Player Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 4. Trigger the Knockback via our newly upgraded MasterState!
            // This now handles both the physical push AND the stun time!
            masterState.ApplyKnockback(knockbackForce.x, knockbackForce.y, stunDuration);
        }

        return false;
    }

    private void Die()
    {
        currentHealth = 0;
        masterState.TriggerDeath(3f); // Play death anim and lock controls for 3 seconds
        Debug.Log("Player Died!");
    }

    public void UseSpirit(int amount)
    {
        currentSpirit -= amount;
        if (currentSpirit < 0) currentSpirit = 0;
    }
}
