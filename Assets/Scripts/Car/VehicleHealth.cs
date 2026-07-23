using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class VehicleHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    float currentHealth;

    [Header("Config de daño")]
    [Tooltip("Si está destildado, los choques no restan vida (ej: modo Drift). Los eventos de colisión igual se disparan para otros sistemas (ej: reset de multiplicador de drift).")]
    public bool damageEnabled = true;
    public float minForceForDamage = 3f;
    public float forceToDamageRatio = 1.5f;
    [Range(0f, 1f)] public float attackerWeightFactor = 0.8f;
    public float fallbackMultiplier = 1f;

    [Header("Layer de otros autos")]
    public LayerMask carLayer;

    public event Action<float, float> OnHealthChanged;
    public event Action OnVehicleDestroyed;
    public event Action<VehicleHealth> OnVehicleDestroyedByAttacker;
    public event Action<Collision> OnCollisionDetected; // se dispara SIEMPRE, tenga o no daño habilitado

    VehicleHealth lastAttacker;
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

        OnCollisionDetected?.Invoke(collision); // siempre, útil para sistemas como DriftScoreTracker

        if (!damageEnabled) return; // corta acá si el modo actual no aplica daño

        VehicleHealth otherHealth = otherRb.GetComponent<VehicleHealth>();
        if (otherHealth != null) lastAttacker = otherHealth;

        ContactPoint contact = collision.contacts[0];

        float closingSpeed = CalculateClosingSpeed(otherRb, contact);
        if (closingSpeed < minForceForDamage) return;

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

    float CalculateClosingSpeed(Rigidbody otherRb, ContactPoint contact)
    {
        float otherSpeedTowardMe = Mathf.Max(0f, Vector3.Dot(otherRb.linearVelocity, -contact.normal));
        float mySpeedTowardImpact = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, contact.normal));

        return (otherSpeedTowardMe * attackerWeightFactor)
             + (mySpeedTowardImpact * (1f - attackerWeightFactor));
    }

    public void ApplyDamage(float amount)
    {
        if (isDestroyed || !damageEnabled) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f) DestroyVehicle();
    }

    void DestroyVehicle()
    {
        isDestroyed = true;
        OnVehicleDestroyed?.Invoke();
        OnVehicleDestroyedByAttacker?.Invoke(lastAttacker);
        var controller = GetComponent<CarController>();
        if (controller != null) controller.StopAllInputs();
    }

    public float CurrentHealth => currentHealth;
    public float HealthPercent => currentHealth / maxHealth;
    public bool IsDestroyed => isDestroyed;
}