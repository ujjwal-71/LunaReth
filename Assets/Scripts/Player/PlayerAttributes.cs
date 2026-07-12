using UnityEngine;
using UnityEngine.UI;

public class _attributes : MonoBehaviour
{
    [Header("Components")]
    public Slider HealthSlider;
    public string tagFilter;
    private Rigidbody2D RB;
    private SpriteRenderer playerSprite;

    [Header("Attributes")]
    public int maxHealth=100;
    private float invinsTimer = 0;
    private float stunTimer = 0;
    public float invinsTime = 1;
    
    private int currentHealth;
    private bool isInvinsible;
    private bool isStunned;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        HealthSlider.maxValue = maxHealth;
        RB = GetComponent<Rigidbody2D>();
        playerSprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (invinsTimer>0)
        {
            invinsTimer -= Time.deltaTime;
            playerSprite.color = new Color(1-invinsTimer,0.5f,0.5f,1);
        }
        else
        {
            isInvinsible = false;
            playerSprite.color = new Color(1,1,1,1);
        }

        if (stunTimer>0)
            stunTimer -= Time.deltaTime;
        else
            isStunned = false;

        if (isStunned)
            GetComponent<Movement>().enabled = false;
        else
            GetComponent<Movement>().enabled = true;
    }

    public void MobContactDamage(int damage, Transform enemyTransform)
    {
        if(!isInvinsible)
        {
            currentHealth -= damage;
            HealthSlider.value = currentHealth;
            GetComponent<Movement>().enabled = false;
            Vector2 pushDirection = (transform.position - enemyTransform.position).normalized;
            RB.linearVelocity = pushDirection * new Vector2(35,10);
            invinsTimer = invinsTime;
            stunTimer = 0.2f;
            isInvinsible = true;
            isStunned = true;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(tagFilter))
        {
            MobContactDamage(10,collision.transform);
        }
    }
}
