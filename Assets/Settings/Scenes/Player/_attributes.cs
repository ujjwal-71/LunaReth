using UnityEngine;
using UnityEngine.UI;

public class _attributes : MonoBehaviour
{
    public int _MaxHealth=100;
    public int _CurrentHealth;
    public Slider HealthSlider;
    void Start()
    {
        _CurrentHealth = _MaxHealth;
        HealthSlider.maxValue = _MaxHealth;
    }
    void Update()
    {
        HealthSlider.value = _CurrentHealth;
    }
}
