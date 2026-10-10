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
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    private float lastDashTime = -100f;

    [Header("Components")]
    private Rigidbody2D rb;
    private Animator anim;
    private Movement movementScript;
    private ActionState actionScript;

    private Coroutine activeRoutine;
    private string originalTag;

    public MasterStateType CurrentState => currentState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        movementScript = GetComponent<Movement>();
        actionScript = GetComponent<ActionState>();
        
        // Save the original tag (e.g. "Player") so we can return to it after dashing
        originalTag = gameObject.tag; 
        if (string.IsNullOrEmpty(originalTag)) 
        {
            originalTag = "Player"; // Fallback just in case!
        }
        
        SetState(MasterStateType.Free);
    }

    private void Update()
    {
        // Only allow dashing if we are free to move
        if (currentState == MasterStateType.Free)
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
            if (movementScript != null) movementScript.enabled = true;
            if (actionScript != null) actionScript.enabled = true;
            
            // Make sure we lose the Ghost tag when returning to normal
            if (!string.IsNullOrEmpty(originalTag))
            {
                gameObject.tag = originalTag; 
            }
        }
        else
        {
            if (movementScript != null) movementScript.enabled = false;
            if (actionScript != null) actionScript.enabled = false;
            
            ResetAnimatorBools();
        }
    }

    // ==========================================
    // REUSABLE ROUTINES & TRIGGERS
    // ==========================================

    public void TriggerDash()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        SetState(MasterStateType.Dashing);
        
        // 1. Give IFrames by changing tag!
        gameObject.tag = "Ghost"; 
        
        if (anim != null) anim.SetBool("isDASHING", true);

        // 2. Hollow Knight Dash Physics (Zero gravity, straight line)
        float direction = transform.localScale.x > 0 ? 1f : -1f;
        float originalGravity = rb.gravityScale;
        
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(direction * dashSpeed, 0f);

        // 3. Wait for dash to finish
        yield return new WaitForSeconds(dashDuration);

        // 4. Restore everything
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero; // Stop momentum immediately after dash
        lastDashTime = Time.time;
        
        if (anim != null) anim.SetBool("isDASHING", false);
        SetState(MasterStateType.Free);
    }

    /// <summary>
    /// Can be used for BOTH enemy hit knockback AND attack recoil!
    /// </summary>
    public void ApplyKnockback(float pushForceX, float pushForceY, float recoveryTime = 0.2f)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(KnockbackRoutine(pushForceX, pushForceY, recoveryTime));
    }

    private IEnumerator KnockbackRoutine(float forceX, float forceY, float recoveryTime)
    {
        SetState(MasterStateType.KnockedBack);
        
        rb.linearVelocity = Vector2.zero; // Wipe current velocity
        rb.AddForce(new Vector2(forceX, forceY), ForceMode2D.Impulse);

        yield return new WaitForSeconds(recoveryTime);
        
        SetState(MasterStateType.Free);
    }

    public void TriggerStun(float duration)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        SetState(MasterStateType.Stunned);
        yield return new WaitForSeconds(duration);
        SetState(MasterStateType.Free);
    }

    public void TriggerDeath(float duration)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(DeathRoutine(duration));
    }

    private IEnumerator DeathRoutine(float duration)
    {
        SetState(MasterStateType.Dead);
        if (anim != null) anim.SetBool("isDEAD", true);
        
        yield return new WaitForSeconds(duration);
    }

    public void ResetAnimatorBools()
    {
        if (anim == null) return;
        anim.SetBool("isHEAVYATTACKING", false);
        anim.SetBool("isGAURDING", false);
        anim.SetBool("isRUNNING", false);
        anim.SetBool("isJUMPING", false);
        anim.SetBool("isFALLING", false);
        anim.SetBool("isDEAD", false);
        anim.SetBool("isDASHING", false);
        anim.SetBool("isSHADOWDASHING", false);
    }
}
