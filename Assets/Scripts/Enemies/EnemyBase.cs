using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField]
    public int MaxStun;
    public int MaxHealth;
    public Transform groundRay;
    public Transform frontRay;
    public Transform playerTransform;
    public float detectionRange;
    public float attackRange;
    public LayerMask ground;
    public LayerMask wall;
    public LayerMask player;
    public float walkSpeed;
    public float chaseSpeed;
    public bool isGuarded;
    private int currStun;
    private int currHealth;
    private float attackCoolDown;
    private State currentState;
    private Animator animations;
    private Rigidbody2D rb;
    private Transform tb;
    
    private Vector2 targetVelocity;
    private float waitTimer;
    private enum State {idle, patrol, chase, stun, attack, dead }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        tb = GetComponent<Transform>();
        animations = GetComponent<Animator>();
        currHealth = MaxHealth;
        currStun = MaxStun;
        currentState = State.patrol;
    }


    void Update()
    {
        targetVelocity.y = rb.linearVelocity.y;
        
        if(currHealth <= 0)
        {
            animReset();
            currentState = State.dead;
        }
        else if(currStun <= 0)
        {
            animReset();
            currStun = 1;
            waitTimer = 5;
            currentState = State.stun;
        }


        switch (currentState)
        {
            case State.idle:
                idle();
                break;
            case State.patrol:
                patrol();
                break;
            
            case State.chase:
                chase();
                break;

            case State.stun:
                stunned();
                break;

            case State.attack:
                attack();
                break;

            case State.dead:
                animations.SetBool("isDead", true);
                return;
        }
    }

    void FixedUpdate()
    {
        Debug.Log(currentState);
        rb.linearVelocity = targetVelocity;
    }

    private bool isGrounded()
    {
        return Physics2D.Raycast(groundRay.position,Vector2.down,2.5f, ground);
    }

    private bool isPathBlocked()
    {
        return Physics2D.Raycast(groundRay.position,Vector2.right * Mathf.Sign(tb.localScale.x),1.5f, wall);
    }

    private bool isItPlayer()
    {
        return Physics2D.Raycast(frontRay.position,Vector2.right * Mathf.Sign(tb.localScale.x), detectionRange, player);
    }

    private void animReset()
    {
        animations.SetBool("isStunned",false);
        animations.SetBool("isDead", false);
        animations.SetBool("isAttacking_1",false);
    }

    private void idle()
    {
        targetVelocity.x = 0;
        waitTimer -= Time.deltaTime;
        if(waitTimer <= 0)
        {
            tb.localScale = new Vector3(tb.localScale.x * -1, tb.localScale.y, 1);
            currentState = State.patrol;
        }
    }

    private void patrol()
    {
        if (isItPlayer())
        {
            currentState = State.chase;
            return;
        }
        if(!isGrounded() || isPathBlocked())
        {
            waitTimer = 3;
            rb.linearVelocityX = 0;
            currentState = State.idle;
            return;
        }
        targetVelocity.x = walkSpeed * Mathf.Sign(tb.localScale.x);
    }

    private void chase()
    {
        float directionToPlayer = playerTransform.position.x - transform.position.x;
        if (Mathf.Abs(directionToPlayer) > 3f)
        {
            tb.localScale = new Vector3(Mathf.Sign(directionToPlayer), tb.localScale.y, 1);
        }

        if(isGrounded())
            targetVelocity.x = chaseSpeed * Mathf.Sign(tb.localScale.x);
        else
            targetVelocity.x = 0;

        // State Changing
        if(Vector2.Distance(transform.position, playerTransform.position) < attackRange)
        {
            attackCoolDown = 0.1f;
            animReset();
            currentState = State.attack;
        }
        if (Vector2.Distance(transform.position, playerTransform.position) > detectionRange)
        {
            currentState = State.patrol;
        }
    }

    private void attack()
    {
        targetVelocity.x = 0;
        float directionToPlayer = playerTransform.position.x - transform.position.x;
        tb.localScale = new Vector3(Mathf.Sign(directionToPlayer),1,1);
        attackCoolDown -= Time.deltaTime;
        if (attackCoolDown < 0)
        {
            animations.SetBool("isAttacking_1", true);
            attackCoolDown = 2;
        }
        else
            animReset();

        // State Changing
        if (Vector2.Distance(transform.position, playerTransform.position) > attackRange)
        {
            currentState = State.chase;
        }
    }

    public void stunBar(int stunAmount)
    {
        currStun -= stunAmount;
    }
    public void healthBar(int amount)
    {
        currHealth -= amount;
    }

    private void stunned(bool actualStunned = true)
    {
        targetVelocity.x = 0;
        if (actualStunned)
            animations.SetBool("isStunned",true);
        waitTimer -= Time.deltaTime;
        
        if(waitTimer < 0)
        {
            currStun = MaxStun;
            animReset();
            currentState = State.chase;
        }
    }
    public void ApplyKnockback(float pushForceX, float pushForceY, float stunDuration)
    {
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(new Vector2(pushForceX, pushForceY), ForceMode2D.Impulse);
        waitTimer = 0.3f;
        stunned(false); 
    }
}
