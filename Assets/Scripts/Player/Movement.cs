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
    public float speed = 4f;
    private float jump = 1f;
    private float jumpTimer=0f;
    private bool jumping;
    public float jumpPower;

    [Header("State")]
    public LayerMask Ground;
    public Transform FeetPosition;
    private bool isGrounded = true;
    private bool wasGrounded;
    private int direc;
    private bool dashing;
    private bool dash;
    private float dashTimer;
    private float attacktimer = 0;
    

    void Start()
    {
        attacker.SetActive(false);
        RB = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        DashEff = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(FeetPosition.position,0.15f,Ground);
        GroundCheck();
        HandleDashing();
        HandleMovement();
        HandleJumping();
        HandleAttacking();
    }

        // This function only runs in the Unity Editor Scene view so you can see your invisible shapes!

    private void HandleDashing()
    {
        if (dash && Input.GetKeyDown(KeyCode.X))
        {
            dashing = true;
            if (sprite.flipX)
                direc = -1;
            else
                direc = 1;
        }

        if (dashing)
        {
            if (dashTimer > 0.1f)
            {
                RB.linearVelocity = new Vector2(0, 0);
                dashing = false;
                dashTimer = 0;
                DashEff.Stop();
            }
            else
            {
                DashEff.Play();
                dash = false;
                dashTimer += Time.deltaTime;
                RB.linearVelocity = new Vector2(direc * 50, 0);
            }
        }
    }

    private void HandleMovement()
    {
        if (!dashing)
        {
            if (Input.GetKey(KeyCode.D))
            {
                RB.linearVelocityX = speed;
                sprite.flipX = false;
            }
            if (Input.GetKey(KeyCode.A))
            {
                RB.linearVelocityX = -speed;
                sprite.flipX = true;
            }
            if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
            {
                RB.linearVelocityX = 0f;
                anim.SetBool("isRUNNING", false);
            }
        }

        if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && isGrounded)
            anim.SetBool("isRUNNING", true);
        else
            anim.SetBool("isRUNNING", false);
    }

    private void HandleJumping()
    {
        if (!dashing)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                jumpTimer += Time.deltaTime;
                if (jumpTimer <= 0.4f)
                {
                    if (isGrounded && Input.GetKeyDown(KeyCode.Space))
                        jumping = true;

                    if (jumping){
                        RB.linearVelocityY = jump;

                    if (jump<16)
                        jump += jumpPower * Time.deltaTime;
                    }
                }
                
                else
                {
                    jump = 0;
                    jumping = false;
                }
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
        if (Input.GetKeyDown(KeyCode.E) && attacktimer <= 0)
        {
            if (sprite.flipX)
            {
                Attack_Transform.localPosition = new Vector3(-0.3f, 0.1f, 0);
                Attack_Sprite.flipX = true;
            }
            else
            {
                Attack_Transform.localPosition = new Vector3(0.3f, 0.1f, 0);
                Attack_Sprite.flipX = false;
            }
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

    void GroundCheck()
    {
        if (isGrounded && !wasGrounded)
        {
            jump = 0;
            jumpTimer = 0;
        }
        if (isGrounded && !dashing)
            dash = true;

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

        wasGrounded = isGrounded;
    }
}
