using UnityEngine;

public class Attack : MonoBehaviour
{
    public int damage = 20;
    public Collider2D enemy;

    void Start()
    {
        
    }

    void Update()
    {
    }

    void OnTriggerEnter2D(Collider2D enemy)
    {
        Enemy_AI enemyAI = enemy.GetComponent<Enemy_AI>();
        if(enemyAI != null)
        enemy.GetComponent<Enemy_AI>().Damaging(20);
    }
}
