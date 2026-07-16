using UnityEngine;

/// <summary>
/// Sistema de daño por deformación de malla para vehículos.
/// Enganchar en el objeto que tiene el MeshFilter/MeshRenderer del cuerpo del auto
/// (NO en las ruedas, que quedan fuera de este sistema).
/// </summary>
public class CarMeshDamage : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("MeshFilter de la carrocería a deformar. Si lo dejás vacío, busca uno en este objeto o en sus hijos.")]
    [SerializeField] MeshFilter targetMeshFilter;

    [Header("Deformación")]
    [Tooltip("Radio (en unidades locales del mesh) que afecta cada impacto")]
    public float damageRadius = 0.6f;

    [Tooltip("Cuánto se hunde la malla por unidad de fuerza de impacto")]
    public float damageMultiplier = 0.02f;

    [Tooltip("Desplazamiento máximo acumulado por vértice, para que la malla no se rompa")]
    public float maxVertexDisplacement = 0.35f;

    [Tooltip("Velocidad relativa mínima para que un golpe cuente como daño")]
    public float minImpactForce = 3f;

    [Header("Rendimiento")]
    [Tooltip("Cada cuántos impactos se recalculan bounds (no hace falta todos los frames)")]
    public int recalculateBoundsEvery = 3;

    Mesh workingMesh;
    Vector3[] originalVertices;   // posiciones "sanas" de referencia, nunca se tocan
    Vector3[] displacedVertices;  // estado actual, acumulado
    int impactCount;

    void Awake()
    {
        // Si no lo asignaron a mano en el Inspector, intentamos encontrarlo solos
        if (targetMeshFilter == null)
            targetMeshFilter = GetComponent<MeshFilter>();
        if (targetMeshFilter == null)
            targetMeshFilter = GetComponentInChildren<MeshFilter>();

        if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null)
        {
            Debug.LogError($"[CarMeshDamage] No se encontró un MeshFilter con mesh asignada en '{name}'. " +
                            "Arrastrá la carrocería del auto al campo 'Target Mesh Filter' en el Inspector.", this);
            enabled = false;
            return;
        }

        // Clonamos la malla: nunca deformamos el asset compartido
        Mesh sourceMesh = targetMeshFilter.sharedMesh;
        workingMesh = Instantiate(sourceMesh);
        workingMesh.MarkDynamic(); // le avisa a Unity que esta mesh se va a actualizar seguido
        targetMeshFilter.mesh = workingMesh;

        originalVertices = sourceMesh.vertices; // vértices originales, sin tocar
        displacedVertices = (Vector3[])originalVertices.Clone();
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce) return;

        Transform meshTransform = targetMeshFilter.transform;
        foreach (ContactPoint contact in collision.contacts)
        {
            // Dirección robusta: no dependemos del signo de contact.normal (poco confiable).
            // Usamos la posición real de lo que nos chocó, "prolongada" a través del punto
            // de contacto — así el empuje siempre va hacia adentro del auto, sin ambigüedad.
            Vector3 sourceWorldPos = contact.otherCollider.attachedRigidbody != null
                ? contact.otherCollider.attachedRigidbody.worldCenterOfMass
                : contact.otherCollider.bounds.center;

            Vector3 worldDir = (contact.point - sourceWorldPos).normalized;

            Vector3 localPoint = meshTransform.InverseTransformPoint(contact.point);
            Vector3 localDir = meshTransform.InverseTransformDirection(worldDir);
            ApplyDent(localPoint, localDir, impactForce);
        }

        workingMesh.vertices = displacedVertices;
        workingMesh.RecalculateNormals();

        impactCount++;
        if (impactCount % recalculateBoundsEvery == 0)
            workingMesh.RecalculateBounds();
    }

    void ApplyDent(Vector3 localPoint, Vector3 localDir, float force)
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            float dist = Vector3.Distance(originalVertices[i], localPoint);
            if (dist > damageRadius) continue;

            // Falloff suave tipo "smoothstep": arranca y termina con pendiente cero,
            // así el borde del abollón se funde con la carrocería sin escalón ni pico.
            float t = 1f - (dist / damageRadius);
            float falloff = t * t * (3f - 2f * t);

            Vector3 currentOffset = displacedVertices[i] - originalVertices[i];
            Vector3 newOffset = currentOffset + localDir * force * damageMultiplier * falloff;

            // Clamp: evita que la malla se rompa con impactos repetidos en el mismo punto
            if (newOffset.magnitude > maxVertexDisplacement)
                newOffset = newOffset.normalized * maxVertexDisplacement;

            displacedVertices[i] = originalVertices[i] + newOffset;
        }
    }

    /// <summary>Útil si querés resetear el auto entre rondas sin recargar la escena.</summary>
    public void ResetDamage()
    {
        displacedVertices = (Vector3[])originalVertices.Clone();
        workingMesh.vertices = displacedVertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
        impactCount = 0;
    }
}