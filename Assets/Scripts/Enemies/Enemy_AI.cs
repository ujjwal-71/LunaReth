using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class Enemy_AI : MonoBehaviour
{
    [Header("Components")]
    public Collider2D Attack_Box;
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

    private float playerDistance;
    void Update()
    {
        currentState = enemyState.Chasing;
        playerDistance = PlayerTransform.position.x - transform.position.x;
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
        }
    }
    private void Chasing()
    {
        if (playerDistance > 0.2f || playerDistance < -0.2f)
        {
            EnemyRB.linearVelocityX = Mathf.Sign(playerDistance) * speed;
        }
    }

    private void Healing()
    {
        
    }

    private void Attacking()
    {
        
    }

    public void Damaging(int Damage)
    {
        Current_Health -= Damage;
        sprite_enemy.color = new Color(1, 0.2f, 0.3f, 1);
        EnemyRB.linearVelocity = new Vector2(PlayerTransform.right.x * 5, 10.5f) ;
        
        if (Current_Health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void _Damaged()
    {
        sprite_enemy.color = new Color(1, 1, 1, 1);
    }

}
