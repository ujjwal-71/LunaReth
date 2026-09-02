using System;
using Unity.Mathematics;
using UnityEngine;

public class Enemy_AI : MonoBehaviour
{
    [Header("Components")]
    public Animator enemyAnimator;
    private SpriteRenderer sprite_enemy;
    public Transform PlayerTransform;
    private Rigidbody2D EnemyRB;
    public GameObject waypointsGameObj;
    private Transform[] wayPoints;
    public int speed = 30;
    private bool vunerable;
    public Transform healthBar;

    [Header("Stats")]
    public float Max_Health = 100;
    private float Current_Health = 100;
    private int maxMobStun = 100;
    public int currentMobStun;
    private float stunTimer = 5f;

    private enum enemyState
    {
        Patrol,
        Chasing,
        Attacking,
        Healing,
        stun,
    }

    private enemyState currentState;

    private void Awake()
    {
        currentMobStun = maxMobStun;
        wayPoints = new Transform[waypointsGameObj.transform.childCount];
        for (int i=0; i<waypointsGameObj.transform.childCount; i++)
        {
            wayPoints[i] = waypointsGameObj.transform.GetChild(i);
        }
        currentState = enemyState.Patrol;
        Current_Health = Max_Health;
        healthBar.transform.localScale = new Vector3(Current_Health/Max_Health, healthBar.transform.localScale.y, 0);
        sprite_enemy = GetComponent<SpriteRenderer>();
        EnemyRB = GetComponent<Rigidbody2D>();
    }

    private float playerDistanceX;
    private float playerDistanceY;
    void Update()
    {
        if (vunerable && EnemyRB.linearVelocity.magnitude < 0.1f)
        {
            sprite_enemy.color = new Color(1,1,1);
            vunerable = false;
        }

        if (currentMobStun <= 0)
            currentState = enemyState.stun;
        
        playerDistanceX = transform.position.x - PlayerTransform.position.x;
        playerDistanceY = PlayerTransform.position.y - transform.position.y;

        if (Mathf.Abs(playerDistanceX) < 8 && MathF.Abs(playerDistanceY) < 5)
            currentState = enemyState.Chasing;
        else
            currentState = enemyState.Patrol;

        switch (currentState)
        {
            case enemyState.Patrol:
                Patrol();
                break;
            case enemyState.Chasing:
                Chasing();
                break;
            case enemyState.Healing:
                Healing();
                break;
            case enemyState.Attacking:
                Attacking();
                break;
            case enemyState.stun:
                stunned();
                break;
            default:
                break;
        }
    }

    private int wayPointsIndex = 0;
    private float waitTimer = 0;
    private float distance;
    
    private void Patrol()
    {
        distance = wayPoints[wayPointsIndex].position.x - transform.position.x;
        if (distance < 0.2 && distance > -0.2)
        {
            if (waitTimer < 1f)
            {
                EnemyRB.linearVelocityX = 0;
                enemyAnimator.SetBool("isMOVING",false);
                enemyAnimator.SetBool("isCHASING",false);
                waitTimer += Time.deltaTime;
            }
            else
            {
                waitTimer = 0;
                if (wayPointsIndex == waypointsGameObj.transform.childCount-1)
                    wayPointsIndex = 0;
                else
                    wayPointsIndex++;
            }
        }
        else
        {
            if (Mathf.Sign(distance) == -1)
                transform.rotation = Quaternion.Euler(0,0,0);
            else
                transform.rotation = Quaternion.Euler(0,180,0);
            
            if (!vunerable)
            {
            EnemyRB.linearVelocityX = Mathf.Sign(distance) * speed;
            enemyAnimator.SetBool("isMOVING",true);
            enemyAnimator.SetBool("isCHASING",false);
            }
        }
        
    }
    private void Chasing()
    {
        if (playerDistanceX > 0.5f || playerDistanceX < -0.5f)
        {
            if(!vunerable)
                EnemyRB.linearVelocityX = -Mathf.Sign(playerDistanceX) * speed * 1.8f;

            if (Mathf.Sign(-playerDistanceX) == -1)
                transform.rotation = Quaternion.Euler(0,0,0);
            else
                transform.rotation = Quaternion.Euler(0,180,0);
        }
        if (Mathf.Abs(EnemyRB.linearVelocityX) > 4)
        {
            enemyAnimator.SetBool("isCHASING",true);
            enemyAnimator.SetBool("isMOVING",false);
        }
        else if(Mathf.Abs(EnemyRB.linearVelocityX) < 4 && Mathf.Abs(EnemyRB.linearVelocityX) > 1f)
        {
            enemyAnimator.SetBool("isCHASING",false);
            enemyAnimator.SetBool("isMOVING",true);
        }
        else
            enemyAnimator.SetBool("isCHASING",false);
            enemyAnimator.SetBool("isMOVING",false);
    }

    private void Healing()
    {
        
    }

    private void Attacking()
    {
        
    }

    private void stunned()
    {
        if (stunTimer < 0)
        {
            currentState = enemyState.Patrol;
            stunTimer = 5f;
        }
        else
            stunTimer -= Time.deltaTime; 
        EnemyRB.linearVelocity = Vector2.zero;
    }

    public void stun(int stunAmount)
    {
        if(!vunerable)
            EnemyRB.linearVelocity = Vector2.zero;

        currentMobStun -= stunAmount;
        vunerable = true;
        EnemyRB.linearVelocity = new Vector2( MathF.Sign(playerDistanceX)*12 , 10 );
    }
    public void GetDamaged(int damagePower)
    {
        sprite_enemy.color = new Color(1,0.5f,0.3f);

        if(!vunerable)
            EnemyRB.linearVelocity = Vector2.zero;

        Current_Health -= damagePower;
        if ( Current_Health < 0)
            Destroy(gameObject);
        
        healthBar.transform.localScale = new Vector3(Current_Health/Max_Health, healthBar.transform.localScale.y, 0);
        vunerable = true;
        EnemyRB.linearVelocity = new Vector2( MathF.Sign(playerDistanceX)*8 , 8 );
    }
}
