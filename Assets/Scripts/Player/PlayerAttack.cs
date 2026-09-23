using System.Collections;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D playerRigid;
    public Transform playerTransform;

    [Header("Attributes")]
    public int attackDamage;
    public IEnumerator HitPause(float duration)
    {
        Debug.LogError(Time.timeScale);
        Time.timeScale = 0.5f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }

    public void OnTriggerEnter2D(Collider2D enemy)
    {
        EnemyBase enemyStats = enemy.GetComponent<EnemyBase>();
        _attributes playerStats = GetComponentInParent<_attributes>();

        if(enemyStats == null)
            return;

        if (enemy.CompareTag("Enemies") || enemy.CompareTag("EnemiesWeapon"))
        {
            enemyStats.healthBar(attackDamage);
        }

    }
}