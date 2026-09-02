using System;
using UnityEngine;
using UnityEngine.UI;

public class _attributes : MonoBehaviour
{
    [Header("Components")]
    public Animator anim;

    public string tagFilter;
    private Rigidbody2D RB;

    [Header("Attributes")]
    public int maxHealth=100;
    public int maxStun = 10;
    public int currentStun;
    private float invinsTimer = 0;
    private float stunTimer = 0;
    public float parryTimer;
    public float invinsTime = 1;
    private int currentHealth;
    private bool isInvinsible;
    private bool isStunned;
    public bool isGuarded;
    private float reSpawnTimer;
    public float deathAnimTimer;
    public Vector3 checkPoint;
    
    private void Awake()
    {
        currentStun = maxStun;
        currentHealth = maxHealth;
        RB = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        PlayerHealthCheck();

        if (invinsTimer>0 && reSpawnTimer == 0)
        {
            invinsTimer -= Time.deltaTime;
        }
        else if (reSpawnTimer != 0)
        {
            
        }
        else
        {
            isInvinsible = false;
        }

        if (stunTimer>0)
            stunTimer -= Time.deltaTime;
        else
            isStunned = false;
    }



    public void MobContactDamage(int damage, Transform enemyTransform)
    {
        if(!isInvinsible)
        {
            currentHealth -= damage;
            isInvinsible = true;
            isStunned = true;
            invinsTimer = invinsTime;
            stunTimer = 0.2f;
            Vector2 pushDirection = (transform.position - enemyTransform.position).normalized;
            RB.linearVelocity = new Vector2(pushDirection.x * 10f, 10f);
        }
    }
    

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(tagFilter))
        {
            Debug.Log("OUCH! I was just stabbed by an object named: " + collision.gameObject.name);
            MobContactDamage(10,collision.transform);
        }
    }

    public void PlayerHealthCheck()
    {
        if (currentHealth <= 0)
        {
            
            reSpawnTimer = 3f;
            currentHealth = maxHealth;
        }
        if (reSpawnTimer > 0.1f)
        {
            PlayerDeath(reSpawnTimer);
            reSpawnTimer -= Time.deltaTime;
        }
        if (reSpawnTimer <= 0.1f && reSpawnTimer != 0)
            PlayerRespawn();
    }

    public void PlayerDeath(float timer)
    {
        StartCoroutine(GetComponent<Movement>().InterruptAction(deathAnimTimer));
    }

    public void PlayerRespawn()
    {
        reSpawnTimer = 0;
        currentHealth = maxHealth;
        transform.position = checkPoint;
    }
}
