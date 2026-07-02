using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Movement : MonoBehaviour
{
    private SpriteRenderer sprite;
    public GameObject attacker;
    private ParticleSystem DashEff;
    private Rigidbody2D RB;
    public Animator anim;
    public SpriteRenderer Attack_Sprite;
    public Transform Attack_Transform;

    void Start()
    {
        attacker.SetActive(false);
        RB = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        DashEff = GetComponent<ParticleSystem>();
    }

    public float speed=4f;
    public float jump=1f;
    private bool isGrounded = true;
    private int direc;
    private bool dashing;
    private bool dash;
    private float dashTimer;
    private float attacktimer = 0;
    void Update()
    {
        if(dash && Input.GetKeyDown(KeyCode.X))
        {
            dashing = true;
            if(sprite.flipX)
                direc= -1;
            else
                direc= 1;
        }
        if (dashing)
        {
            if(dashTimer> 0.1f)
            {
                RB.linearVelocity = new Vector2 (0,0);
                dashing = false;
                dashTimer = 0;
                DashEff.Stop();
            }
            else
            {
                DashEff.Play();
                dash = false;
                dashTimer += Time.deltaTime;
                RB.linearVelocity = new Vector2 (direc*50,0);
            }
        }
        

        if(!dashing){
            if(Input.GetKey(KeyCode.D)){
                RB.linearVelocityX = speed;
                sprite.flipX = false;
            }
            if (Input.GetKey(KeyCode.A)){
                RB.linearVelocityX = -speed;
                sprite.flipX = true;
            }
            if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
            {
                RB.linearVelocityX = 0f;
                anim.SetBool("isRunning", false);
            }

            if(Input.GetKey(KeyCode.Space)){
                if(jump<16f && isGrounded){
                    RB.linearVelocityY = jump;
                    jump+=0.5f;
                }
                else{
                    anim.SetBool("isJUMPED", false);
                    isGrounded = false;
                    jump= 1;
                }
            }
            if (Input.GetKeyUp(KeyCode.Space))
                jump = 17;
        }
        if((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && isGrounded)
            anim.SetBool("isRunning", true);
        else
            anim.SetBool("isRunning", false);

        if (Input.GetKeyDown(KeyCode.E) && attacktimer <= 0 )
        {
            if (sprite.flipX)
            {
                Attack_Transform.localPosition = new Vector3(-0.3f,0.1f,0);
                Attack_Sprite.flipX = true;
            }
            else
            {
                Attack_Transform.localPosition = new Vector3(0.3f,0.1f,0);
                Attack_Sprite.flipX = false;
            }
            attacker.SetActive(true);
            attacktimer =0.25f;
        }
        if (attacktimer > 0)
        {
            attacktimer -= Time.deltaTime;
            if (attacktimer <= 0) {
                attacker.SetActive(false);
        }
        }


    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        isGrounded = true;
        dash = true;
        jump = 1;
        anim.SetBool("isJUMPED", false);
        anim.SetBool("isGROUNDED", true);
    }
    void OnTriggerStay2D(Collider2D collider)
    {
        dash = true;
        isGrounded = true;
        anim.SetBool("isGROUNDED", true);
    }

    void OnTriggerExit2D(Collider2D collider)
    {
        anim.SetBool("isJUMPED", true);
    }
}
