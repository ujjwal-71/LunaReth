using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D playerRigid;
    public Transform playerTransform;

    [Header("Attributes")]
    private float AttackPause = 0.2f;
    public int attackPower = 10;


    public void OnTriggerEnter2D(Collider2D enemy)
    {
        if (enemy.CompareTag("Enemy"))
        {
            Enemy_AI hitEnemy = enemy.GetComponent<Enemy_AI>();
            if (hitEnemy != null)
            {
                hitEnemy.GetDamaged(attackPower);
                playerRigid.linearVelocity = new Vector2(10 *2 , 8);
            }
        }
    }
}
