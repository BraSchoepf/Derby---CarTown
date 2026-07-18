using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class VehicleHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    float currentHealth;

    [Header("Config de daño")]
    public float minForceForDamage = 3f;
    public float forceToDamageRatio = 1.5f;
    [Tooltip("Cuánto pesa 'quién te embistió' vs tu propia velocidad al calcular tu daño recibido")]
    [Range(0f, 1f)] public float attackerWeightFactor = 0.8f;
    [Tooltip("Multiplicador si no se encuentra ninguna zona (fallback de seguridad)")]
    public float fallbackMultiplier = 1f;

    [Header("Layer de otros autos")]
    public LayerMask carLayer;

    public event Action<float, float> OnHealthChanged;
    public event Action OnVehicleDestroyed;

    bool isDestroyed = false;
    Rigidbody rb;
    VehicleHitZone[] hitZones;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        hitZones = GetComponentsInChildren<VehicleHitZone>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDestroyed) return;
        if (((1 << collision.gameObject.layer) & carLayer) == 0) return;

        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null) return;

        ContactPoint contact = collision.contacts[0];

        float closingSpeed = CalculateClosingSpeed(otherRb, contact);
        if (closingSpeed < minForceForDamage) return;

        // La cápsula física es la que recibe el contacto real; buscamos
        // qué zona lógica (trigger) está geométricamente más cerca de ese punto
        VehicleHitZone zone = GetClosestZone(contact.point);
        float zoneMultiplier = zone != null ? zone.damageMultiplier : fallbackMultiplier;

        ApplyDamage(closingSpeed * forceToDamageRatio * zoneMultiplier);
    }

    VehicleHitZone GetClosestZone(Vector3 worldContactPoint)
    {
        VehicleHitZone closest = null;
        float minDist = float.MaxValue;

        foreach (var hz in hitZones)
        {
            Collider col = hz.GetComponent<Collider>();
            if (col == null) continue;

            Vector3 closestPoint = col.ClosestPoint(worldContactPoint);
            float dist = Vector3.Distance(closestPoint, worldContactPoint);

            if (dist < minDist)
            {
                minDist = dist;
                closest = hz;
            }
        }
        return closest;
    }

    // Fuerza asimétrica: cuánto se movía el OTRO hacia mí + cuánto me movía YO hacia el impacto,
    // ponderado por attackerWeightFactor. Esto reemplaza el par duplicado que tenías antes
    // (CalculateClosingSpeed / CalculateAsymmetricForce hacían lo mismo con distinto peso).
    float CalculateClosingSpeed(Rigidbody otherRb, ContactPoint contact)
    {
        float otherSpeedTowardMe = Mathf.Max(0f, Vector3.Dot(otherRb.linearVelocity, -contact.normal));
        float mySpeedTowardImpact = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, contact.normal));

        return (otherSpeedTowardMe * attackerWeightFactor)
             + (mySpeedTowardImpact * (1f - attackerWeightFactor));
    }

    public void ApplyDamage(float amount)
    {
        if (isDestroyed) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f) DestroyVehicle();
    }

    void DestroyVehicle()
    {
        isDestroyed = true;
        OnVehicleDestroyed?.Invoke();
        var controller = GetComponent<CarController>();
        if (controller != null) controller.enabled = false;
    }

    public float CurrentHealth => currentHealth;
    public float HealthPercent => currentHealth / maxHealth;
    public bool IsDestroyed => isDestroyed;
}