using UnityEngine;

public class ActionState : MonoBehaviour
{
    [Header("Components")]
    private Animator anim;
    private Movement playerMovement;
    private _attributes attributes; // Ensure you have _attributes attached

    [Header("Combo Settings")]
    public float comboWindow = 0.6f; 
    private float lastAttackTime;
    private int comboStep = 0; 

    [Header("Charge Settings")]
    public float chargeTimeRequired = 0.5f; 
    private float currentChargeTime = 0f;
    private bool isCharging = false;

    [Header("Combat Art System")]
    public AnimatorOverrideController overrideController;
    public AnimationClip equippedCombatArt; 
    public int combatArtSpiritCost = 5; 

    [Header("Flying Raijin (Minato Teleport)")]
    public GameObject kunaiPrefab;
    [Tooltip("Optional throw point on the character's hand. If empty, a point near the upper body is used.")]
    public Transform kunaiThrowPoint;
    [Tooltip("Layers that can block the kunai at the instant it is thrown.")]
    public LayerMask kunaiBlockingLayers;
    [Min(0f)] public float kunaiWallClearance = 0.08f;
    [Tooltip("Adds a small upward arc to the kunai's initial throw velocity.")]
    public float kunaiThrowArc = 2f;
    private GameObject activeKunai;
    public float aoeKnockbackRadius = 3f;
    public int aoeStunDamage = 10;
    public float selfTeleportKnockback = 10f;

    private void Start()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<Movement>();
        attributes = GetComponent<_attributes>();
        
