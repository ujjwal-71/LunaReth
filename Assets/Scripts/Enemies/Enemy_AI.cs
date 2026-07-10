using System;
using UnityEngine;

public class Enemy_AI : MonoBehaviour
{
    [Header("Components")]
    public Collider2D Attack_Box;
    private SpriteRenderer sprite_enemy;
    public Transform PlayerTransform;
    private Rigidbody2D EnemyRB;

    [Header("Stats")]
    public int Max_Health = 100;
    private int Current_Health = 100;

    private void Awake()
    {
        Current_Health = Max_Health;
        sprite_enemy = GetComponent<SpriteRenderer>();
        EnemyRB = GetComponent<Rigidbody2D>();
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
