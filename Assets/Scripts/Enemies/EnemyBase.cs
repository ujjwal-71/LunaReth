using System.Collections.Generic;
using UnityEngine;

public enum EnemyArchetype
{
    Custom,
    GroundPatrol,
    HeavyJumper,
    Flying
}

[System.Serializable]
public class EnemyAttackData
{
    [Tooltip("Trigger name in the Animator Controller (e.g. 'Attack1', 'JumpSlam', 'Thrust').")]
    public string animationTrigger = "Attack1";
    [Tooltip("Maximum distance from target to select this attack.")]
    public float attackRange = 2.0f;
    [Tooltip("Health damage dealt on hit.")]
    public int damage = 15;
    [Tooltip("Horizontal knockback force applied to player on hit.")]
    public float knockbackX = 6.0f;
    [Tooltip("Vertical lift force applied to player on hit.")]
    public float knockbackY = 2.0f;
    [Tooltip("Posture damage taken by THIS enemy if the player perfect deflects.")]
    public int parrySelfDamage = 20;
    [Tooltip("Cooldown delay before this specific attack can be used again.")]
    public float cooldown = 2.0f;
    [Tooltip("Boss phase required to unlock this attack (1 = default, 2 = phase 2).")]
    public int minPhaseRequired = 1;

    [HideInInspector] public float lastUsedTime = -100f;
}

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Archetype & Preset")]
    [Tooltip("Select an archetype to auto-apply recommended defaults. Values remain 100% public and tunable.")]
    public EnemyArchetype preset = EnemyArchetype.GroundPatrol;

    [Header("Vitals & Posture (Sekiro)")]
    public int maxHealth = 100;
    public int maxStun = 100;
    [Tooltip("Posture recovered per second when calm.")]
    [Min(0f)] public float postureRate = 12f;
    [Tooltip("Calm delay (seconds) before posture begins recovering.")]
    [Min(0f)] public float postureDelay = 1.5f;

    [Header("Locomotion (All Public for Tuning)")]
    public float walkSpeed = 3.0f;
    public float chaseSpeed = 5.5f;
    public float detectionRange = 8.0f;
    public float attackRange = 2.0f;

    [Header("Heavy Jumper Settings (If Applicable)")]
    public Vector2 jumpForce = new Vector2(6.0f, 12.0f);
    public float jumpCooldown = 3.0f;

    [Header("Flying Settings (If Applicable)")]
    public float hoverHeight = 3.5f;
    public float swoopSpeed = 7.0f;

    [Header("Raycast Anchors & Layers")]
    [Tooltip("Point at feet/front for checking platform ledges so ground enemies don't walk off cliffs.")]
    public Transform groundRay;
    [Tooltip("Point at eye/chest level for forward wall checks and line-of-sight.")]
    public Transform frontRay;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask playerLayer;

    [Header("Multiplayer / Lock-On Target System")]
    [Tooltip("The specific player currently locked-on.")]
    public Transform currentTarget;
    [Tooltip("How long (in seconds) the enemy remembers and pursues the locked-on player after losing line-of-sight.")]
    public float targetMemoryDuration = 5.0f;
    private float targetLostTimer = 0f;

    [Header("Boss Phases & Transitions")]
    [Tooltip("Total phases. Set to 1 for standard enemies, 2 or 3 for multi-phase bosses.")]
    public int totalPhases = 1;
    public int currentPhase = 1;
    [Range(0.1f, 0.9f)] public float phase2HpThreshold = 0.5f;

    [Header("Self-Healing (Optional)")]
    public bool canHeal = false;
    public int healAmount = 25;
    public float healCooldown = 15.0f;
    [Range(0.1f, 0.9f)] public float healHpThreshold = 0.35f;
    private float lastHealTime = -100f;

    [Header("Attacks List (Single Hitbox Workflow)")]
    [Tooltip("Single child hitbox GameObject with EnemyAttack.cs attached. Auto-found if unassigned.")]
    public EnemyAttack activeHitbox;
    public EnemyAttackData[] attacks = new EnemyAttackData[]
    {
        new EnemyAttackData() { animationTrigger = "Attack1", attackRange = 2.0f, damage = 15, cooldown = 1.5f }
    };

    public enum State
    {
        idle,
        patrol,
        chase,
        attack,
        stun,
        executionVulnerable,
        phaseTransition,
        healing,
        dead
    }

    [Header("Current AI State")]
    [SerializeField] private State currentState = State.patrol;

    private int currentHealth;
    private int currentStun;
    private float lastStunTime;
    private float postureRemainder;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform tf;

    private Vector2 targetVelocity;
    private float stateTimer;
    private float jumpTimer;
    private bool isGrounded;
    private bool isFlying;

    public State CurrentState => currentState;
    public bool CanBeFinished => currentState == State.executionVulnerable;

    private void OnValidate()
    {
        // Auto-sets recommended default values when switching preset in Inspector.
        // User can freely edit any variable afterwards or choose 'Custom'.
        if (preset == EnemyArchetype.GroundPatrol)
        {
            walkSpeed = 3.0f;
            chaseSpeed = 5.5f;
            detectionRange = 8.0f;
            attackRange = 2.0f;
        }
        else if (preset == EnemyArchetype.HeavyJumper)
        {
            walkSpeed = 2.0f;
            chaseSpeed = 4.0f;
            jumpForce = new Vector2(7.0f, 13.0f);
            jumpCooldown = 3.0f;
            detectionRange = 10.0f;
            attackRange = 3.0f;
        }
        else if (preset == EnemyArchetype.Flying)
        {
            walkSpeed = 4.0f;
            chaseSpeed = 6.0f;
            hoverHeight = 3.5f;
            swoopSpeed = 8.0f;
            detectionRange = 10.0f;
            attackRange = 2.5f;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        tf = GetComponent<Transform>();
        anim = GetComponent<Animator>();

        if (activeHitbox == null)
            activeHitbox = GetComponentInChildren<EnemyAttack>(true);

        isFlying = (preset == EnemyArchetype.Flying);
        if (isFlying)
            rb.gravityScale = 0f;

        currentHealth = maxHealth;
        currentStun = maxStun;
        currentPhase = 1;
        currentState = State.patrol;
    }

    private void Update()
    {
        RecoverPosture();
        UpdateTargetLockOn();
        CheckPhaseProgression();

        if (currentState == State.dead)
            return;

        switch (currentState)
        {
            case State.idle:
                TickIdle();
                break;
            case State.patrol:
                TickPatrol();
                break;
            case State.chase:
                TickChase();
                break;
            case State.attack:
                TickAttack();
                break;
            case State.stun:
                TickStun();
                break;
            case State.executionVulnerable:
                TickExecutionVulnerable();
                break;
            case State.phaseTransition:
                TickPhaseTransition();
                break;
            case State.healing:
                TickHealing();
                break;
        }
    }

    private void FixedUpdate()
    {
        // Physics impulses (knockback, stun) take priority over locomotion
        if (currentState == State.stun || currentState == State.dead || currentState == State.executionVulnerable)
            return;

        if (isFlying)
        {
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            // Maintain vertical gravity while steering horizontal velocity
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    // ==========================================
    // MULTIPLAYER LOCK-ON & PERCEPTION
    // ==========================================

    private void UpdateTargetLockOn()
    {
        Vector2 eyePos = frontRay != null ? (Vector2)frontRay.position : (Vector2)transform.position + Vector2.up * 0.8f;
        float facingDir = Mathf.Sign(tf.localScale.x);

        // 1. Line-of-sight Raycast (blocked by walls!)
        RaycastHit2D hit = Physics2D.Raycast(eyePos, Vector2.right * facingDir, detectionRange, playerLayer | wallLayer);

        if (hit.collider != null)
        {
            // If the ray hits a wall first, player is behind cover and cannot be seen
            if (((1 << hit.collider.gameObject.layer) & wallLayer) != 0)
            {
                OnLostVisual();
            }
            // If the ray hits a player
            else if (((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
            {
                // Lock onto the FIRST player spotted (Multiplayer support)
                if (currentTarget == null)
                {
                    currentTarget = hit.transform;
                }
                else if (currentTarget != hit.transform)
                {
                    // If already locked on someone else, check if our locked target is dead
                    _attributes lockedAttr = currentTarget.GetComponentInParent<_attributes>();
                    if (lockedAttr != null && lockedAttr.CurrentHealth <= 0)
                    {
                        currentTarget = hit.transform; // Switch to live target
                    }
                }

                // Reset memory timer while player is in sight
                targetLostTimer = targetMemoryDuration;
            }
        }
        else
        {
            OnLostVisual();
        }

        // Clean up target if dead
        if (currentTarget != null)
        {
            _attributes targetAttr = currentTarget.GetComponentInParent<_attributes>();
            if (targetAttr != null && targetAttr.CurrentHealth <= 0)
            {
                currentTarget = null;
            }
        }
    }

    private void OnLostVisual()
    {
        if (currentTarget != null)
        {
            targetLostTimer -= Time.deltaTime;
            if (targetLostTimer <= 0f)
            {
                currentTarget = null; // Memory expired, return to patrol
            }
        }
    }

    private bool HasLedgeAhead()
    {
        if (groundRay == null) return true;
        return Physics2D.Raycast(groundRay.position, Vector2.down, 2.0f, groundLayer);
    }

    private bool PathBlockedByWall()
    {
        if (frontRay == null) return false;
        return Physics2D.Raycast(frontRay.position, Vector2.right * Mathf.Sign(tf.localScale.x), 1.0f, wallLayer | groundLayer);
    }

    // ==========================================
    // AI STATES
    // ==========================================

    private void TickIdle()
    {
        targetVelocity = Vector2.zero;
        stateTimer -= Time.deltaTime;

        if (currentTarget != null)
        {
            currentState = State.chase;
            return;
        }

        if (stateTimer <= 0f)
        {
            // Flip facing and resume patrol
            tf.localScale = new Vector3(tf.localScale.x * -1f, tf.localScale.y, 1f);
            currentState = State.patrol;
        }
    }

    private void TickPatrol()
    {
        if (currentTarget != null)
        {
            currentState = State.chase;
            return;
        }

        // Check for cliff edge or wall in path
        if (!isFlying && (!HasLedgeAhead() || PathBlockedByWall()))
        {
            stateTimer = 2.0f;
            targetVelocity.x = 0f;
            currentState = State.idle;
            return;
        }

        targetVelocity.x = walkSpeed * Mathf.Sign(tf.localScale.x);
    }

    private void TickChase()
    {
        if (currentTarget == null)
        {
            currentState = State.patrol;
            return;
        }

        float dirToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Turn towards target
        if (dirToTarget != 0f)
            tf.localScale = new Vector3(dirToTarget, tf.localScale.y, 1f);

        // Check for self-healing window
        if (canHeal && ShouldHeal(distToTarget))
        {
            StartHealing();
            return;
        }

        // Heavy Jumper Locomotion
        if (preset == EnemyArchetype.HeavyJumper)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0f && HasLedgeAhead())
            {
                jumpTimer = jumpCooldown;
                rb.linearVelocity = new Vector2(dirToTarget * jumpForce.x, jumpForce.y);
            }
        }
        // Flying Locomotion
        else if (isFlying)
        {
            Vector2 hoverTarget = (Vector2)currentTarget.position + Vector2.up * hoverHeight;
            targetVelocity = Vector2.MoveTowards(rb.linearVelocity, (hoverTarget - (Vector2)transform.position).normalized * chaseSpeed, 10f * Time.deltaTime);
        }
        // Ground Patrol Locomotion
        else
        {
            if (HasLedgeAhead())
                targetVelocity.x = chaseSpeed * dirToTarget;
            else
                targetVelocity.x = 0f;
        }

        // If within attack range of ANY available attack, initiate combat
        if (distToTarget <= GetMaxAvailableAttackRange())
        {
            TryInitiateAttack(distToTarget);
        }
    }

    // ==========================================
    // ATTACKS & SINGLE HITBOX INTEGRATION
    // ==========================================

    private float GetMaxAvailableAttackRange()
    {
        float maxRange = attackRange;
        foreach (EnemyAttackData atk in attacks)
        {
            if (atk.minPhaseRequired <= currentPhase && atk.attackRange > maxRange)
                maxRange = atk.attackRange;
        }
        return maxRange;
    }

    private void TryInitiateAttack(float distToTarget)
    {
        // Pick an attack that is in range, ready, and unlocked for this phase
        List<EnemyAttackData> validAttacks = new List<EnemyAttackData>();
        foreach (EnemyAttackData atk in attacks)
        {
            if (atk.minPhaseRequired <= currentPhase
                && distToTarget <= atk.attackRange
                && Time.time >= atk.lastUsedTime + atk.cooldown)
            {
                validAttacks.Add(atk);
            }
        }

        if (validAttacks.Count == 0)
            return;

        EnemyAttackData chosen = validAttacks[Random.Range(0, validAttacks.Count)];
        ExecuteAttack(chosen);
    }

    private void ExecuteAttack(EnemyAttackData atk)
    {
        currentState = State.attack;
        stateTimer = atk.cooldown * 0.5f;
        atk.lastUsedTime = Time.time;

        targetVelocity = Vector2.zero;

        // Configure the single child hitbox with this specific attack's parameters!
        if (activeHitbox != null)
        {
            activeHitbox.SetAttackStats(atk.damage, atk.knockbackX, atk.knockbackY, atk.parrySelfDamage);
        }

        // Fire animator trigger. In Unity animation clip, user keyframes hitbox collider on/off.
        ResetAnim();
        anim.SetTrigger(atk.animationTrigger);
    }

    private void TickAttack()
    {
        targetVelocity.x = 0f;
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            currentState = State.chase;
        }
    }

    // ==========================================
    // BOSS PHASES & HEALING
    // ==========================================

    private void CheckPhaseProgression()
    {
        // Boss Phase 2 Transition (triggers by HP threshold, deathblow is for final death!)
        if (totalPhases > 1 && currentPhase == 1 && currentHealth <= maxHealth * phase2HpThreshold)
        {
            currentPhase = 2;
            currentState = State.phaseTransition;
            stateTimer = 2.0f;

            currentStun = maxStun; // Restore posture for phase 2
            targetVelocity = Vector2.zero;

            ResetAnim();
            anim.SetTrigger("PhaseTransition");
        }
    }

    private void TickPhaseTransition()
    {
        targetVelocity = Vector2.zero;
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            currentState = State.chase;
        }
    }

    private bool ShouldHeal(float distToTarget)
    {
        return currentHealth <= maxHealth * healHpThreshold
            && Time.time >= lastHealTime + healCooldown
            && distToTarget >= attackRange * 2f;
    }

    private void StartHealing()
    {
        currentState = State.healing;
        stateTimer = 2.0f;
        lastHealTime = Time.time;
        targetVelocity = Vector2.zero;

        ResetAnim();
        anim.SetTrigger("Heal");
    }

    private void TickHealing()
    {
        targetVelocity = Vector2.zero;
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
            currentState = State.chase;
        }
    }

    // ==========================================
    // DAMAGE, POSTURE & DEATHBLOW
    // ==========================================

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentState == State.dead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // If HP reaches 0 in the final phase, enter Deathblow vulnerable state!
        if (currentHealth <= 0 && currentState != State.executionVulnerable)
        {
            OpenFinisherWindow(5.0f);
        }
    }

    public void TakePostureDamage(int amount)
    {
        if (amount <= 0 || currentState == State.dead || currentState == State.executionVulnerable)
            return;

        currentStun = Mathf.Max(0, currentStun - amount);
        lastStunTime = Time.time;
        postureRemainder = 0f;

        // Sekiro Posture Break -> enters Visceral Execution Vulnerable window!
        if (currentStun <= 0)
        {
            OpenFinisherWindow(4.0f);
        }
    }

    private void RecoverPosture()
    {
        if (currentStun >= maxStun || currentState == State.executionVulnerable
            || currentState == State.dead || Time.time < lastStunTime + postureDelay)
            return;

        postureRemainder += postureRate * Time.deltaTime;
        int recovered = Mathf.FloorToInt(postureRemainder);
        if (recovered <= 0)
            return;

        currentStun = Mathf.Min(maxStun, currentStun + recovered);
        postureRemainder -= recovered;

        if (currentStun >= maxStun)
            postureRemainder = 0f;
    }

    public void OpenFinisherWindow(float duration)
    {
        ResetAnim();
        stateTimer = duration;
        currentState = State.executionVulnerable;
        anim.SetBool("isStunned", true);
        targetVelocity = Vector2.zero;
    }

    private void TickExecutionVulnerable()
    {
        targetVelocity = Vector2.zero;
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            // If player didn't execute in time, recover some posture and resume fight
            currentStun = maxStun / 2;
            ResetAnim();
            currentState = State.chase;
        }
    }

    /// <summary>
    /// Triggered by Player ActionState.TryFinisher() for the cinematic lethal execution.
    /// </summary>
    public void ExecuteFinisher(int fatalDamage)
    {
        if (currentState != State.executionVulnerable)
            return;

        currentHealth = 0;
        currentStun = 0;
        ResetAnim();
        currentState = State.dead;
        anim.SetBool("isDead", true);

        // Turn off colliders so body doesn't block player
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (activeHitbox != null) activeHitbox.gameObject.SetActive(false);
    }

    public void ApplyKnockback(float velocityX, float velocityY, float stunDuration)
    {
        if (currentState == State.dead || currentState == State.executionVulnerable || rb == null)
            return;

        currentState = State.stun;
        stateTimer = Mathf.Max(0f, stunDuration);
        targetVelocity = Vector2.zero;

        ResetAnim();
        anim.SetBool("isStunned", true);

        rb.linearVelocity = new Vector2(velocityX, velocityY);
    }

    private void TickStun()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ResetAnim();
            currentState = State.chase;
        }
    }

    private void ResetAnim()
    {
        anim.SetBool("isStunned", false);
        anim.SetBool("isDead", false);
    }
}