        if (anim.runtimeAnimatorController != null)
        {
            overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
            anim.runtimeAnimatorController = overrideController;
        }
    }

    private void Update()
    {
        HandleGuard();
        HandleAttackAndCharge();
        HandleCombatArt();
        HandleFlyingRaijin(); // <--- NEW!
        
        ResetComboIfWindowPassed(); 

        ManageMovementState();
    }

    private void HandleFlyingRaijin()
    {
        // "X" button triggers the throw/teleport
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (activeKunai == null)
            {
                // 1. THROW KUNAI
                if (kunaiPrefab != null)
                {
                    float direction = transform.localScale.x > 0 ? 1f : -1f;
                    Vector2 origin = kunaiThrowPoint != null
                        ? (Vector2)kunaiThrowPoint.position
                        : (Vector2)transform.position + new Vector2(0f, 0.8f);
                    Vector2 forward = new Vector2(direction, 0f);
                    float clearance = kunaiWallClearance;

                    // Start inside the character's near side, then move the spawn point
                    // outward only as far as the first wall allows. This avoids placing
                    // the projectile beyond a wall when the player is pressed against it.
                    Vector2 spawnPos = origin + forward * 0.15f;
                    if (kunaiBlockingLayers.value != 0 && clearance > 0f)
                    {
                        RaycastHit2D wallHit = Physics2D.Raycast(origin, forward, clearance, kunaiBlockingLayers);
                        if (wallHit.collider != null)
                            spawnPos = wallHit.point - forward * 0.03f;
                    }

                    activeKunai = Instantiate(kunaiPrefab, spawnPos, Quaternion.identity);
                    activeKunai.transform.localScale = new Vector3(direction, 1, 1);

                    KunaiProjectile projectile = activeKunai.GetComponent<KunaiProjectile>();
                    if (projectile != null)
                    {
                        // Reuse the projectile's configured surface mask so the
                        // spawn clearance matches the surfaces it can stick to.
                        if (kunaiBlockingLayers.value == 0)
                            kunaiBlockingLayers = projectile.stickableLayers;
                        projectile.Launch(forward, kunaiThrowArc);
                    }
                }
            }
            else
            {
                // 2. TELEPORT TO KUNAI
                transform.position = activeKunai.transform.position;
                
                // Destroy it so we can throw another one later
                Destroy(activeKunai);
                activeKunai = null;
                
                // Trigger the special attack animation
                anim.SetTrigger("specialAttack");
                
                // 3. AoE Damage/Knockback to Enemies
                Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, aoeKnockbackRadius);
                foreach (Collider2D enemy in hitEnemies)
                {
                    if (enemy.CompareTag("Enemies"))
                    {
                        EnemyBase enemyStats = enemy.GetComponent<EnemyBase>();
                        if (enemyStats != null)
                        {
                            enemyStats.stunBar(aoeStunDamage);
                        }
                    }
                }
                
                // 4. Safety Knockback for Player
                MasterState master = GetComponent<MasterState>();
                if (master != null)
                {
                    float pushDirection = transform.localScale.x > 0 ? -1f : 1f;
                    // Push player backward slightly after teleporting
                    master.ApplyKnockback(pushDirection * selfTeleportKnockback, 5f, 0.2f);
                }
            }
        }
    }

    private void ManageMovementState()
    {
        // Disable movement if we are currently attacking or guarding
        bool isDoingAction = anim.GetInteger("AttackIndex") > 0 || anim.GetBool("isGAURDING");
        
        if (playerMovement != null)
        {
            playerMovement.enabled = !isDoingAction;
            if (isDoingAction)
            {
                // Clear movement anims so we don't moonwalk while attacking
                anim.SetBool("isRUNNING", false);
                anim.SetBool("isJUMPING", false);
                anim.SetBool("isFALLING", false);
            }
        }
    }

    private void HandleAttackAndCharge()
    {
        // If guarding, don't allow attacks
        if (anim.GetBool("isGAURDING")) return;

        // Start Charging
        if (Input.GetButtonDown("Attack"))
        {
            isCharging = true;
            currentChargeTime = 0f;
        }

        // Keep Charging
        if (Input.GetButton("Attack") && isCharging)
        {
            currentChargeTime += Time.deltaTime;
        }

        // Release Button - Execute
        if (Input.GetButtonUp("Attack") && isCharging)
        {
            isCharging = false;

            if (currentChargeTime >= chargeTimeRequired)
            {
                PerformHeavyAttack();
            }
            else
            {
                PerformLightAttack(); 
            }
            
            currentChargeTime = 0f; 
        }
    }

    private void PerformLightAttack()
    {
        lastAttackTime = Time.time;
        
        if (comboStep == 0) comboStep = 1;
        else if (comboStep == 1) comboStep = 2;
        else if (comboStep >= 2) comboStep = 1;

        anim.SetInteger("AttackIndex", comboStep);
    }

    private void PerformHeavyAttack()
    {
        comboStep = 3; // Let's use 3 for Heavy Attack in your animator logic
        anim.SetInteger("AttackIndex", comboStep);
        lastAttackTime = Time.time; 
    }

    private void ResetComboIfWindowPassed()
    {
        if (comboStep != 0 && Time.time > lastAttackTime + comboWindow && !isCharging)
        {
            comboStep = 0;
            anim.SetInteger("AttackIndex", 0);
        }
    }

    private void HandleGuard()
    {
        if (Input.GetButton("Gaurd"))
        {
            if (!anim.GetBool("isGAURDING")) 
            {
                anim.SetInteger("GuardStance", Random.Range(1, 3)); 
                anim.SetBool("isGAURDING", true);
            }
        }
        else 
        {
            if (anim.GetBool("isGAURDING"))
            {
                anim.SetBool("isGAURDING", false);
                anim.SetInteger("GuardStance", 0);
            }
        }
    }

    private void HandleCombatArt()
    {
        // Pressing Guard and Attack together, or whatever button you prefer
        if (Input.GetButton("Gaurd") && Input.GetButtonDown("Attack"))
        {
            if (attributes != null && attributes.currentSpirit >= combatArtSpiritCost)
            {
                attributes.UseSpirit(combatArtSpiritCost);
                SwapCombatArtAnimation();
                anim.SetTrigger("TriggerCombatArt");
                
                comboStep = 0;
                anim.SetInteger("AttackIndex", 0);
            }
            else
            {
                Debug.Log("Not enough Spirit!");
            }
        }
    }

    public void SwapCombatArtAnimation()
    {
        if (equippedCombatArt != null && overrideController != null)
        {
            // The string here MUST match the exact name of the placeholder clip 
            // inside your ActiveCombatArt node in the Animator.
            overrideController["CombatArtPlaceholder"] = equippedCombatArt;
        }
    }

}
