using UnityEngine;

public class Enemy_AI : MonoBehaviour
{
    public Collider2D Attack_Box;
    public int Max_Health = 100;
    int Current_Health = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Current_Health = Max_Health;
    }
    public void Damaging(int Damage)
    {
        Current_Health -= Damage;
        if(Current_Health<=0)
            Die();
    }
    void Die()
    {
        Destroy(gameObject);
    }
}
