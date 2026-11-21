using UnityEngine;

/// <summary>
/// Routes player state events (Jump, Land, Deflect, Hit) into the central VFXManager.
/// Exposes public animation event hooks for animation clips.
/// </summary>
public class PlayerVFXController : MonoBehaviour
{
    [Header("Feet Position for Dust")]
    [Tooltip("Reference to the feet transform for precise ground dust alignment.")]
    public Transform feetTransform;

    [Header("Weapon Transform for Sparks")]
    [Tooltip("Reference to the weapon transform for precise spark alignment.")]
    public Transform weaponTransform;

    // Component dependencies
    private MasterState masterState;
    private Movement movement;
    private _attributes attributes;
    private PlayerAttack playerAttack;

    private void Awake()
    {
        masterState = GetComponent<MasterState>();
        movement = GetComponent<Movement>();
        attributes = GetComponent<_attributes>();
        playerAttack = GetComponentInChildren<PlayerAttack>(true);

        if (feetTransform == null && movement != null)
        {
            feetTransform = movement.legs;
        }

        if (weaponTransform == null && playerAttack != null)
        {
            weaponTransform = playerAttack.transform;
        }
    }

    private void OnEnable()
    {
        if (movement != null)
        {
            movement.OnGroundJump += HandleGroundJump;
            movement.OnWallJump += HandleWallJump;
            movement.OnLand += HandleLand;
        }

        if (attributes != null)
        {
            attributes.OnDeflected += HandleDeflected;
            attributes.OnBlocked += HandleBlocked;
            attributes.OnHitTaken += HandleHitTaken;
            attributes.OnPostureBroken += HandlePostureBroken;
        }

        if (playerAttack != null)
        {
            playerAttack.OnAttackHitLanded += HandleAttackHitLanded;
        }
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.OnGroundJump -= HandleGroundJump;
            movement.OnWallJump -= HandleWallJump;
            movement.OnLand -= HandleLand;
        }

        if (attributes != null)
        {
            attributes.OnDeflected -= HandleDeflected;
            attributes.OnBlocked -= HandleBlocked;
            attributes.OnHitTaken -= HandleHitTaken;
            attributes.OnPostureBroken -= HandlePostureBroken;
        }

        if (playerAttack != null)
        {
            playerAttack.OnAttackHitLanded -= HandleAttackHitLanded;
        }
    }

    // ==========================================
    // STATE EVENT HANDLERS
    // ==========================================

    private void HandleGroundJump()
    {
        if (VFXManager.Instance == null) return;
        Vector3 pos = feetTransform != null ? feetTransform.position : transform.position;
        VFXManager.Instance.SpawnDustPuff(pos, Vector2.down * 0.4f, 0.75f, 0.22f);
    }

    private void HandleLand()
    {
        if (VFXManager.Instance == null) return;
        Vector3 pos = feetTransform != null ? feetTransform.position : transform.position;
        VFXManager.Instance.SpawnDustPuff(pos, Vector2.up * 0.2f, 1.15f, 0.28f);
    }

    private void HandleWallJump(Vector2 wallNormal)
    {
        if (VFXManager.Instance == null) return;
        Vector3 pos = transform.position;
        VFXManager.Instance.SpawnDustPuff(pos, wallNormal * 2.5f, 0.9f, 0.25f);
    }

    private void HandleDeflected()
    {
        if (VFXManager.Instance == null) return;
        Vector3 pos = weaponTransform != null ? weaponTransform.position : transform.position + Vector3.up * 0.5f;
        int dir = transform.localScale.x > 0 ? 1 : -1;
        VFXManager.Instance.SpawnDeflectSparks(pos, dir);
    }

    private void HandleBlocked()
    {
        if (VFXManager.Instance == null) return;
        Vector3 pos = weaponTransform != null ? weaponTransform.position : transform.position + Vector3.up * 0.5f;
        int dir = transform.localScale.x > 0 ? 1 : -1;
        VFXManager.Instance.SpawnDeflectSparks(pos, dir);
    }

    private void HandleHitTaken()
    {
        if (VFXManager.Instance == null) return;
        VFXManager.Instance.SpawnHitImpact(transform.position, true);
    }

    private void HandlePostureBroken()
    {
        if (VFXManager.Instance == null) return;
        Vector3 pos = transform.position + Vector3.up * 1.5f;
        VFXManager.Instance.SpawnDeflectSparks(pos, 1);
    }

    private void HandleAttackHitLanded(Vector2 enemyPos)
    {
        if (VFXManager.Instance == null) return;
        VFXManager.Instance.SpawnHitImpact(enemyPos, false);
    }

    // ==========================================
    // ANIMATION EVENT HOOKS
    // ==========================================

    public void AnimEvent_FootstepDust()
    {
        if (VFXManager.Instance == null) return;
        Vector3 pos = feetTransform != null ? feetTransform.position : transform.position;
        VFXManager.Instance.SpawnDustPuff(pos, Vector2.zero, 0.5f, 0.18f);
    }

    public void AnimEvent_SlashEffect(int attackIndex)
    {
        // Reserved for slash arc prefab or sweep
    }
}
