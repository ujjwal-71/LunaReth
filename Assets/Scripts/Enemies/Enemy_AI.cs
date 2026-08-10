using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class Enemy_AI : MonoBehaviour
{
    [Header("Components")]
    public Collider2D Attack_Box;
    public Animator enemyAnimator;
    private SpriteRenderer sprite_enemy;
    public Transform PlayerTransform;
    private Rigidbody2D EnemyRB;
    public GameObject waypointsGameObj;
    private Transform[] wayPoints;
    public int speed = 30;

    [Header("Stats")]
    public int Max_Health = 100;
    private int Current_Health = 100;

    private enum enemyState
    {
        Patrol,
        Chasing,
        Attacking,
        Healing,
        getDamage,
    }

    private enemyState currentState;

    private void Awake()
    {
        wayPoints = new Transform[waypointsGameObj.transform.childCount];
        for (int i=0; i<waypointsGameObj.transform.childCount; i++)
        {
            wayPoints[i] = waypointsGameObj.transform.GetChild(i);
        }
        currentState = enemyState.Patrol;
        Current_Health = Max_Health;
        sprite_enemy = GetComponent<SpriteRenderer>();
        EnemyRB = GetComponent<Rigidbody2D>();
    }

    private float playerDistanceX;
    private float playerDistanceY;
    void Update()
    {
        playerDistanceX = transform.position.x - PlayerTransform.position.x;
        playerDistanceY = PlayerTransform.position.y - transform.position.y;

        if (playerDistanceX < 8 && playerDistanceY < 5)
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
            case enemyState.getDamage:
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

            EnemyRB.linearVelocityX = Mathf.Sign(distance) * speed;
            enemyAnimator.SetBool("isMOVING",true);
            enemyAnimator.SetBool("isCHASING",false);
        }
        
    }
    private void Chasing()
    {
        if (playerDistanceX > 0.5f || playerDistanceX < -0.5f)
        {
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
}
