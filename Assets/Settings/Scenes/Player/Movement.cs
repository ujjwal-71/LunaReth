using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Movement : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Rigidbody2D RB;
    public Animator anim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RB = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public float speed=4f;
    public float jump=1f;
    private bool isGrounded = true;
    private int direc;
    private bool dashing;
    private bool dash;
    private float dashTimer;
    // Update is called once per frame
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
            if(dashTimer> 0.2f)
            {
                RB.linearVelocity = new Vector2 (0,0);
                dashing = false;
                dashTimer = 0;
            }
            else
            {
                dash = false;
                dashTimer += Time.deltaTime;
                RB.linearVelocity = new Vector2 (direc*20,0);
            }
        }
        

        if(!dashing){
            if(Input.GetKey(KeyCode.D)){
                RB.linearVelocityX = speed;
                sprite.flipX = false;
                anim.SetBool("isRunning", true);
            }
            if (Input.GetKey(KeyCode.A)){
                RB.linearVelocityX = -speed;
                sprite.flipX = true;
                anim.SetBool("isRunning", true);
            }
            if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
            {
                RB.linearVelocityX = 0f;
                anim.SetBool("isRunning", false);
                Debug.Log(anim.GetBool("isRunning"));
            }

            if(Input.GetKey(KeyCode.Space)){
                if(jump<16f && isGrounded){
                    RB.linearVelocityY = jump;
                    jump+=0.5f;
                }
                else{
                    isGrounded = false;
                    jump= 1;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        isGrounded=true;
        dash = true;
        jump = 1;
    }
    void OnTriggerStay2D(Collider2D collider)
    {
        dash = true;
    }
}
