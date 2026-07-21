using Unity.VisualScripting;
using UnityEngine;

public class Enemy_AI : MonoBehaviour
{
    [Header("Components")]
    public Collider2D Attack_Box;
    private SpriteRenderer sprite_enemy;
    public Transform PlayerTransform;
    private Rigidbody2D EnemyRB;
    private Transform[] wayPoints;

    [Header("Stats")]
    public int Max_Health = 100;
    private int Current_Health = 100;
    private Transform nextPoint;

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
        wayPoints = new Transform[transform.childCount];
        for (int i=0; i<transform.childCount; i++)
        {
            wayPoints[i] = transform.GetChild(i);
        }
        nextPoint = wayPoints[0];
        currentState = enemyState.Patrol;
        Current_Health = Max_Health;
        sprite_enemy = GetComponent<SpriteRenderer>();
        EnemyRB = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
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

    private void Patrol()
    {
        if (nextPoint == wayPoints[transform.childCount-1])
            nextPoint = wayPoints[0];
        transform.Translate(1f,0f,0f,nextPoint);
    }
    
    private void Chasing()
    {
        
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
            Die();
        }
    }

    public void _Damaged()
    {
        sprite_enemy.color = new Color(1, 1, 1, 1);
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
