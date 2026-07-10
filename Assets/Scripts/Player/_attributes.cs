using UnityEngine;
using UnityEngine.UI;

public class _attributes : MonoBehaviour
{
    public int _MaxHealth=100;
    private int _CurrentHealth;
    public Slider HealthSlider;
    private Collider2D playerCollider;
    
    private bool isInvinsible;
    
    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        _CurrentHealth = _MaxHealth;
        HealthSlider.maxValue = _MaxHealth;
    }

    public void GetDamage()
    {
        if(!isInvinsible)
        {
            
        }
    }  
}
