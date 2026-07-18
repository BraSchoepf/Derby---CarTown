using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class VehicleHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    float currentHealth;

    [Header("Multiplicadores por zona")]
    public float frontRearMultiplier = 1f;
    public float sideMultiplier = 2.2f;

    [Header("Config de daño")]
    public float minForceForDamage = 3f;
    public float forceToDamageRatio = 1.5f;
    [Tooltip("Cuánto pesa 'quién te embistió' vs tu propia velocidad al calcular tu daño recibido")]
    [Range(0f, 1f)] public float attackerWeightFactor = 0.8f;

    [Header("Layer de otros autos")]
    public LayerMask carLayer;

    public event Action<float, float> OnHealthChanged;
    public event Action OnVehicleDestroyed;

    bool isDestroyed = false;
    Rigidbody rb;
    Dictionary<Collider, VehicleHitZone.Zone> zoneLookup;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        BuildZoneLookup();
    }

    void BuildZoneLookup()
    {
        zoneLookup = new Dictionary<Collider, VehicleHitZone.Zone>();
        foreach (var hitZone in GetComponentsInChildren<VehicleHitZone>())
        {
            Collider col = hitZone.GetComponent<Collider>();
            if (col != null) zoneLookup[col] = hitZone.zone;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDestroyed) return;
        if (((1 << collision.gameObject.layer) & carLayer) == 0) return;

        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null) return;

        ContactPoint contact = collision.contacts[0];

        VehicleHitZone hitZone = contact.thisCollider.GetComponent<VehicleHitZone>();
        float zoneMultiplier = hitZone != null ? hitZone.damageMultiplier : frontRearMultiplier;

        float closingSpeed = CalculateClosingSpeed(otherRb, contact);
        if (closingSpeed < minForceForDamage) return;

        float force = CalculateAsymmetricForce(collision, otherRb, contact);
        ApplyDamage(force * forceToDamageRatio * zoneMultiplier);
    }

    // La clave de la asimetría: no usamos relativeVelocity (simétrica),
    // usamos cuánto se movía el OTRO auto hacia mí en el momento del choque.
    float CalculateAsymmetricForce(Collision collision, Rigidbody otherRb, ContactPoint contact)
    {
        // contact.normal apunta alejándose de la superficie de ESTE collider,
        // así que "-contact.normal" es la dirección hacia mí desde afuera
        float otherSpeedTowardMe = Mathf.Max(0f, Vector3.Dot(otherRb.linearVelocity, -contact.normal));
        float mySpeedTowardImpact = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, contact.normal));

        // Mezcla ponderada: si el otro venía fuerte hacia mí, yo soy la "víctima" → más daño.
        // Si yo era el que se metía contra algo quieto, tomo menos (fue más "mi culpa" que del otro).
        float weightedForce = (otherSpeedTowardMe * attackerWeightFactor)
                             + (mySpeedTowardImpact * (1f - attackerWeightFactor));

        return weightedForce;
    }

    float GetZoneMultiplier(Collider hitCollider)
    {
        if (hitCollider != null && zoneLookup.TryGetValue(hitCollider, out var zone))
        {
            return (zone == VehicleHitZone.Zone.Left || zone == VehicleHitZone.Zone.Right)
                ? sideMultiplier
                : frontRearMultiplier;
        }
        return frontRearMultiplier; // fallback si no se encontró el collider en el lookup
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

    float CalculateClosingSpeed(Rigidbody otherRb, ContactPoint contact)
    {
        float otherSpeedTowardMe = Mathf.Max(0f, Vector3.Dot(otherRb.linearVelocity, -contact.normal));
        float mySpeedTowardImpact = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, contact.normal));
        return otherSpeedTowardMe + mySpeedTowardImpact;
    }

    public float CurrentHealth => currentHealth;
    public float HealthPercent => currentHealth / maxHealth;
    public bool IsDestroyed => isDestroyed;
}