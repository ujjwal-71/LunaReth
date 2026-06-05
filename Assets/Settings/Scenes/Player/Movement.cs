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
    // Update is called once per frame
    void Update()
    {
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

        if (Input.GetKeyDown(KeyCode.X))
        {
            RB.AddForce(Vector2.up * 20, ForceMode2D.Impulse);
        }

    }

    private bool isGrounded = true;
    void OnTriggerEnter2D()
    {
        isGrounded=true;
        jump = 1;
    }
}
