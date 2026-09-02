using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.DualShock.LowLevel;

public class Movement : MonoBehaviour
{
    [Header("Components")]
    public Animator anim;
    private Rigidbody2D RB;

    [Header("Movement Stats")]
    private float moveHorizontal;
    public float speed = 4f;
    private float prevXPos;
    private float jump = 1f;
    private float jumpTimer=0f;
    private bool jumping;

    [Header("State")]
    public LayerMask Ground;
    public Transform FeetPosition;
    private float coyoteTimer;
    private float jumpBuffer;
    private bool isGrounded;
    private bool dashing;
    private float dashTimer;
    public float shadowDashCoolDown;
    private float shadowDashtimer = 0;
    public float dashSpeed;
    private float attackTimer;
    public float attackAnimTimer;
    public float heavyAttckAnimTimer;
    private float dashCoolDown;
    public float tempdashCoolDown;
    public float dashAnimTimer;

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

    private void Awake()
    {
        dashCoolDown = tempdashCoolDown;
        currentMovementState = movementState.idle;
        currentCombatState = combatState.idle;
        RB = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        _attributes player = GetComponentInParent<_attributes>();
        isGrounded = Physics2D.OverlapCircle(FeetPosition.position,0.5f,Ground);
        moveHorizontal = Input.GetAxis("Horizontal");
        GroundCheck();
        
        if(dashCoolDown > 0)
            dashCoolDown -= Time.deltaTime;

        if (shadowDashtimer <= shadowDashCoolDown && shadowDashtimer > 0)
            shadowDashtimer -= Time.deltaTime;
        else
            shadowDashtimer = 0;

        if (Input.GetButtonDown("Dash") && dashCoolDown<=0)
        {
            BackToIdle();
            currentMasterState = masterState.dashing;
        }

        if (currentMasterState == masterState.dashing)
        {
            if (dashTimer > dashAnimTimer)
            {
                BackToIdle();
                gameObject.layer = LayerMask.NameToLayer("Player");
                dashTimer = 0;
                RB.linearVelocity = new Vector2(0, RB.linearVelocity.y);
                dashCoolDown = tempdashCoolDown;
                currentMasterState = masterState.stunned;
                StartCoroutine(InterruptAction(dashAnimTimer));
            }
            else
            {
                anim.SetBool("isDASHING",true);
                HandleDashing();
            }
            return;
        }
        else if(currentMasterState == masterState.stunned)
        {
            
            return;
        }

        else if(currentMasterState == masterState.free)
        {
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
                        player.parryTimer = 0.08f;
                        BackToIdle();
                        anim.SetBool("isGAURDING", true);
                        currentCombatState = combatState.gaurding;
                    }
                    break;

                case combatState.gaurding:
                    RB.linearVelocityX = 0;
                    player.isGuarded = true;
                    player.parryTimer -= Time.deltaTime;
                    if (Input.GetButtonUp("Gaurd"))
                    {   
                        player.isGuarded = false;
                        BackToIdle();
                        currentCombatState = combatState.idle;
                    }
                    if (Input.GetButtonDown("Attack"))
                    {
                        attackTimer = heavyAttckAnimTimer;
                        currentCombatState = combatState.attacking;
                        BackToIdle();
                        anim.SetBool("isHEAVYATTACKING", true);
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
                        HandleJumping();

                        if (RB.linearVelocityY <= 0)
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

                        if (moveHorizontal == 0)
                        {
                            BackToIdle();
                            currentMovementState = movementState.idle;
                        }
                        else if (Input.GetButtonDown("Jump"))
                        {
                            jump = 20;
                            jumpBuffer = 0.5f;
                            BackToIdle();
                            currentMovementState = movementState.jumping;
                        }
                        if (RB.linearVelocityY < 0)
                        {
                            BackToIdle();
                            anim.SetBool("isFALLING", true);
                            currentMovementState = movementState.falling;
                        }
                        break;
                }
            }
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
    }
    private void idle()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jump = 20;
            jumpBuffer = 0.5f;
            currentMovementState = movementState.jumping;
        }
        else if (moveHorizontal != 0)
        {
            currentMovementState = movementState.walking;
        }
        else if (!isGrounded && RB.linearVelocityY < 0 && currentMovementState != movementState.jumping)
        {
            BackToIdle();
            anim.SetBool("isFALLING", true);
            currentMovementState = movementState.falling;
        }
        else
        {
            RB.linearVelocity = new Vector2(0, RB.linearVelocity.y);
        }

    }

    public IEnumerator InterruptAction(float duration, bool dead = false)
    {
        if (dead)
        {
            BackToIdle();
            anim.SetBool("isDEAD",true);
        }
        RB.linearVelocity = new Vector2(0, RB.linearVelocity.y);
        
        jumping = false;
        jump = 0;
        jumpTimer = 0;
        dashTimer = 0;
        yield return new WaitForSecondsRealtime(duration);
        currentMasterState = masterState.free;

    }


    private void HandleDashing()
    {
        dashTimer += Time.deltaTime;
        RB.linearVelocity = new Vector2(Mathf.Sign(transform.localScale.x) * dashSpeed, 0);
        if (shadowDashtimer == 0)
        {
            shadowDashtimer = shadowDashCoolDown;
            gameObject.layer = LayerMask.NameToLayer("Ghost");
        }
        else
        {
        }
    }

    private void HandleMovement()
    {         
        RB.linearVelocityX = speed * moveHorizontal;
        if (moveHorizontal < -0.1f)
            transform.localScale = new Vector3(-1, 1, 0);
        else if (moveHorizontal > 0.1f)
            transform.localScale = new Vector3(1, 1, 0);
    }
    private void HandleJumping()
    {
        jumpBuffer -= Time.deltaTime;

        if (coyoteTimer > 0 && jumpBuffer > 0)
            jumping = true;

        if ( jumping && Input.GetButton("Jump"))
            if (jumpTimer <= 0.3f)
            {
                jumpTimer += Time.deltaTime;
                jumpBuffer = 0;
                coyoteTimer = 0;
                RB.linearVelocityY = jump;
                if (jump<20 && jump > 0)
                    jump -= 4.5f;
            }
            else
            {
                jump = 20;
                jumping = false;
            }
        else
        {
            jumpTimer = 0;
            jump = 20;
            jumping = false;
        }
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
}