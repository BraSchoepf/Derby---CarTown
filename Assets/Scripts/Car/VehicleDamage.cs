using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class VehicleDamage : MonoBehaviour
{
    [Header("Referencias")]
    public MeshFilter targetMesh; // el mesh de la carrocería a deformar

    [Header("Config de daño")]
    public float damageRadius = 0.8f;       // radio de vértices afectados por impacto
    public float damageMultiplier = 0.02f;  // qué tan profundo abolla por unidad de fuerza
    public float maxDisplacementPerVertex = 0.4f; // límite para que no se "trague" el mesh
    public float minCollisionForce = 3f;    // fuerza mínima para generar daño (evita micro-golpes)
    public LayerMask damageableLayers = ~0; // qué layers pueden generar daño (excluir piso, por ej.)
    public float damageCooldown = 0.15f; // segundos mínimos entre aplicaciones de daño por colisión sostenida
    float lastDamageTime = -999f;

    Mesh mesh;
    Vector3[] originalVertices;
    Vector3[] displacedVertices;
    float[] vertexDamageAccumulated; // cuánto ya se abolló cada vértice, para el clamp

    void Start()
    {
        if (targetMesh == null)
            targetMesh = GetComponent<MeshFilter>();

        // Clonamos el mesh para no modificar el asset original compartido
        mesh = Instantiate(targetMesh.mesh);
        targetMesh.mesh = mesh;

        originalVertices = mesh.vertices;
        displacedVertices = (Vector3[])originalVertices.Clone();
        vertexDamageAccumulated = new float[originalVertices.Length];
    }

    void OnCollisionEnter(Collision collision)
    {
        ProcessCollision(collision);
        lastDamageTime = Time.time;
    }


    void OnCollisionStay(Collision collision)
    {
        // Solo re-aplicar daño cada cierto intervalo, no en cada FixedUpdate
        if (Time.time - lastDamageTime < damageCooldown)
            return;

        ProcessCollision(collision);
        lastDamageTime = Time.time;
    }

    void ProcessCollision(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & damageableLayers) == 0)
            return;

        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minCollisionForce)
            return;

        Transform meshTransform = targetMesh.transform; // el transform REAL del mesh, no this.transform

        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 localPoint = meshTransform.InverseTransformPoint(contact.point);
            Vector3 localImpactDir = meshTransform.InverseTransformDirection(contact.normal).normalized;
            ApplyDeformation(localPoint, localImpactDir, impactForce);
        }

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void ApplyDeformation(Vector3 localImpactPoint, Vector3 impactDirection, float force)
    {
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float dist = Vector3.Distance(displacedVertices[i], localImpactPoint);
            if (dist > damageRadius) continue;

            // Falloff suave (smoothstep) en vez de lineal — evita el borde "cuadrado"
            float t = 1f - (dist / damageRadius);
            float falloff = t * t * (3f - 2f * t); // smoothstep

            float pushAmount = force * damageMultiplier * falloff;

            float remainingRoom = maxDisplacementPerVertex - vertexDamageAccumulated[i];
            if (remainingRoom <= 0f) continue;
            pushAmount = Mathf.Min(pushAmount, remainingRoom);

            // Un poco de ruido por vértice para romper la uniformidad "perfecta"
            float noise = 1f + Random.Range(-0.15f, 0.15f);

            // Empujamos en la dirección REAL del impacto (contact.normal invertida),
            // no alejando del punto — esto es lo que da el look "abollado hacia adentro"
            displacedVertices[i] += impactDirection * pushAmount * noise;
            vertexDamageAccumulated[i] += pushAmount;
        }
    }

    // Opcional: resetear el mesh (útil para testing o "repair" en el juego)
    public void ResetDamage()
    {
        displacedVertices = (Vector3[])originalVertices.Clone();
        for (int i = 0; i < vertexDamageAccumulated.Length; i++)
            vertexDamageAccumulated[i] = 0f;

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}