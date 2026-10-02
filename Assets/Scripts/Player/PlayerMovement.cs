using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Components")]
    private Animator anim;
    private Rigidbody2D RB;

    [Header("Movement Stats")]
    private float moveHorizontal;
    public float speed = 4f;
    private Vector2 targetVelocity;
    private bool jumpRequested;
    private bool jumpCutOff;
    [Header("Dash Elements")]
    private bool dashing;
    private bool canDash;
    public float dashTimer;
    public int dashForce;

    [Header("State")]
    public LayerMask Ground;

    public Transform FeetPosition;
    private float coyoteTimer;
    private float jumpBuffer;
    private bool isGrounded;
    private float attackTimer;
    public float attackAnimTimer;
    public float heavyAttckAnimTimer;
    public float maxJumpForce = 20f;

    private enum masterState
    {
        free,
        dashing,
        stunned,
        dead,
    }
    private enum movementState
    {
        idle,
        jumping,
        falling,
        walking,
    }
    private enum combatState
    {
        idle,
        attacking,
        gaurding,
    }
    private masterState currentMasterState;
    private movementState currentMovementState;
    private combatState currentCombatState;

    private void Start()
    {
        anim = GetComponent<Animator>();
        currentMovementState = movementState.idle;
        currentCombatState = combatState.idle;
        RB = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        jumpBuffer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
            jumpBuffer = 0.5f;
        if (Input.GetButtonUp("Jump"))
            jumpCutOff = true;

        _attributes playerStats = GetComponentInParent<_attributes>();
        moveHorizontal = Input.GetAxis("Horizontal");
        GroundCheck();
        if (currentMasterState == masterState.dead)
        {
            canDash = false;
            return;
        }

        if (Input.GetButtonDown("Dash") && canDash)
        {
            currentMasterState = masterState.dashing;
        }

        if (currentMasterState == masterState.stunned)
        {
            canDash = false;
            return;
        }
        else if (currentMasterState == masterState.dashing)
        {
            StartCoroutine(Dashing(dashTimer));
            return;
        }
        else if(currentMasterState == masterState.free)
        {
            if (jumpBuffer > 0 && coyoteTimer > 0 && currentCombatState == combatState.idle && currentMasterState == masterState.free)
            {
                jumpRequested = true;
                jumpBuffer = 0;
                coyoteTimer = 0;
                BackToIdle();
                currentMovementState = movementState.jumping;
            }
            switch (currentCombatState)
            {
                
                case combatState.idle:
                    if (Input.GetButtonDown("Attack"))
                    {
                        attackTimer = attackAnimTimer;
                        BackToIdle();
                        anim.SetBool("isATTACKING", true);
                        currentCombatState = combatState.attacking;
                    }
                    else if (Input.GetButtonDown("Gaurd"))
                    {
                        currentCombatState = combatState.gaurding;
                        playerStats.parryTimer = 0.2f;
                        BackToIdle();
                        anim.SetBool("isGAURDING", true);
                    }
                    break;

                case combatState.gaurding:
                    targetVelocity.x = 0;
                    playerStats.isGuarded = true;
                    playerStats.parryTimer -= Time.deltaTime;
                    if (Input.GetButtonUp("Gaurd"))
                    {   
                        playerStats.isGuarded = false;
                        BackToIdle();
                        currentCombatState = combatState.idle;
                    }
                    if (Input.GetButtonDown("Attack"))
                    {
                        attackTimer = heavyAttckAnimTimer;
                        BackToIdle();
                        anim.SetBool("isHEAVYATTACKING", true);
                        currentCombatState = combatState.attacking;
                    }
                    
                    break;

                case combatState.attacking:
                    if (attackTimer < 0)
                    {
                        BackToIdle();
                        currentCombatState = combatState.idle;
                    }
                    if (Input.GetButtonDown("Attack"))
                    {
                        BackToIdle();
                        anim.SetBool("isATTACKING", true);
                        attackTimer = attackAnimTimer;
                    }

                    attackTimer -= Time.deltaTime;
                    break;
            }
            if (currentCombatState == combatState.idle)
            {
                switch (currentMovementState)
                {
                    case movementState.idle:
                        idle();
                        break;

                    case movementState.falling:
                        HandleMovement();
                        if (isGrounded)
                        {
                            BackToIdle();
                            currentMovementState = movementState.idle;
                        }
                        Debug.Log("falling");
                        break;

                    case movementState.jumping:
                        anim.SetBool("isJUMPING", true);
                        HandleMovement();

                        if (RB.linearVelocity.y <= -10 && !jumpRequested)
                        {
                            BackToIdle();
                            anim.SetBool("isFALLING", true);
                            BackToIdle();
                            anim.SetBool("isFALLING", true);
                            currentMovementState = movementState.falling;
                        }
                        Debug.Log("jumped");
                        break;

                    case movementState.walking:
                        anim.SetBool("isRUNNING", true);
                        HandleMovement();

                        if (RB.linearVelocity.y < -10)
                        {
                            BackToIdle();
                            anim.SetBool("isFALLING", true);
                            currentMovementState = movementState.falling;
                            return;
                        }
                        if (Mathf.Abs(moveHorizontal) < 0.1f)
                        {
                            BackToIdle();
                            currentMovementState = movementState.idle;
                        }
                        break;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (RB.linearVelocity.y < -50)
            RB.linearVelocityY = -50;
        isGrounded = Physics2D.OverlapCircle(FeetPosition.position, 0.5f, Ground);

        if (currentMasterState == masterState.stunned) 
        { 
            
        }
        else if (currentMasterState == masterState.dashing)
        {
            RB.linearVelocity = new Vector2(targetVelocity.x, 0);
        }
        else
        {
            RB.linearVelocity = new Vector2(targetVelocity.x, RB.linearVelocity.y);
        }

        if (jumpRequested)
        {
            RB.linearVelocityY = maxJumpForce;
            jumpRequested = false;
        }
        if (jumpCutOff)
        {
            if (RB.linearVelocityY > 0)
                RB.linearVelocityY *= -0.1f;

            jumpCutOff = false;
        }
    }
    public void BackToIdle()
    {
        anim.SetBool("isATTACKING", false);
        anim.SetBool("isHEAVYATTACKING", false);
        anim.SetBool("isRUNNING", false);
        anim.SetBool("isJUMPING", false);
        anim.SetBool("isFALLING", false);
        anim.SetBool("isGAURDING", false);
        anim.SetBool("isDEAD", false);
        anim.SetBool("isDASHING", false);
        anim.SetBool("isSHADOWDASHING", false);
    }

    private IEnumerator Dashing(float duration)
    {
        canDash = false;
        dashing = true;
        BackToIdle();
        anim.SetBool("isDASHING", true);
        targetVelocity.x = dashForce * transform.localScale.x * -1; 
        yield return new WaitForSeconds(duration);
        BackToIdle();
        dashing = false;
        currentMasterState = masterState.free;
        GravityChange(0,1f);

    }
    private IEnumerator GravityChange(float amount, float duration)
    {
        RB.gravityScale = amount;
        yield return new WaitForSecondsRealtime(duration);
        RB.gravityScale = 7;
    }
    private void idle()
    {
        if (moveHorizontal != 0)
        {
            currentMovementState = movementState.walking;
        }
        else if (!isGrounded && RB.linearVelocity.y <= -10 && currentMovementState != movementState.jumping)
        {
            BackToIdle();
            anim.SetBool("isFALLING", true);
            currentMovementState = movementState.falling;
        }
        else
        {
            targetVelocity.x = 0;
        }

    }

    public IEnumerator InterruptAction(float duration, bool dead = false)
    {
        if (dead)
        {
            BackToIdle();
            anim.SetBool("isDEAD",true);
        }
        targetVelocity.x = 0;
        yield return new WaitForSecondsRealtime(duration);
        BackToIdle();
        currentMasterState = masterState.free;
    }

    private void HandleMovement()
    {         
        targetVelocity.x = speed * moveHorizontal;
        if (moveHorizontal < -0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveHorizontal > 0.1f)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private void GroundCheck()
    {
        if (isGrounded)
        {
            coyoteTimer = 0.2f;
            if (!dashing)
            {
                if (currentMovementState == movementState.falling)
                {
                    BackToIdle();
                    currentMovementState = movementState.idle;
                }
            }
            canDash = true;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
            if(currentMovementState != movementState.jumping)
                currentMovementState = movementState.falling;
        }
    }

    public void ApplyKnockback(float pushForceX, float pushForceY)
    {
        RB.linearVelocity = Vector2.zero; 
        RB.AddForce(new Vector2(pushForceX, pushForceY), ForceMode2D.Impulse);
    }
}