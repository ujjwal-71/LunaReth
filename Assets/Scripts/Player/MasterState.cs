using System.Collections;
using UnityEngine;

public enum MasterStateType
{
    Free,
    Dashing,
    Stunned,
    Dead,
    KnockedBack
}

[RequireComponent(typeof(Rigidbody2D))]
public class MasterState : MonoBehaviour
{
    [Header("Current Master State")]
    [SerializeField] private MasterStateType currentState = MasterStateType.Free;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    [Tooltip("Total Dash Lockout Time: Set to (Dash Animation Length + Extra Recovery Pause). Player cannot move or act during this entire duration.")]
    public float dashTimer = 0.4f;
    [Tooltip("Active forward slide duration before momentum stops (e.g. 0.2s). The remainder of dashTimer acts as the recovery pose.")]
    public float dashSlideDuration = 0.2f;
    public float dashCooldown = 0.5f;

    private Rigidbody2D rb;
    private Animator anim;
    private ActionState actionScript;

    private Coroutine activeRoutine;
    private string originalTag;
    private float lastDashTime = -100f;
    private float storedGravity;

    public MasterStateType CurrentState => currentState;

    /// <summary>
    /// True when player is allowed to move horizontally and jump.
    /// False during Dash, Stun, Knockback, Death, and active Attack/Guard actions.
    /// </summary>
    public bool CanMove => currentState == MasterStateType.Free && (actionScript == null || !actionScript.IsActing);

    /// <summary>
    /// True when player can initiate new attacks, guards, or combat arts.
    /// </summary>
    public bool CanAct => currentState == MasterStateType.Free;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        actionScript = GetComponent<ActionState>();
        
        originalTag = gameObject.tag;
        storedGravity = rb.gravityScale;

        SetState(MasterStateType.Free);
    }

    private void Update()
    {
        if (CanAct)
        {
            if (Input.GetButtonDown("Dash") && Time.time >= lastDashTime + dashCooldown)
            {
                TriggerDash();
            }
        }
    }

    public void SetState(MasterStateType newState)
    {
        currentState = newState;

        if (currentState == MasterStateType.Free)
        {
            gameObject.tag = originalTag;
            // Restore gravity in case a previous state zeroed it
            rb.gravityScale = storedGravity;
        }
        else
        {
            ResetAnim();
        }
    }

    // ==========================================
    // DASH
    // ==========================================

    public void TriggerDash()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        SetState(MasterStateType.Dashing);
        anim.SetBool("isDASHING", true);

        float direction = transform.localScale.x > 0 ? 1f : -1f;

        // Zero gravity, straight horizontal line
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);

        float slideTime = Mathf.Min(dashSlideDuration, dashTimer);

        // 1. Active dash slide
        yield return new WaitForSeconds(slideTime);

        // 2. Stop momentum for the recovery pose
        rb.linearVelocity = Vector2.zero;

        // 3. Wait remaining duration (intentional end-lag recovery)
        float recoveryTime = dashTimer - slideTime;
        if (recoveryTime > 0f)
        {
            yield return new WaitForSeconds(recoveryTime);
        }

        lastDashTime = Time.time;
        anim.SetBool("isDASHING", false);
        SetState(MasterStateType.Free);
    }

    // ==========================================
    // RECOIL (doesn't interrupt combat state)
    // ==========================================

    /// <summary>
    /// Applies a physics recoil pushback (e.g. on landing sword hits)
    /// WITHOUT interrupting attack states, combos, or inputs.
    /// </summary>
    public void ApplyRecoil(float velocityX, float velocityY)
    {
        if (currentState == MasterStateType.Dead)
            return;

        rb.linearVelocity = new Vector2(velocityX, velocityY);
    }

    // ==========================================
    // KNOCKBACK (takes damage, enemy hits, hazards)
    // ==========================================

    /// <summary>
    /// Used when the player takes damage or gets launched by an enemy.
    /// Locks player into KnockedBack state for recoveryTime.
    /// </summary>
    public void ApplyKnockback(float velocityX, float velocityY, float recoveryTime = 0.2f)
    {
        if (currentState == MasterStateType.Dead)
            return;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(KnockbackRoutine(velocityX, velocityY, recoveryTime));
    }

    private IEnumerator KnockbackRoutine(float velocityX, float velocityY, float recoveryTime)
    {
        SetState(MasterStateType.KnockedBack);

        // Restore gravity so knockback arcs naturally
        rb.gravityScale = storedGravity;
        rb.linearVelocity = new Vector2(velocityX, velocityY);

        yield return new WaitForSeconds(recoveryTime);
        
        SetState(MasterStateType.Free);
    }

    // ==========================================
    // STUN (posture break)
    // ==========================================

    public void TriggerStun(float duration)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        SetState(MasterStateType.Stunned);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = storedGravity;

        yield return new WaitForSeconds(duration);
        SetState(MasterStateType.Free);
    }

    // ==========================================
    // DEATH
    // ==========================================

    public void TriggerDeath(float duration)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(DeathRoutine(duration));
    }

    private IEnumerator DeathRoutine(float duration)
    {
        SetState(MasterStateType.Dead);
        anim.SetBool("isDEAD", true);
        rb.linearVelocity = Vector2.zero;
        
        yield return new WaitForSeconds(duration);
    }

    // ==========================================
    // ANIM CLEANUP
    // ==========================================

    public void ResetAnim()
    {
        anim.SetBool("isGAURDING", false);
        anim.SetBool("isRUNNING", false);
        anim.SetBool("isJUMPING", false);
        anim.SetBool("isFALLING", false);
        anim.SetBool("isDEAD", false);
        anim.SetBool("isDASHING", false);
        anim.SetBool("isWALLSTUCK", false);
        anim.SetInteger("AttackIndex", 0);
    }
}
