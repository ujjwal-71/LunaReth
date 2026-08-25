using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.DualShock.LowLevel;

public class Movement : MonoBehaviour
{
    [Header("Components")]
    public Animator anim;
    private SpriteRenderer sprite;
    private Rigidbody2D RB;
    private ParticleSystem DashEff;

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
    private float coyoteTimer = 0f;
    private float jumpBuffer = 0f;
    private bool isGrounded = true;
    private bool dashing;
    private float dashTimer;
    private float shadowDashtimer = 0;
    private float dashSpeed;
    private float attackTimer;
    private float dashCoolDown;

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
        currentMovementState = movementState.idle;
        currentCombatState = combatState.idle;
        RB = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        DashEff = GetComponent<ParticleSystem>();
        DashEff.Play();
    }

    void Update()
    {
        _attributes player = GetComponentInParent<_attributes>();
        isGrounded = Physics2D.OverlapCircle(FeetPosition.position,0.35f,Ground);
        moveHorizontal = Input.GetAxis("Horizontal");
        GroundCheck();
        
        if(dashCoolDown > 0)
            dashCoolDown -= Time.deltaTime;

        if (Input.GetButtonDown("Dash") && dashCoolDown<=0)
        {
            currentMasterState = masterState.dashing;
        }

        if (currentMasterState == masterState.dashing)
        {
            if (dashTimer > 0.1f)
            {
                gameObject.layer = LayerMask.NameToLayer("Player");
                anim.SetBool("isDASHING",false);
                dashTimer = 0;
                RB.linearVelocity = new Vector2(0, RB.linearVelocity.y);
                dashCoolDown = 0.5f;
                currentMasterState = masterState.stunned;
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
            StartCoroutine(InterruptAction(0.1f));
            return;
        }

        switch (currentCombatState)
        {
            
            case combatState.idle:
                anim.SetBool("isATTACKING", false);
                anim.SetBool("isGAURDING", false);
                if (Input.GetButtonDown("Attack"))
                {
                    attackTimer = 0.21f;
                    currentCombatState = combatState.attacking;
                }
                else if (Input.GetButtonDown("Gaurd"))
                {
                    player.parryTimer = 0.08f;
                    currentCombatState = combatState.gaurding;
                }
                break;

            case combatState.gaurding:
                anim.SetBool("isGAURDING", true);
                player.isGuarded = true;
                player.parryTimer -= Time.deltaTime;
                if (Input.GetButtonUp("Gaurd"))
                {   
                    player.isGuarded = false;
                    currentCombatState = combatState.idle;
                }
                if (Input.GetButtonDown("Attack"))
                {
                    attackTimer = 0.21f;
                    currentCombatState = combatState.attacking;
                }
                
                break;

            case combatState.attacking:
                anim.SetBool("isATTACKING", true);
                if (attackTimer < 0)
                    currentCombatState = combatState.idle;
                else
                    attackTimer -= Time.deltaTime;
                break;
        }

        switch (currentMovementState)
        {
            case movementState.idle:
                anim.SetBool("isRUNNING",false);
                idle();
                break;

            case movementState.falling:
                anim.SetBool("isJUMPED", false);
                anim.SetBool("isRUNNING", false);
                anim.SetBool("isFALLING", true);
                HandleMovement();
                if (isGrounded)
                {
                    currentMovementState = movementState.idle;
                }
                Debug.Log("falling");
                break;

            case movementState.jumping:
                anim.SetBool("isJUMPED",true);
                anim.SetBool("isRUNNING",false);
                HandleMovement();
                HandleJumping();

                if (RB.linearVelocityY <= 0) 
                    currentMovementState = movementState.falling;
                Debug.Log("jumped");
                break;

            case movementState.walking:
                anim.SetBool("isRUNNING", true);
                HandleMovement();

                if (moveHorizontal == 0) 
                    currentMovementState = movementState.idle;
                else if (Input.GetButtonDown("Jump"))
                {
                    jump = 16;
                    jumpBuffer = 0.25f;
                    currentMovementState = movementState.jumping;
                }
                if (RB.linearVelocityY < 0)
                {
                    currentMovementState = movementState.falling;
                }
                break;
        }
    }

    private void idle()
    {
        anim.SetBool("isJUMPED", false);
        anim.SetBool("isFALLING", false);
        anim.SetBool("isRUNNING", false);
        RB.linearVelocity = new Vector2(0, RB.linearVelocity.y);

        if (Input.GetButtonDown("Jump"))
        {
            jump = 16;
            jumpBuffer = 0.25f;
            currentMovementState = movementState.jumping;
        }
        else if (moveHorizontal != 0)
            currentMovementState = movementState.walking;
        else if (!isGrounded && RB.linearVelocityY < 0 && currentMovementState != movementState.jumping) 
            currentMovementState = movementState.falling;
    }

    public IEnumerator InterruptAction(float duration)
    {
        RB.linearVelocity = new Vector2(0, RB.linearVelocity.y);
        
        jumping = false;
        jump = 0;
        jumpTimer = 0;
        dashTimer = 0;
        anim.SetBool("isATTACKING", false);
        anim.SetBool("isGAURDING", false);
        anim.SetBool("isRUNNING", false);
        anim.SetBool("isJUMPED", false);
        anim.SetBool("isFALLING", false);
        yield return new WaitForSecondsRealtime(duration);
        currentMasterState = masterState.free;

    }


    private void HandleDashing()
    {
        dashTimer += Time.deltaTime;
        RB.linearVelocity = new Vector2(Mathf.Sign(transform.localScale.x) * dashSpeed, 0);

        var trailsModule = DashEff.trails;
        shadowDashtimer -= Time.deltaTime;
        
        if (shadowDashtimer > 0)
        {
            trailsModule.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.black);
            dashSpeed = 100;
            gameObject.layer = LayerMask.NameToLayer("Ghost");
        }
        else
        {
            trailsModule.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.white);
            dashSpeed = 80;
        }
    }

    private void HandleMovement()
    {               
        RB.linearVelocityX = speed * moveHorizontal;
        if (moveHorizontal < -0.1f)
            transform.localScale = new Vector3(-8, 8, 0);
        else if (moveHorizontal > 0.1f)
            transform.localScale = new Vector3(8, 8, 0);
    }
    private void HandleJumping()
    {
        jumpBuffer -= Time.deltaTime;

        if (coyoteTimer > 0 && jumpBuffer > 0)
            jumping = true;

        if ( jumping && Input.GetButton("Jump"))
            if (jumpTimer <= 0.3f)
            {
                anim.SetBool("isJUMPED", true);
                jumpTimer += Time.deltaTime;
                jumpBuffer = 0;
                coyoteTimer = 0;
                RB.linearVelocityY = jump;
                if (jump<16 && jump > 0)
                    jump -= 4.5f;
            }
            else
            {
                jump = 15;
                jumping = false;
            }
        else
        {
            jumpTimer = 0;
            jump = 16;
            jumping = false;
        }
    }


    private void GroundCheck()
    {
        if (isGrounded)
        {
            coyoteTimer = 0.1f;
            if (!dashing)
            {
                if (currentMovementState == movementState.falling)
                    currentMovementState = movementState.idle;
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
