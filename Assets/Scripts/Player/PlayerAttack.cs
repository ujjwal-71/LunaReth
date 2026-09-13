using System;
using System.Collections;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D playerRigid;
    public Transform playerTransform;

    [Header("Attributes")]
    public int attackDamage;
    public int stunAmount;

    public IEnumerator HitPause(float duration)
    {
        Debug.Log(Time.timeScale);
        Time.timeScale = 0.5f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }

    public void OnTriggerEnter2D(Collider2D enemy)
    {
        int temp = (playerTransform.localScale.x < 0) ? -1 : 1;
        EnemyBase enemyStats = enemy.GetComponent<EnemyBase>();
        _attributes player = GetComponentInParent<_attributes>();
        if (enemy.CompareTag("EnemiesWeapon"))
        {
            if ( player != null && player.isGuarded && player.parryTimer > 0)
            {
                Debug.Log("Perfect Parry");
                enemyStats.stunBar(stunAmount);
                return;
            }
            else if (player != null && player.isGuarded && player.parryTimer < 0)
            {
                Debug.Log("Perfect Gaurd");
                player.currentStun += 10;
                playerRigid.linearVelocity = new Vector2(temp * 10 , 5);
                return;
            }
        }
        else if (enemy.CompareTag("Enemies"))
        {
            if ( player != null && !player.isGuarded)
            {
                Debug.Log("Enemy Hitted");
                return;
            }
        }
    }
}