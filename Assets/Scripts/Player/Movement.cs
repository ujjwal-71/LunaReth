using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Movement : MonoBehaviour
{
    [Header("Components")]
    public Animator anim;
    public GameObject attacker;
    public SpriteRenderer Attack_Sprite;
    public Transform Attack_Transform;
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
    public float jumpPower;

    [Header("State")]
    public LayerMask Ground;
    public Transform FeetPosition;
    private float coyoteTimer = 0f;
    private float jumpBuffer = 0f;
    private bool isGrounded = true;
    private bool dashing;
    private bool dash;
    private float dashTimer;
    private float dashPauseTimer;
    private bool dashPause;
    private float attacktimer = 0;


    private void Awake()
    {
        attacker.SetActive(false);
        RB = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        DashEff = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(FeetPosition.position,0.15f,Ground);
        moveHorizontal = Input.GetAxis("Horizontal");
        GroundCheck();
        HandleDashing();
        if (!dashPause)
        {
            AnimHAndle();
            HandleMovement();
            HandleJumping();
            HandleAttacking();
        }
    }

    public void InterruptAction()
    {
        jumping = false;
        jump = 0;
        jumpTimer = 0;
        
        dashing = false;
        dashTimer = 0;
        
        attacktimer = 0;
        attacker.SetActive(false);
    }
    private void HandleDashing()
    {
        if (dash && Input.GetButtonDown("Dash"))
            dashing = true;

        if (dashing)
        {
            if (dashTimer > 0.1f)
            {
                DashEff.Stop();
                RB.linearVelocity = Vector2.zero;
                dashing = false;
                InterruptAction();
                dashPauseTimer = 0.3f;
            }
            else
            {
                dash = false;
                dashTimer += Time.deltaTime;
                RB.linearVelocity = transform.right * 40;
                DashEff.Play();
            }
        }
        if (dashPauseTimer > 0)
        {
            dashPause = true;
            dashPauseTimer -= Time.deltaTime;
        }
        else
            dashPause = false;
    }

    private void HandleMovement()
    {
        if (!dashing)
        {
            RB.linearVelocityX = speed * moveHorizontal;

            if (moveHorizontal < -0.1f)
                transform.rotation = Quaternion.Euler(0,180,0);

            else if (moveHorizontal > 0.1f)
                transform.rotation = Quaternion.Euler(0,0,0);
        }
    }
    private void HandleJumping()
    {
        if (!dashing)
        {
            if (Input.GetButtonDown("Jump"))
                jumpBuffer = 0.15f;
            else
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
                    if (jump<16)
                        jump += jumpPower * Time.deltaTime;
                }
                else
                {
                    jump = 0;
                    jumping = false;
                }
            else
            {
                jumpTimer = 0;
                jump = 0;
                jumping = false;
            }
        }
    }

    private void HandleAttacking()
    {
        if (Input.GetButtonDown("Attack") && attacktimer <= 0)
        {
            attacker.SetActive(true);
            attacktimer = 0.25f;
        }

        if (attacktimer > 0)
        {
            attacktimer -= Time.deltaTime;
            if (attacktimer <= 0)
            {
                attacker.SetActive(false);
            }
        }
    }

    private void GroundCheck()
    {
        if (isGrounded)
        {
            coyoteTimer = 0.1f;
            if (!dashing)
                dash = true;
        }
        else
            coyoteTimer -= Time.deltaTime;
    }

    private void AnimHAndle()
    {
        if (RB.linearVelocityY < -0.1f)
        {
            anim.SetBool("isJUMPED", false);
            anim.SetBool("isGROUNDED", false);
            anim.SetBool("isFALLING", true);
        }
        else if (RB.linearVelocityY > 0.1f)
        {
            anim.SetBool("isJUMPED", true);
            anim.SetBool("isGROUNDED", false);
            anim.SetBool("isFALLING", false);
        }
        else
        {
            anim.SetBool("isJUMPED", false);
            anim.SetBool("isGROUNDED", true);
            anim.SetBool("isFALLING", false);
        }

        if (isGrounded && (moveHorizontal > 0.1f || moveHorizontal < -0.1f))
            anim.SetBool("isRUNNING",true);

        else
            anim.SetBool("isRUNNING",false);
    }
}
