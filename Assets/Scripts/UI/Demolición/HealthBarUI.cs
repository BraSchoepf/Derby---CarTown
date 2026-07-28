using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Slider healthSlider;
    VehicleHealth targetHealth;

    public void SetTarget(VehicleHealth health)
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= UpdateBar; // limpiar suscripción anterior

        targetHealth = health;
        targetHealth.OnHealthChanged += UpdateBar;
        healthSlider.value = 1f;
    }

    void UpdateBar(float current, float max)
    {
        healthSlider.value = current / max;
    }
}