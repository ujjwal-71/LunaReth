using UnityEngine;

public enum MovementStates
{
    Idle,
    Walking,
    Jumping,
    Falling,
    WallStuck // New State!
}

public class Movement : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D playerBody;
    private Animator playerAnim;
    public Transform legs;
    public LayerMask ground;
    public LayerMask wall;

    [Header("Movement Stats")]
    public float walkSpeed = 8f;
    public float jumpForce = 18f;
    
    [Header("Gravity & Physics")]
    public float normalGravity = 6f;
    public float fallGravity = 8f;
    public float terminalVelocity = -50f;
    
    [Header("Timers")]
    public float coyoteTimeMax = 0.15f;
    public float jumpBufferMax = 0.15f;

    [Header("Wall Stuck Stats")]
    public float wallStuckDuration = 3f;
    public Vector2 wallJumpForce = new Vector2(10f, 15f);
    private float currentWallStuckTime;
    private bool hasUsedWallStuck = false;

    private MovementStates currentMovementState;
    private float currentCoyoteTime;
    private float currentJumpBuffer;
    private float moveHorizontal;
    private int playerDirection = 1;
    private Vector2 playerVelocity;
    private bool isWall;
    private bool isGrounded;

    private void Start()
    {
        playerAnim = GetComponent<Animator>();
        playerBody = GetComponent<Rigidbody2D>();
        currentMovementState = MovementStates.Idle;
    }

    private void resetAnimation()
    {
        playerAnim.SetBool("isRUNNING", false);
        playerAnim.SetBool("isJUMPING", false);
        playerAnim.SetBool("isFALLING", false);
        playerAnim.SetBool("isWALLSTUCK", false);
    }

    private void Update()
    {
        isGrounded = Physics2D.OverlapCircle(legs.position, 0.5f, ground);
        
        // Fix for isWall: Cast slightly forward in the direction the player is facing!
        Vector2 wallCheckPos = (Vector2)legs.position + new Vector2(playerDirection * 0.5f, 0);
        isWall = Physics2D.OverlapCircle(wallCheckPos, 0.3f, wall);

        if (isGrounded)
        {
            currentCoyoteTime = coyoteTimeMax;
            hasUsedWallStuck = false; // Reset wall stuck on ground!
        }
        else
            currentCoyoteTime -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
            currentJumpBuffer = jumpBufferMax;
        else
            currentJumpBuffer -= Time.deltaTime; 

        // Don't process normal horizontal input if we are wall stuck
        if (currentMovementState != MovementStates.WallStuck)
        {
            moveHorizontal = Input.GetAxis("Horizontal");
            if (Mathf.Abs(moveHorizontal) > 0.2f)
            {
                playerDirection = moveHorizontal < 0 ? -1 : 1;
                transform.localScale = new Vector3(playerDirection, 1, 1);
            }
        }

        switch (currentMovementState)
        {
            case MovementStates.Idle:
                IdleState();
                break;
            case MovementStates.Walking:
                WalkingState();
                break;
            case MovementStates.Jumping:
                JumpingState();
                break;
            case MovementStates.Falling:
                FallingState();
                break;
            case MovementStates.WallStuck:
                WallStuckState();
                break;
        }

        // Jump Execution (Checks for both Ground Jump and Wall Jump)
        if (currentJumpBuffer > 0f)
        {
            if (currentCoyoteTime > 0f)
            {
                ExecuteJump(false); // Normal Jump
            }
            else if (currentMovementState == MovementStates.WallStuck)
            {
                ExecuteJump(true); // Wall Jump
            }
        }
    }

    private void FixedUpdate()
    {
        // Don't clamp Y velocity or apply X velocity if we are stuck to a wall
        if (currentMovementState == MovementStates.WallStuck) return;

        float currentY = playerBody.linearVelocity.y;
        if (currentY < terminalVelocity)
            currentY = terminalVelocity;
            
        playerBody.linearVelocity = new Vector2(playerVelocity.x, currentY);
    }

    private void ExecuteJump(bool isWallJump)
    {
        currentJumpBuffer = 0f;
        currentCoyoteTime = 0f;

        if (isWallJump)
        {
            // Push away from the wall and up!
            playerBody.linearVelocity = new Vector2(-playerDirection * wallJumpForce.x, wallJumpForce.y);
            
            // Flip the player immediately so they face away from the wall
            playerDirection *= -1;
            transform.localScale = new Vector3(playerDirection, 1, 1);
            moveHorizontal = playerDirection;
            playerVelocity.x = playerDirection * walkSpeed;
        }
        else
        {
            // Normal upward jump
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, jumpForce);
        }
        
        playerBody.gravityScale = normalGravity;
        currentMovementState = MovementStates.Jumping;
    }

    private void IdleState()
    {
        if (Mathf.Abs(moveHorizontal) > 0.2f)
        {
            currentMovementState = MovementStates.Walking;
            WalkingState(); 
            return;
        }
        else if (playerBody.linearVelocity.y < 0 && !isGrounded)
        {
            currentMovementState = MovementStates.Falling;
            FallingState();
            return;
        }

        playerVelocity.x = 0;
        resetAnimation();
        playerBody.gravityScale = normalGravity;
    }

    private void WalkingState()
    {
        if (Mathf.Abs(moveHorizontal) < 0.2f)
        {
            currentMovementState = MovementStates.Idle;
            IdleState();
            return;
        }
        else if (playerBody.linearVelocity.y < 0 && !isGrounded)
        {
            currentMovementState = MovementStates.Falling;
            FallingState();
            return;
        }

        playerVelocity.x = moveHorizontal * walkSpeed;
        resetAnimation();
        playerAnim.SetBool("isRUNNING", true);
        playerBody.gravityScale = normalGravity;
    }

    private void JumpingState()
    {
        // Can stick to wall while jumping up if they hit a wall
        if (isWall && !isGrounded && !hasUsedWallStuck)
        {
            currentMovementState = MovementStates.WallStuck;
            currentWallStuckTime = wallStuckDuration;
            hasUsedWallStuck = true;
            return;
        }

        if (playerBody.linearVelocity.y <= 0)
        {
            currentMovementState = MovementStates.Falling;
            FallingState();
            return;
        }

        playerVelocity.x = moveHorizontal * walkSpeed;
        resetAnimation();
        playerAnim.SetBool("isJUMPING", true);
        playerBody.gravityScale = normalGravity;

        if (Input.GetButtonUp("Jump") && playerBody.linearVelocity.y > 0)
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, playerBody.linearVelocity.y * 0.5f);
            playerBody.gravityScale = fallGravity;
        }
    }

    private void FallingState()
    {
        if (isGrounded)
        {
            currentMovementState = MovementStates.Idle;
            IdleState();
            return;
        }

        // Stick to wall while falling
        if (isWall && !hasUsedWallStuck)
        {
            currentMovementState = MovementStates.WallStuck;
            currentWallStuckTime = wallStuckDuration;
            hasUsedWallStuck = true;
            return;
        }

        playerVelocity.x = moveHorizontal * (walkSpeed * 1.2f);
        resetAnimation();
        playerAnim.SetBool("isFALLING", true);
        playerBody.gravityScale = fallGravity;
    }

    private void WallStuckState()
    {
        // Physics lock
        playerVelocity.x = 0;
        playerBody.linearVelocity = Vector2.zero;
        playerBody.gravityScale = 0f;
        
        resetAnimation();
        playerAnim.SetBool("isWALLSTUCK", true);
        
        currentWallStuckTime -= Time.deltaTime;
        
        // Break out if time runs out, wall disappears, or they touch ground
        if (currentWallStuckTime <= 0 || !isWall || isGrounded)
        {
            currentMovementState = MovementStates.Falling;
            playerBody.gravityScale = fallGravity;
            playerAnim.SetBool("isWALLSTUCK", false);
        }
    }
}