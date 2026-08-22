using UnityEngine;

public class CarMeshDamage : MonoBehaviour
{
    [Header("Referencia")]
    [SerializeField] MeshFilter targetMeshFilter;

    [Header("Vínculo con salud")]
    [Tooltip("Si lo dejás vacío, busca un VehicleHealth en este objeto o en el padre")]
    public VehicleHealth vehicleHealth;
    [Tooltip("Mapea % de vida PERDIDA (0 = full vida, 1 = destruido) a un multiplicador de deformación. " +
             "A vida llena, los golpes dejan marcas leves; cerca de morir, cada golpe se nota mucho más.")]
    public AnimationCurve damageScaleByHealthLost = AnimationCurve.Linear(0f, 0.35f, 1f, 1.5f);

    [Header("Deformación")]
    public float damageRadius = 0.6f;
    public float damageMultiplier = 0.02f;
    public float maxVertexDisplacement = 0.35f;
    public float minImpactForce = 3f;

    [Header("Rendimiento")]
    public int recalculateBoundsEvery = 3;

    Mesh workingMesh;
    Vector3[] originalVertices;
    Vector3[] displacedVertices;
    int impactCount;

    void Awake()
    {
        if (targetMeshFilter == null) targetMeshFilter = GetComponent<MeshFilter>();
        if (targetMeshFilter == null) targetMeshFilter = GetComponentInChildren<MeshFilter>();

        if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null)
        {
            Debug.LogError($"[CarMeshDamage] No se encontró un MeshFilter con mesh asignada en '{name}'.", this);
            enabled = false;
            return;
        }

        if (vehicleHealth == null) vehicleHealth = GetComponent<VehicleHealth>();
        if (vehicleHealth == null) vehicleHealth = GetComponentInParent<VehicleHealth>();
        if (vehicleHealth == null)
            Debug.LogWarning($"[CarMeshDamage] No se encontró VehicleHealth en '{name}' — la deformación no va a escalar con la vida.", this);

        Mesh sourceMesh = targetMeshFilter.sharedMesh;
        workingMesh = Instantiate(sourceMesh);
        workingMesh.MarkDynamic();
        targetMeshFilter.mesh = workingMesh;

        originalVertices = sourceMesh.vertices;
        displacedVertices = (Vector3[])originalVertices.Clone();
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce) return;

        // A vida llena, healthLostPercent = 0 → deformación atenuada.
        // Cerca de destruirse, healthLostPercent → 1 → deformación acentuada.
        float healthLostPercent = vehicleHealth != null ? 1f - vehicleHealth.HealthPercent : 1f;
        float damageScale = damageScaleByHealthLost.Evaluate(Mathf.Clamp01(healthLostPercent));

        float scaledForce = impactForce * damageScale;
        float scaledMaxDisplacement = maxVertexDisplacement * damageScale;

        Transform meshTransform = targetMeshFilter.transform;
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 sourceWorldPos = contact.otherCollider.attachedRigidbody != null
                ? contact.otherCollider.attachedRigidbody.worldCenterOfMass
                : contact.otherCollider.bounds.center;

            Vector3 worldDir = (contact.point - sourceWorldPos).normalized;
            Vector3 localPoint = meshTransform.InverseTransformPoint(contact.point);
            Vector3 localDir = meshTransform.InverseTransformDirection(worldDir);

            ApplyDent(localPoint, localDir, scaledForce, scaledMaxDisplacement);
        }

        workingMesh.vertices = displacedVertices;
        workingMesh.RecalculateNormals();

        impactCount++;
        if (impactCount % recalculateBoundsEvery == 0)
            workingMesh.RecalculateBounds();
    }

    void ApplyDent(Vector3 localPoint, Vector3 localDir, float force, float maxDisplacement)
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            float dist = Vector3.Distance(originalVertices[i], localPoint);
            if (dist > damageRadius) continue;

            float t = 1f - (dist / damageRadius);
            float falloff = t * t * (3f - 2f * t);

            Vector3 currentOffset = displacedVertices[i] - originalVertices[i];
            Vector3 newOffset = currentOffset + localDir * force * damageMultiplier * falloff;

            if (newOffset.magnitude > maxDisplacement)
                newOffset = newOffset.normalized * maxDisplacement;

            displacedVertices[i] = originalVertices[i] + newOffset;
        }
    }

    public void ResetDamage()
    {
        displacedVertices = (Vector3[])originalVertices.Clone();
        workingMesh.vertices = displacedVertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
        impactCount = 0;
    }
}