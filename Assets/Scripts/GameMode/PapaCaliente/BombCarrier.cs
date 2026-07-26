using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
public class BombCarrier : MonoBehaviour
{
    [Header("Transferencia")]
    public float transferCooldown = 0.6f;

    VehicleHealth ownHealth;
    bool isCarrying = false;
    float lastTransferAttempt = -999f;

    void Awake() => ownHealth = GetComponent<VehicleHealth>();

    public void SetCarrying(bool carrying)
    {
        isCarrying = carrying;
        if (carrying) lastTransferAttempt = Time.time;
    }

    // Llamado desde BombTransferRelay, ya no desde OnCollisionEnter/Stay propio
    public void HandleTransferTrigger(Collider other)
    {
        if (!isCarrying) return;
        if (Time.time - lastTransferAttempt < transferCooldown) return;

        // Como ahora es un trigger (no colisión física), buscamos VehicleHealth
        // en el padre del collider que tocó la zona
        VehicleHealth otherHealth = other.GetComponentInParent<VehicleHealth>();
        if (otherHealth == null || otherHealth == ownHealth || otherHealth.IsDestroyed) return;

        Rigidbody otherRb = otherHealth.GetComponent<Rigidbody>();
        float impactForce = otherRb != null
            ? (otherRb.linearVelocity - GetComponent<Rigidbody>().linearVelocity).magnitude
            : 0f;

        bool success = BombCarrierManager.Instance != null
                       && BombCarrierManager.Instance.TryTransferBomb(ownHealth, otherHealth, impactForce);

        if (success) lastTransferAttempt = Time.time;
    }
}