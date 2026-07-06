using UnityEngine;
using UnityEngine.UI;

public class _attributes : MonoBehaviour
{
    public int _MaxHealth=100;
    private int _CurrentHealth;
    public Slider HealthSlider;
    void Start()
    {
        _CurrentHealth = _MaxHealth;
        HealthSlider.maxValue = _MaxHealth;
    }

    public void GetDamage()
    {
        
    }  
}
