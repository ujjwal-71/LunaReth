using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public int attackPower;
    public int stunAmount = 10;
    public int stunPower;

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

        if(playerStats == null) return;
        
        playerStats.GetDamage(attackPower);
        if (playerStats.parried)
        {
            Debug.LogError("Parried");
            enemyStats.stunBar(playerStats.stunPower);
        }
        else playerStats.currentStun += 5;
    }
}
