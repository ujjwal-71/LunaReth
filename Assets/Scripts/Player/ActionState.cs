using System.Collections.Generic;
using UnityEngine;

public class ActionState : MonoBehaviour
{
    [Header("Components")]
    private Animator anim;
    private MasterState masterState;
    private _attributes attributes;
    private PlayerAttack[] playerAttacks = new PlayerAttack[0];

    [Header("Combat Arts")]
    public AnimationClip equippedCombatArt; 
    public int combatArtSpiritCost = 5; 

    [Header("Defense & Deflect")]
    [Tooltip("Time window in seconds after pressing Guard that counts as a Perfect Deflect.")]
    public float perfectDeflectWindow = 0.2f;

    [Header("Deathblow Execution")]
    public float executionRange = 2f;
    public int finisherDamage = 999;

    [Header("Attacks & Combos")]
    [Tooltip("Duration (in seconds) the Attack button must be held to trigger Heavy Attack instead of Light Attack.")]
    public float heavyChargeDuration = 0.5f;
    [Tooltip("Window at start of an attack (in seconds) where pressing Guard cancels the swing into a Deflect.")]
    public float attackCancelWindow = 0.18f;
    [Tooltip("Input buffer window (in seconds) to queue attacks or guards during recovery.")]
    public float inputBufferDuration = 0.15f;
    [Tooltip("Idle duration (in seconds) after swinging before combo sequence resets back to step 1.")]
    public float comboResetTimeout = 1.0f;

    // ==========================================
    // Internal Physics & Visual Constants
    // ==========================================
    private const float finisherOffset = 0.8f;
    private const float guardSpamThreshold = 0.35f;
    private const float guardSpamResetDelay = 0.5f;

    private AnimatorOverrideController overrideController;
    private float lastAttackTime;
    private int comboStep = 0;
    private float currentChargeTime = 0f;
    private bool isCharging = false;
    private float guardStartTime = -100f;
    private float lastGuardPressTime = -100f;
    private int guardSpamCount = 0;
    private float attackBufferTimer = 0f;
    private float guardBufferTimer = 0f;

    /// <summary>
    /// True when player is mid-swing or actively guarding.
    /// </summary>
    public bool IsActing => (anim != null) && (anim.GetInteger("AttackIndex") > 0 || anim.GetBool("isGAURDING"));

    private void Awake()
    {
        anim = GetComponent<Animator>();
        masterState = GetComponent<MasterState>();
        attributes = GetComponent<_attributes>();
        playerAttacks = GetComponentsInChildren<PlayerAttack>(true);
        
        overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = overrideController;
    }

    private void Update()
    {
        UpdateBuffers();

        // If player is in a locked master state (Dashing, Stunned, KnockedBack, Dead), ignore action inputs
        if (!masterState.CanAct)
        {
            if (comboStep > 0 && !isCharging)
            {
                comboStep = 0;
                anim.SetInteger("AttackIndex", 0);
            }
            return;
        }

        HandleGuard();
        HandleAttack();
        HandleCombatArt();
        
        ResetCombo();
        UpdateMovement();
    }

    private void UpdateBuffers()
    {
        if (attackBufferTimer > 0f)
            attackBufferTimer -= Time.deltaTime;

        if (guardBufferTimer > 0f)
            guardBufferTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Attack"))
            attackBufferTimer = inputBufferDuration;

        if (Input.GetButtonDown("Gaurd"))
            guardBufferTimer = inputBufferDuration;
    }

    private void UpdateMovement()
    {
        if (IsActing)
        {
            // Clear movement anims so we don't moonwalk while attacking or guarding
            anim.SetBool("isRUNNING", false);
            anim.SetBool("isJUMPING", false);
            anim.SetBool("isFALLING", false);
        }
    }

    private void HandleAttack()
    {
        if (anim.GetBool("isGAURDING"))
            return;

        // Finisher execution check
        if (Input.GetButtonDown("Attack") || attackBufferTimer > 0f)
        {
            if (TryFinisher())
            {
                attackBufferTimer = 0f;
                return;
            }
        }

        if (Input.GetButtonDown("Attack"))
        {
            isCharging = true;
            currentChargeTime = 0f;
        }

        if (Input.GetButton("Attack") && isCharging)
            currentChargeTime += Time.deltaTime;

        if (Input.GetButtonUp("Attack") && isCharging)
        {
            isCharging = false;

            if (currentChargeTime >= heavyChargeDuration)
            {
                PerformHeavyAttack();
            }
            else
            {
                PerformLightAttack(); 
            }
            
            attackBufferTimer = 0f;
            currentChargeTime = 0f;
        }
        // Attack buffer: if an attack was buffered while recovering, fire the instant the player is neutral
        else if (attackBufferTimer > 0f && comboStep == 0 && !isCharging)
        {
            attackBufferTimer = 0f;
            PerformLightAttack();
        }
    }

