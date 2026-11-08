using UnityEngine;

public class _attributes : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    [Tooltip("Brief invulnerability window after a direct hit to prevent multi-hit shredding.")]
    [Min(0f)] public float iFrameDuration = 0.65f;
    private int currentHealth;
    private float nextDamageTime;

    [Header("Spirit (Combat Arts & Ninjutsu)")]
    public int maxSpirit = 20;
    public int currentSpirit { get; private set; }

    [Header("Posture / Guard Meter (Sekiro)")]
    public int maxPosture = 100;
    [Tooltip("Posture points recovered per second when calm.")]
    public float postureRecoveryRate = 20f;
    [Tooltip("Duration player is stunned when posture shatters.")]
    public float postureStunDuration = 2f;

    private const float postureDelay = 1.5f;
    private int currentPosture;
    private float lastPostureTime;
    private float postureRemainder;

    [Header("Components")]
    private MasterState masterState;
    private ActionState actionState;
    private Animator anim;

    public enum DefenseOutcome
    {
        Hit,
        Blocked,
        Deflected,
        Ignored
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        currentSpirit = maxSpirit;
        currentPosture = 0;
        masterState = GetComponent<MasterState>();
        actionState = GetComponent<ActionState>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        RecoverPosture();
    }

    private void RecoverPosture()
    {
        if (currentPosture > 0 && Time.time >= lastPostureTime + postureDelay)
        {
            postureRemainder += postureRecoveryRate * Time.deltaTime;
            int recovered = Mathf.FloorToInt(postureRemainder);
            if (recovered <= 0)
                return;

            currentPosture = Mathf.Max(0, currentPosture - recovered);
            postureRemainder -= recovered;

            if (currentPosture == 0)
                postureRemainder = 0f;
        }
    }

    /// <summary>
    /// Applies guard, deflect, and damage rules, then reports the result to the attacker.
    /// </summary>
    public DefenseOutcome TakeDamage(int damageAmount, Vector2 knockbackForce, float stunDuration = 0.2f)
    {
        // Only Dead state and brief post-hit protection ignore damage. Dashing takes damage.
        if (masterState.CurrentState == MasterStateType.Dead || Time.time < nextDamageTime)
            return DefenseOutcome.Ignored;

        bool isGuarding = anim.GetBool("isGAURDING");
        if (isGuarding)
        {
            bool isPerfectDeflect = actionState.IsInParryWindow;

            if (isPerfectDeflect)
            {
                // A perfect deflect resets anti-spam penalty, uses a tiny recoil, and avoids health and posture damage.
                actionState.OnSuccessfulDeflect();
                masterState.ApplyRecoil(knockbackForce.x * 0.1f, 0f);
                return DefenseOutcome.Deflected;
            }
            else
            {
                // A regular block protects health but adds pressure to the posture meter.
                int postureDamage = damageAmount * 2;
                GainPosture(postureDamage, knockbackForce);
                return DefenseOutcome.Blocked;
            }
        }

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            nextDamageTime = Time.time + iFrameDuration;
            masterState.ApplyKnockback(knockbackForce.x, knockbackForce.y, stunDuration);
        }

        return DefenseOutcome.Hit;
    }

    public void GainPosture(int amount, Vector2 knockbackForce)
    {
        currentPosture += amount;
        lastPostureTime = Time.time;
        postureRemainder = 0f;

        if (currentPosture >= maxPosture)
        {
            BreakPosture();
        }
        else
        {
            masterState.ApplyKnockback(knockbackForce.x * 0.3f, 0, 0.1f);
        }
    }

    private void BreakPosture()
    {
        currentPosture = 0;
        postureRemainder = 0f;
        anim.SetBool("isGAURDING", false);
        masterState.TriggerStun(postureStunDuration);
    }

    public int CurrentHealth => currentHealth;

    private void Die()
    {
        currentHealth = 0;
        masterState.TriggerDeath(3f); // Play death anim and lock controls for 3 seconds

        // Notify PlayerRespawn to reload the scene cleanly after death animation
        PlayerRespawn respawn = GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.TriggerDeathRespawn(2.5f);
        }
    }

    public void UseSpirit(int amount)
    {
        currentSpirit -= amount;
        if (currentSpirit < 0) currentSpirit = 0;
    }
}
