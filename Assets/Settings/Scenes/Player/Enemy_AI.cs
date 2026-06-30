using System;
using UnityEngine;

public class Enemy_AI : MonoBehaviour
{
    public Collider2D Attack_Box;
    private SpriteRenderer sprite_enemy;
    public int Max_Health = 100;
    int Current_Health = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Current_Health = Max_Health;
        sprite_enemy = GetComponent<SpriteRenderer>();
    }
    public void Damaging(int Damage)
    {
        Current_Health -= Damage;
        sprite_enemy.color = new Color(1,0.2f,0.3f,1);
        if(Current_Health<=0)
            Die();
    }
    public void _Damaged()
    {
        sprite_enemy.color = new Color(1,1,1,1);
    }
    void Die()
    {
        Destroy(gameObject);
    }
}
