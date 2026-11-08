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

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Prevent it from falling in a curve (optional, if you want it to shoot perfectly straight)
        rb.gravityScale = gravityScale;
        
        // Prevent tunneling (going inside walls/ground) at high speeds
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Destroy automatically after despawnTime if teleport isn't used
        Destroy(gameObject, despawnTime);
    }

    public void Launch(Vector2 direction, float upwardArc = 0f)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null || isStuck)
            return;

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

        // Check layers by name just in case Stickable Layers isn't set in the Inspector!
        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("wall");

        if (hitObj.layer == groundLayer || hitObj.layer == wallLayer || (stickableLayers.value & (1 << hitObj.layer)) > 0)
        {
            Stick(hitTransform);
        }
        else if (hitObj.CompareTag("Enemies"))
        {
            Stick(hitTransform);
        }
    }

    private void Stick(Transform targetTransform)
    {
        isStuck = true;
        
        // Stop physics simulation cleanly without destroying the component
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        // Disable collider so it doesn't trigger additional hits
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;
        
        // Parent to the object so it moves WITH the enemy or platform
        transform.SetParent(targetTransform);

        // Tilt it slightly so it looks like it thudded into the surface hard
        transform.Rotate(0, 0, Random.Range(-10f, 15f));
    }
}
