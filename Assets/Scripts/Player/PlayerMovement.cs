using UnityEngine;

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
    private bool dash;
    private float dashTimer;
    private float dashPauseTimer;
    private bool dashPause;
    private float shadowDashtimer = 0;
    private float dashSpeed;


    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        DashEff = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(FeetPosition.position,0.25f,Ground);
        moveHorizontal = Input.GetAxis("Horizontal");
        GroundCheck();
        HandleDashing();
        if (!dashPause)
        {
            AnimHAndle();
            HandleMovement();
            HandleJumping();
        }
    }

    public void InterruptAction()
    {
        jumping = false;
        jump = 0;
        jumpTimer = 0;

        dashing = false;
        dashTimer = 0;

        anim.SetBool("isRUNNING",false);
        anim.SetBool("isGROUNDED",true);
        anim.SetBool("isJUMPED",false);
        anim.SetBool("isFALLING",false);
    }


    private void HandleDashing()
    {
        var trailcol = DashEff.trails;
        shadowDashtimer -= Time.deltaTime;

        if (dash && Input.GetButtonDown("Dash"))
        {
            dashing = true;
            if (shadowDashtimer < 0)
            {
                trailcol.colorOverTrail= new ParticleSystem.MinMaxGradient(Color.black);
                dashSpeed = 100;
                gameObject.layer = LayerMask.NameToLayer("Ghost");
            }
            else
            {
                trailcol.colorOverTrail= new ParticleSystem.MinMaxGradient(Color.white);
                dashSpeed = 80;
            }
        }
        
        if (dashing)
        {
            if (dashTimer > 0.1f)
            {
                if (dashSpeed > 80)
                    shadowDashtimer = 1.5f;
                DashEff.Stop();
                dashTimer = 0;
                dashing = false;
                dashPauseTimer = 0.5f;
                RB.linearVelocity = new Vector2(0,0);
                gameObject.layer = LayerMask.NameToLayer("Player");
                InterruptAction();
            }
            else
            {
                dash = false;
                dashTimer += Time.deltaTime;
                RB.linearVelocity = transform.right * dashSpeed;
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
