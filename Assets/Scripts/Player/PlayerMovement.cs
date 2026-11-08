using UnityEngine;

public enum MovementStates
{
    Idle,
    Walking,
    Jumping,
    Falling,
    WallStuck
}

public class Movement : MonoBehaviour
{
    [Header("Collision Layers & Detection")]
    public Transform legs;
    public LayerMask ground;
    public LayerMask wall;

    [Header("Locomotion")]
    public float walkSpeed = 8.5f;

    [Header("Jump Settings")]
    [Tooltip("Upward speed applied while jumping.")]
    public float jumpForce = 16f;
    [Tooltip("Maximum duration (in seconds) the player can hold jump to sustain upward lift.")]
    public float jumpHoldDuration = 0.2f;
    [Tooltip("Normal gravity scale while ascending with jump held.")]
    public float defaultGravity = 5.5f;
    [Tooltip("Heavy gravity scale applied when falling or when jump button is released.")]
    public float fallGravity = 8.0f;
    [Tooltip("Grace period (in seconds) allowing jump input just after stepping off a platform ledge.")]
    public float coyoteTime = 0.15f;
    [Tooltip("Input buffer window (in seconds) to queue jump input slightly before touching the ground.")]
    public float jumpBufferTime = 0.15f;

    [Header("Wall Cling & Wall Jump (Silksong)")]
    [Tooltip("How long (in seconds) the player stays stuck on the wall before falling off.")]
    public float wallStuckDuration = 3f;
    [Tooltip("Cooldown delay (in seconds) after falling off a wall before the player can stick to a wall again. Resets immediately upon landing on ground.")]
    public float wallReStickCooldown = 0.6f;
    public Vector2 wallJumpForce = new Vector2(12f, 16f);

    // ==========================================
    // Internal Constants
    // ==========================================
    private const float wallJumpLockoutDuration = 0.14f;
    private const float terminalVelocity = -30f;

    private MovementStates currentMovementState = MovementStates.Idle;
    private float currentCoyoteTime;
    private float currentJumpBuffer;
    private float moveHorizontal;
    private int playerDirection = 1;
    private bool isWall;
    private bool isGrounded;
    private float wallJumpLockoutTime;
    private Vector2 wallNormal = Vector2.zero;

    // Jump holding state
    private bool isHoldingJump = false;
    private float currentJumpHoldTimer = 0f;

    // Wall stick timer (Silksong)
    private float wallStuckTimer = 0f;
    private bool wasWallStuck = false;
    private float nextWallStickTime = 0f;

    private Rigidbody2D playerBody;
    private Animator playerAnim;
    private Collider2D playerCol;
    private MasterState masterState;
    private int animStateHash = -1;

    private void Start()
    {
        playerAnim = GetComponent<Animator>();
        playerBody = GetComponent<Rigidbody2D>();
        playerCol = GetComponent<Collider2D>();
        masterState = GetComponent<MasterState>();
        currentMovementState = MovementStates.Idle;
    }

    private void Update()
    {
        CheckSurroundings();
        UpdateTimers();
        HandleJumpInput();
        HandleHorizontalInput();
        ResolveMovementState();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        // When MasterState owns physics (Dashing, KnockedBack, Stunned, Dead), don't interfere
        if (masterState != null && masterState.CurrentState != MasterStateType.Free)
            return;

        CheckCornerCorrection();
        ApplyPhysics();
    }

    private void CheckSurroundings()
    {
        // Ground Check
        Vector2 groundOrigin = legs != null ? (Vector2)legs.position : (Vector2)transform.position;
        isGrounded = Physics2D.OverlapCircle(groundOrigin, 0.4f, ground);

        // Wall Check from body center
        Vector2 bodyCenter = playerCol != null ? (Vector2)playerCol.bounds.center : (Vector2)transform.position + Vector2.up * 0.8f;
        float checkDist = playerCol != null ? (playerCol.bounds.extents.x + 0.18f) : 0.55f;

        RaycastHit2D hit = Physics2D.Raycast(bodyCenter, Vector2.right * playerDirection, checkDist, wall);
        if (hit.collider != null)
        {
            isWall = true;
            wallNormal = hit.normal;
        }
        else
        {
            isWall = false;
            wallNormal = Vector2.zero;
        }
    }

