using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Serialize]
    public int MaxStun;
    public Transform ray;
    public int MaxHealth;
    private int currStun;
    private int currHealth;
    private State currentState;
    private Animator animations;
    private Rigidbody2D rb;
    private Transform tb;
    private bool move;

    private enum State
    {
        patrol, chase, stun, attack, dead
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        tb = GetComponent<Transform>();
        animations = GetComponent<Animator>();
        currentState = State.patrol;
    }


    void Update()
    {
        switch (currentState)
        {
            case State.patrol:
                patrol();
                break;
            
            case State.chase:
                break;

            case State.stun:
                break;

            case State.attack:
                break;

            case State.dead:
                break;
        }
    }

    void FixedUpdate()
    {
    }

    private bool isGrounded()
    {
        return Physics2D.Raycast(ray.position,Vector2.down,0.5f, 3);
    }

    private bool isPathBlocked()
    {
        return Physics2D.Raycast(ray.position,Vector2.right,1.5f, 9);
    }

    private void animReset()
    {
        animations.SetBool("isAttacking_1",false);
    }

    private IEnumerator idleStun(float duration,bool rotate = false)
    {
        if (rotate)
        {
            tb.localScale = new Vector3(tb.localScale.x * -1, tb.localScale.y, 0);
            yield return new WaitForSecondsRealtime(duration);
        }
        else
        {
            
            yield return new WaitForSecondsRealtime(duration);
        }
    }

    private void patrol()
    {
        if (!isGrounded() || isPathBlocked())
        {
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(idleStun(5, true));
        }
        if (currStun <= 0)
            currentState = State.stun;
        else if(currHealth <= 0)
            currentState = State.dead;
    }

    public void stunBar(int stunAmount)
    {
        
    }
}
