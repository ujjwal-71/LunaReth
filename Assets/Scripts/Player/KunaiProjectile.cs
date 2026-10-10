using UnityEngine;

public class KunaiProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 25f;
    public float despawnTime = 4f;

    public LayerMask stickableLayers;

    private Rigidbody2D rb;
    private bool isStuck = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Destroy automatically after 4 seconds if teleport isn't used
        Destroy(gameObject, despawnTime);
        
        // Give it initial forward velocity based on rotation or scale
        // Assumes ActionState sets the proper scale/rotation when spawning
        float direction = transform.localScale.x > 0 ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * speed, 0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isStuck) return;

        // Hit a Wall/Ground
        if ((stickableLayers.value & (1 << collision.gameObject.layer)) > 0)
        {
            StickToSurface(collision.transform);
        }
        // Hit an Enemy
        else if (collision.CompareTag("Enemies"))
        {
            StickToSurface(collision.transform);
        }
    }

    private void StickToSurface(Transform targetTransform)
    {
        isStuck = true;
        
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Stop physics interactions (Modern Unity Way)
        }
        
        // Parent to the object so it moves WITH the enemy or platform!
        transform.SetParent(targetTransform);
    }
}
