using UnityEngine;

public class KunaiProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 25f;
    public float despawnTime = 4f;

    public LayerMask stickableLayers;
    public float gravityScale;

    private Rigidbody2D rb;
    private bool isStuck = false;
    private bool hasLaunched = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Prevent it from falling in a curve (optional, if you want it to shoot perfectly straight)
        rb.gravityScale = gravityScale;
        
        // Prevent tunneling (going inside walls/ground) at high speeds
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Destroy automatically after 4 seconds if teleport isn't used
        Destroy(gameObject, despawnTime);
        
        // Keep compatibility with any scene or prefab that instantiates the kunai directly.
        if (!hasLaunched)
        {
            float direction = transform.localScale.x > 0 ? 1f : -1f;
            Launch(new Vector2(direction, 0f), 0f);
        }
    }

    public void Launch(Vector2 direction, float upwardArc = 0f)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null || isStuck)
            return;

        hasLaunched = true;
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        rb.linearVelocity = direction * speed + Vector2.up * upwardArc;

        // Rotate the sprite's forward axis to match the launch vector. Horizontal
        // facing is carried by velocity, so avoid also mirroring its scale here.
        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.localScale = new Vector3(1f, 1f, 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        // Rotate the kunai to match its velocity direction while it's in the air
        if (!isStuck && rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            
            // If the sprite is flipped left (scale X is negative), we must adjust the angle
            // otherwise the rotation would apply twice (once from scale, once from rotation)
            if (transform.localScale.x < 0)
            {
                angle -= 180f;
            }
            
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleHit(collision.gameObject, collision.transform);
    }

    // Fallback just in case you forgot to check "Is Trigger" on the BoxCollider2D!
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject, collision.transform);
    }

    private void HandleHit(GameObject hitObj, Transform hitTransform)
    {
        if (isStuck) return;

        Debug.Log($"Kunai Hit: [{hitObj.name}]. It is on Layer: [{LayerMask.LayerToName(hitObj.layer)}], Tag: [{hitObj.tag}]");

        // Check layers by name just in case Stickable Layers isn't set in the Inspector!
        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("wall");

        if (hitObj.layer == groundLayer || hitObj.layer == wallLayer || (stickableLayers.value & (1 << hitObj.layer)) > 0)
        {
            StickToSurface(hitTransform);
        }
        else if (hitObj.CompareTag("Enemies"))
        {
            StickToSurface(hitTransform);
        }
        else
        {
            Debug.LogWarning($"Kunai did not stick to [{hitObj.name}] because its Layer isn't 'Ground' or 'Wall', and its Tag isn't 'Enemy'!");
        }
    }

    private void StickToSurface(Transform targetTransform)
    {
        isStuck = true;
        
        // Disable the collider so it stops hitting things
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;

        // The ultimate way to stop it from falling: Destroy the Rigidbody completely!
        if (rb != null)
        {
            Destroy(rb); 
        }
        
        // Parent to the object so it moves WITH the enemy!
        transform.SetParent(targetTransform);

        // Tilt it slightly so it looks like it thudded into the surface hard
        transform.Rotate(0, 0, Random.Range(-10f, 15f));
    }
}
