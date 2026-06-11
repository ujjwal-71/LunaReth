using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class Movement : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Rigidbody2D RB;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RB = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public float speed=4f;
    public float jump=1f;
    private int direc;
    private bool dashing;
    private float dashTimer;
    // Update is called once per frame
    void Update()
    {
        if(dash){
            if(sprite.flipX)
                direc= -1;
            else
                direc= 1;

            if (Input.GetKeyDown(KeyCode.X))
            {
                dashing = true;
                Debug.Log("DASHED");
                RB.linearVelocityX = direc*10;
                dash = false;
            }
            if(dashing){
                dashTimer += Time.fixedDeltaTime;
            }
            
        }
        if(dashTimer> 3f){
                dashing = false;
                dashTimer = 0;
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
                RB.linearVelocityX = 0f;


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

    private bool isGrounded = true;
    private bool dash = true;
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
