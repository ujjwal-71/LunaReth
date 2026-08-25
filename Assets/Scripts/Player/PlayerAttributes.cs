using System;
using UnityEngine;
using UnityEngine.UI;

public class _attributes : MonoBehaviour
{
    [Header("Components")]
    public Animator anim;

    public string tagFilter;
    private Rigidbody2D RB;
    private SpriteRenderer playerSprite;

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
    private Vector3 checkPoint;
    
    private void Awake()
    {
        currentStun = maxStun;
        checkPoint = new Vector3(-22,-23,0);
        currentHealth = maxHealth;
        RB = GetComponent<Rigidbody2D>();
        playerSprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        PlayerHealthCheck();

        if (invinsTimer>0 && reSpawnTimer == 0)
        {
            invinsTimer -= Time.deltaTime;
            playerSprite.color = new Color(invinsTimer,0.5f,0.5f);
        }
        else if (reSpawnTimer != 0)
        {
            
        }
        else
        {
            isInvinsible = false;
            playerSprite.color = new Color(1,1,1);
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
        anim.SetBool("isDEATH", true);
        playerSprite.color = new Color(1,1,1,(float)Math.Pow(timer/2,2));
        GetComponent<Movement>().InterruptAction(4f);
    }

    public void PlayerRespawn()
    {
        anim.SetBool("isDEATH", false);
        reSpawnTimer = 0;
        currentHealth = maxHealth;
        playerSprite.color = new Color(1,1,1,1);
        transform.position = checkPoint;
    }
}