    private bool TryFinisher()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, executionRange);
        foreach (Collider2D col in colliders)
        {
            EnemyBase enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy == null || !enemy.CanBeFinished)
                continue;

            float direction = Mathf.Sign(enemy.transform.position.x - transform.position.x);
            if (direction == 0f)
                direction = 1f;

            transform.localScale = new Vector3(direction, 1f, 1f);

            // Snap player right in front of the enemy for clean visual weapon alignment
            Vector3 targetSnapPos = enemy.transform.position - new Vector3(direction * finisherOffset, 0f, 0f);
            targetSnapPos.y = transform.position.y; // Keep current ground plane
            transform.position = targetSnapPos;

            anim.SetTrigger("specialAttack");
            enemy.ExecuteFinisher(finisherDamage);

            // Brief invincibility during the finisher execution
            attributes.TakeDamage(0, Vector2.zero, 0f);

            PlayerAttack attack = GetComponentInChildren<PlayerAttack>();
            if (attack != null)
                StartCoroutine(attack.HitPause(0.12f));

            return true;
        }

        return false;
    }

    private void PerformLightAttack()
    {
        ResetHitTargets();
        lastAttackTime = Time.time;
        
        if (comboStep == 0) comboStep = 1;
        else if (comboStep == 1) comboStep = 2;
        else if (comboStep >= 2) comboStep = 1;

        anim.SetInteger("AttackIndex", comboStep);
    }

    private void PerformHeavyAttack()
    {
        ResetHitTargets();
        comboStep = 3; // AttackIndex 3 = Heavy Attack
        anim.SetInteger("AttackIndex", comboStep);
        lastAttackTime = Time.time; 
    }

    private void ResetHitTargets()
    {
        if (playerAttacks.Length == 0)
            playerAttacks = GetComponentsInChildren<PlayerAttack>(true);

        foreach (PlayerAttack attack in playerAttacks)
        {
            attack.ResetHitTargets();
        }
    }

    private void ResetCombo()
    {
        if (comboStep == 0 || isCharging)
            return;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        bool isAttackState = state.IsName("Attack1") || state.IsName("Attack2") || state.IsName("HeavyAttack") || state.IsName("ActiveCombatArt");

        // When the attack animation clip finishes its swing (normalizedTime >= 1.0f) and isn't transitioning to another attack, return to neutral
        if (isAttackState && state.normalizedTime >= 1.0f && !anim.IsInTransition(0))
        {
            comboStep = 0;
            anim.SetInteger("AttackIndex", 0);
        }
        // Safety timeout in case transition/name changes
        else if (Time.time > lastAttackTime + comboResetTimeout)
        {
            comboStep = 0;
            anim.SetInteger("AttackIndex", 0);
        }
    }

    public bool IsInParryWindow
    {
        get
        {
            if (!anim.GetBool("isGAURDING")) return false;

            // Sekiro anti-spam window decay:
            // 0 recent spam taps = full 0.20s window
            // 1 rapid tap = 0.10s
            // 2+ rapid taps = 0.05s
            float activeWindow = guardSpamCount == 0 
                ? perfectDeflectWindow 
                : (guardSpamCount == 1 ? perfectDeflectWindow * 0.5f : 0.05f);

            return Time.time <= guardStartTime + activeWindow;
        }
    }

    public void OnSuccessfulDeflect()
    {
        // Precise deflect immediately clears spam penalty
        guardSpamCount = 0;
    }

    private void HandleGuard()
    {
        // 1. Sekiro Attack Feint / Startup Cancel:
        // Within the early startup windup of an attack, pressing Guard cancels the attack directly into Deflect
        bool isAttacking = anim.GetInteger("AttackIndex") > 0;
        bool canFeint = isAttacking && (Time.time <= lastAttackTime + attackCancelWindow);

        if ((Input.GetButtonDown("Gaurd") || guardBufferTimer > 0f) && canFeint)
        {
            comboStep = 0;
            isCharging = false;
            currentChargeTime = 0f;
            anim.SetInteger("AttackIndex", 0);
            guardBufferTimer = 0f;
            TriggerGuardPress();
        }
        else if (Input.GetButtonDown("Gaurd"))
        {
            guardBufferTimer = 0f;
            TriggerGuardPress();
        }

        if (Input.GetButton("Gaurd"))
        {
            if (!anim.GetBool("isGAURDING") && anim.GetInteger("AttackIndex") == 0) 
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

    private void TriggerGuardPress()
    {
        // Sekiro anti-spam check: rapid taps within threshold increase spam count
        if (Time.time < lastGuardPressTime + guardSpamThreshold)
            guardSpamCount++;
        else if (Time.time >= lastGuardPressTime + guardSpamResetDelay)
            guardSpamCount = 0;

        lastGuardPressTime = Time.time;
        guardStartTime = Time.time;
    }

    private void HandleCombatArt()
    {
        // Hold Guard and press Attack to spend Spirit on the equipped Combat Art.
        if (Input.GetButton("Gaurd") && Input.GetButtonDown("Attack"))
        {
            if (attributes.currentSpirit >= combatArtSpiritCost)
            {
                attributes.UseSpirit(combatArtSpiritCost);
                ResetHitTargets();
                SetCombatArt();
                anim.SetTrigger("TriggerCombatArt");
                
                comboStep = 0;
                anim.SetInteger("AttackIndex", 0);
            }
        }
    }

    public void SetCombatArt()
    {
        // This clip name must match the placeholder in the Animator's Combat Art state.
        overrideController["CombatArtPlaceholder"] = equippedCombatArt;
    }
}
