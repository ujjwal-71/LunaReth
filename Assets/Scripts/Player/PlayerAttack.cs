using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("Stats")]
    public int damage = 20;

    [Header("Components")]
    public Collider2D enemy;
    public Rigidbody2D RB;


    void OnTriggerEnter2D(Collider2D enemy)
    {
        Enemy_AI enemyAI = enemy.GetComponent<Enemy_AI>();
        if (enemyAI != null)
        {
            RB.linearVelocity = transform.right * -4;
            enemyAI.Damaging(20);
        }
    }

    void OnTriggerExit2D(Collider2D enemy)
    {
        Enemy_AI enemyAI = enemy.GetComponent<Enemy_AI>();
        if (enemyAI != null)
        {
            RB.linearVelocity = Vector2.zero;
            enemyAI._Damaged();
        }
    }
}
