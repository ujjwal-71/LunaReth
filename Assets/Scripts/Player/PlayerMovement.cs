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

    [Header("State")]
    public LayerMask Ground;

    public Transform FeetPosition;
    private float coyoteTimer;
    private float jumpBuffer;
    private bool isGrounded;
    private bool dashing;
    private bool isDoingShadowDash;
    private float dashTimer;
    public float shadowDashCoolDown;
    private float shadowDashtimer = 0;
    public float ShadowDashSpeed = 60;
    public float dashSpeed;
    private float attackTimer;
    public float attackAnimTimer;
    public float heavyAttckAnimTimer;
    private float tempdashCoolDown;
    public float dashCoolDown;
    public float dashAnimTimer;
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
        tempdashCoolDown = dashCoolDown;
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
        
        if(tempdashCoolDown > 0)
            tempdashCoolDown -= Time.deltaTime;

        if (shadowDashtimer <= shadowDashCoolDown && shadowDashtimer > 0)
            shadowDashtimer -= Time.deltaTime;
        else
            shadowDashtimer = 0;

        if (Input.GetButtonDown("Dash") && tempdashCoolDown <= 0)
        {
            if (currentMasterState != masterState.dead && currentMasterState != masterState.stunned)
            {
                BackToIdle();
                dashTimer = 0;
                currentMasterState = masterState.dashing;
                if (shadowDashtimer <= 0)
                {
                    isDoingShadowDash = true;
                    anim.SetBool("isSHADOWDASHING", true);
                    gameObject.layer = LayerMask.NameToLayer("Ghost");
                    targetVelocity = new Vector2(Mathf.Sign(transform.localScale.x) * ShadowDashSpeed * -1, 0);
                }
                else
                {
                    isDoingShadowDash = false;
                    anim.SetBool("isDASHING", true);
                    targetVelocity = new Vector2(Mathf.Sign(transform.localScale.x) * dashSpeed * -1, 0);
                }
            }
        }

        if (currentMasterState == masterState.dashing)
        {
            dashTimer += Time.deltaTime;
            
            if (dashTimer >= dashAnimTimer) 
            {
                BackToIdle();
                gameObject.layer = LayerMask.NameToLayer("Player");
                dashTimer = 0;
                
                tempdashCoolDown = dashCoolDown;
                if (isDoingShadowDash)
                {
                    shadowDashtimer = shadowDashCoolDown;
                    isDoingShadowDash = false;
                }
                
                RB.linearVelocity = new Vector2(0, RB.linearVelocity.y);
                currentMasterState = masterState.free; 
            }
            return;
        }
        else if(currentMasterState == masterState.stunned)
        {
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
        isGrounded = Physics2D.OverlapCircle(FeetPosition.position, 0.5f, Ground);

        if (currentMasterState == masterState.stunned) 
        { 
            
        }
        else if (currentMasterState == masterState.dashing)
        {
            targetVelocity.x *= 0.8f; 
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
                RB.linearVelocityY *= -0.4f;

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
        dashTimer = 0;
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