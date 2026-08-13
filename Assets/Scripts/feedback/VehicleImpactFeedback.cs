using UnityEngine;
using Unity.Cinemachine;

public class VehicleImpactFeedback : MonoBehaviour
{
    [Header("Filtro de colisión")]
    public LayerMask carLayer; 

    [Header("Config general")]
    public float minForceForFeedback = 3f;
    public float heavyImpactThreshold = 10f;

    [Header("Screen Shake")]
    public CinemachineImpulseSource impulseSource;
    public float shakeForceMultiplier = 0.15f;

    [Header("Hit Stop")]
    public bool enableHitStop = true;
    public float hitStopDuration = 0.04f;

    [Header("Física extra")]
    public float extraTorqueMultiplier = 0.3f;

    [Header("Knockback (valores por defecto, se sobrescriben según el modo)")]
    public float knockbackMultiplier = 4f;
    public float maxKnockbackForce = 15f;

    Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void Start()
    {
        ApplyModeSettings();
    }

    void ApplyModeSettings()
    {
        GameModeSO mode = GameSession.Instance != null ? GameSession.Instance.chosenGameMode : null;

        if (mode == null || !mode.enableKnockbackFeedback)
        {
            enabled = false;
            return;
        }

        knockbackMultiplier = mode.knockbackMultiplierOverride;
        maxKnockbackForce = mode.maxKnockbackForceOverride;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & carLayer) == 0)
            return;

        float force = collision.relativeVelocity.magnitude;
        if (force < minForceForFeedback) return;

        ContactPoint contact = collision.contacts[0];
        bool isHeavy = force > heavyImpactThreshold;

        if (impulseSource != null)
            impulseSource.GenerateImpulse(contact.normal * force * shakeForceMultiplier);

        if (enableHitStop && isHeavy && HitStopManager.Instance != null)
            HitStopManager.Instance.DoHitStop(hitStopDuration);

        Vector3 randomTorque = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        rb.AddTorque(randomTorque * force * extraTorqueMultiplier, ForceMode.Impulse);

        ApplyKnockback(contact, force);
    }

    void ApplyKnockback(ContactPoint contact, float force)
    {
        Vector3 knockbackDir = (transform.position - contact.point);
        knockbackDir.y = 0;
        knockbackDir.Normalize();

        float knockbackForce = Mathf.Min(force * knockbackMultiplier, maxKnockbackForce);
        rb.AddForce(knockbackDir * knockbackForce, ForceMode.VelocityChange);
    }
}