    private void UpdateTimers()
    {
        if (isGrounded)
        {
            currentCoyoteTime = coyoteTime;
            isHoldingJump = false;
        }
        else
        {
            currentCoyoteTime -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
            currentJumpBuffer = jumpBufferTime;
        else
            currentJumpBuffer -= Time.deltaTime;
    }

    /// <summary>
    /// Hold jump: sustained upward force for jumpHoldDuration.
    /// Release jump: immediately cuts Y force to 0 and snaps gravity to heavy.
    /// </summary>
    private void HandleJumpInput()
    {
        // During non-Free master states, block jump but still allow wall jump (wall jump sets its own state)
        if (masterState != null && masterState.CurrentState != MasterStateType.Free)
        {
            isHoldingJump = false;
            return;
        }

        // Also block if mid-attack on ground (but allow air attacks + jump)
        bool isActing = masterState != null && !masterState.CanMove;

        // 1. Initiate Jump
        if (currentJumpBuffer > 0f)
        {
            if (currentMovementState == MovementStates.WallStuck)
            {
                ExecuteWallJump();
            }
            else if (currentCoyoteTime > 0f && !isActing)
            {
                ExecuteGroundJump();
            }
        }

        // 2. While holding Jump, sustain upward force until timer expires
        if (Input.GetButton("Jump") && isHoldingJump)
        {
            currentJumpHoldTimer -= Time.deltaTime;
            if (currentJumpHoldTimer > 0f)
            {
                playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, jumpForce);
            }
            else
            {
                isHoldingJump = false;
            }
        }

        // 3. When player releases jump button, immediately cut Y velocity to 0
        if (Input.GetButtonUp("Jump"))
        {
            isHoldingJump = false;
            if (playerBody.linearVelocity.y > 0f)
            {
                playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 0f);
            }
        }
    }

    private void ExecuteGroundJump()
    {
        currentJumpBuffer = 0f;
        currentCoyoteTime = 0f;
        isHoldingJump = true;
        currentJumpHoldTimer = jumpHoldDuration;

        playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, jumpForce);
        currentMovementState = MovementStates.Jumping;
    }

    private void ExecuteWallJump()
    {
        currentJumpBuffer = 0f;
        currentCoyoteTime = 0f;
        isHoldingJump = true;
        currentJumpHoldTimer = jumpHoldDuration * 0.75f;

        // Launch away from wall (Hollow Knight outward kick)
        float launchDirX = wallNormal.x != 0f ? Mathf.Sign(wallNormal.x) : -playerDirection;
        playerBody.linearVelocity = new Vector2(launchDirX * wallJumpForce.x, wallJumpForce.y);

        playerDirection = (int)launchDirX;
        transform.localScale = new Vector3(playerDirection, 1, 1);
        moveHorizontal = playerDirection;

        // Reset wall stick timer so player can re-stick after falling back
        wallStuckTimer = 0f;
        wasWallStuck = false;
        nextWallStickTime = Time.time + wallJumpLockoutDuration;

        wallJumpLockoutTime = Time.time + wallJumpLockoutDuration;
        currentMovementState = MovementStates.Jumping;
    }

    private void HandleHorizontalInput()
    {
        // During wall stuck, ignore horizontal input (player is frozen on wall)
        if (currentMovementState == MovementStates.WallStuck)
        {
            moveHorizontal = 0f;
            return;
        }

        if (Time.time >= wallJumpLockoutTime)
        {
            moveHorizontal = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(moveHorizontal) > 0.05f)
            {
                playerDirection = moveHorizontal < 0 ? -1 : 1;
                transform.localScale = new Vector3(playerDirection, 1, 1);
            }
        }
    }

    private void ResolveMovementState()
    {
        // If master state is not Free, don't change movement state
        if (masterState != null && masterState.CurrentState != MasterStateType.Free)
            return;

        // 1. Wall Stick Check (Silksong style: freeze on wall, timer-based detach)
        // Must respect nextWallStickTime cooldown so falling off doesn't instantly re-stick
        if (isWall && !isGrounded && playerBody.linearVelocity.y <= 0.5f && Time.time >= nextWallStickTime)
        {
            if (currentMovementState != MovementStates.WallStuck)
            {
                // Entering wall stick
                if (!wasWallStuck)
                {
                    wallStuckTimer = wallStuckDuration;
                    wasWallStuck = true;
                }

                // Face toward the wall
                if (wallNormal.x != 0f)
                {
                    playerDirection = wallNormal.x > 0f ? -1 : 1;
                    transform.localScale = new Vector3(playerDirection, 1, 1);
                }
            }

            // Count down the stuck timer
            wallStuckTimer -= Time.deltaTime;

            if (wallStuckTimer <= 0f)
            {
                // Timer expired: trigger re-stick cooldown so player falls smoothly
                wasWallStuck = false;
                playerBody.linearVelocity = new Vector2(-1 * playerDirection * 10 , 0);
                nextWallStickTime = Time.time + wallReStickCooldown;
                currentMovementState = MovementStates.Falling;
                return;
            }

            currentMovementState = MovementStates.WallStuck;
            return;
        }

        // Reset wall stick tracking and cooldown when touching the ground
        if (isGrounded)
        {
            wasWallStuck = false;
            nextWallStickTime = 0f;
        }
        else if (!isWall)
        {
            wasWallStuck = false;
        }

        // 2. Grounded
        if (isGrounded)
        {
            currentMovementState = Mathf.Abs(moveHorizontal) > 0.05f ? MovementStates.Walking : MovementStates.Idle;
            return;
        }

        // 3. In Air
        if (playerBody.linearVelocity.y > 0.05f)
            currentMovementState = MovementStates.Jumping;
        else
            currentMovementState = MovementStates.Falling;
    }

    private void ApplyPhysics()
    {
        Vector2 vel = playerBody.linearVelocity;

        // Wall Stuck (Silksong): Completely frozen on wall, no sliding
        if (currentMovementState == MovementStates.WallStuck)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.gravityScale = 0f;
            return;
        }

        // When mid-action: on ground, stop horizontal slide; in air, preserve momentum for fluid Sekiro air slashes
        bool isActing = masterState != null && !masterState.CanMove;

        float targetSpeedX;
        if (isActing && isGrounded)
        {
            targetSpeedX = 0f;
        }
        else if (isActing && !isGrounded)
        {
            targetSpeedX = vel.x;
        }
        else if (Time.time >= wallJumpLockoutTime)
        {
            targetSpeedX = moveHorizontal * walkSpeed;
        }
        else
        {
            targetSpeedX = vel.x;
        }

        vel.x = isGrounded ? targetSpeedX : Mathf.MoveTowards(vel.x, targetSpeedX, 120f * Time.fixedDeltaTime);
        vel.y = Mathf.Max(vel.y, terminalVelocity);
        playerBody.linearVelocity = vel;

        // Gravity rules:
        // Default gravity (5.5) while ascending with jump held.
        // Heavy gravity (8.0) when falling or when jump button is released.
        if (isHoldingJump && vel.y > 0f)
        {
            playerBody.gravityScale = defaultGravity;
        }
        else
        {
            playerBody.gravityScale = fallGravity;
        }
    }

    private void UpdateAnimations()
    {
        if (playerAnim == null) return;

        // Yield to MasterState (Dashing, Knockback, Stun, Death) or ActionState (Attacks, Guard)
        bool isLockedOrActing = (masterState != null && masterState.CurrentState != MasterStateType.Free)
                             || (masterState != null && !masterState.CanMove);

        if (isLockedOrActing)
        {
            animStateHash = -1;
            return;
        }

        int newHash = (int)currentMovementState;
        if (newHash == animStateHash) return;
        animStateHash = newHash;

        playerAnim.SetBool("isRUNNING", currentMovementState == MovementStates.Walking);
        playerAnim.SetBool("isJUMPING", currentMovementState == MovementStates.Jumping);
        playerAnim.SetBool("isFALLING", currentMovementState == MovementStates.Falling);
        playerAnim.SetBool("isWALLSTUCK", currentMovementState == MovementStates.WallStuck);
    }

    private void CheckCornerCorrection()
    {
        if (playerCol == null || currentMovementState == MovementStates.WallStuck) return;

        Bounds b = playerCol.bounds;

        // Head corner nudge
        if (playerBody.linearVelocity.y > 0.1f)
        {
            Vector2 center = new Vector2(b.center.x, b.max.y);
            RaycastHit2D centerHit = Physics2D.Raycast(center, Vector2.up, 0.3f, ground);

            if (centerHit.collider == null)
            {
                RaycastHit2D leftHit = Physics2D.Raycast(new Vector2(b.min.x + 0.05f, b.max.y), Vector2.up, 0.3f, ground);
                RaycastHit2D rightHit = Physics2D.Raycast(new Vector2(b.max.x - 0.05f, b.max.y), Vector2.up, 0.3f, ground);

                if (leftHit.collider != null && rightHit.collider == null)
                    transform.position += Vector3.right * 0.05f;
                else if (rightHit.collider != null && leftHit.collider == null)
                    transform.position += Vector3.left * 0.05f;
            }
        }
        // Foot step-up nudge
        else if (playerBody.linearVelocity.y <= 0f && !isGrounded && Mathf.Abs(moveHorizontal) > 0.05f)
        {
            Vector2 forwardDir = new Vector2(playerDirection, 0f);
            RaycastHit2D footHit = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), forwardDir, b.extents.x + 0.2f, ground);
            RaycastHit2D hipHit = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.35f), forwardDir, b.extents.x + 0.2f, ground);

            if (footHit.collider != null && hipHit.collider == null)
            {
                transform.position += Vector3.up * 0.05f;
                playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 0f);
            }
        }
    }
}
