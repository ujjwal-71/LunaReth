using UnityEngine;

public class Attack : MonoBehaviour
{
    public int damage = 20;
    public Collider2D enemy;

    void Start()
    {
        enemy.GetComponent<Enemy_AI>();
    }

    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D enemy)
    {
    
    }
}
