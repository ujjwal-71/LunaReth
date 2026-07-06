using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("Stats")]
    public int damage = 20;

    [Header("Components")]
    public Collider2D enemy;

    void OnTriggerEnter2D(Collider2D enemy)
    {
        Enemy_AI enemyAI = enemy.GetComponent<Enemy_AI>();
        if (enemyAI != null)
        {
            enemyAI.Damaging(20);
        }
    }

    void OnTriggerExit2D(Collider2D enemy)
    {
        Enemy_AI enemyAI = enemy.GetComponent<Enemy_AI>();
        if (enemyAI != null)
        {
            enemyAI._Damaged();
        }
    }
}
