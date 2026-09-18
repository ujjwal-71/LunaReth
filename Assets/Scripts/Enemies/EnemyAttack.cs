using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public int attackPower;
    public int stunAmount = 10;

    void Update()
    {
        
    }
    void FixedUpdate()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D player)
    {
        EnemyBase enemyStats = GetComponentInParent<EnemyBase>();
        _attributes playerStats = player.GetComponent<_attributes>();
        Movement PlayerMovement = player.GetComponent<Movement>();

        if(playerStats == null)
            return;
        if (player.CompareTag("Player"))
        {
            int pushDirection = (transform.position.x < player.transform.position.x) ? 1 : -1;
            if (playerStats.isGuarded && playerStats.parryTimer > 0)
            {
                Debug.LogWarning("Perfect Parry");
                enemyStats.stunBar(10);
                return;
            }
            else if (playerStats.isGuarded && playerStats.parryTimer < 0)
            {
                Debug.LogWarning("Perfect Gaurd");
                playerStats.currentStun += stunAmount;
                PlayerMovement.ApplyKnockback(pushDirection * 10, 5, 0.3f);
                return;
            }
            else
                playerStats.GetDamage(attackPower);
        }
    }
}
