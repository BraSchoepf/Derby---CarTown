using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CenterOfMassGizmo : MonoBehaviour
{
    [Header("Referencia")]
    public CarController carController;

    [Header("Visual")]
    public float gizmoRadius = 0.15f;
    public Color gizmoColor = Color.red;
    public bool showLabel = true;

    Rigidbody rb;

    void OnDrawGizmos()
    {
        if (carController == null) carController = GetComponent<CarController>();
        if (carController == null || carController.stats == null) return;

        rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        // Posición real del centro de masa en mundo:
        // pivote del auto + el offset configurado en el CarStatsSO
        Vector3 worldCoM = transform.TransformPoint(carController.stats.centerOfMassOffset);

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(worldCoM, gizmoRadius);
        Gizmos.DrawWireSphere(worldCoM, gizmoRadius * 1.5f);

        // Línea desde el pivote del auto hasta el centro de masa, para ver la relación
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, worldCoM);

#if UNITY_EDITOR
        if (showLabel)
        {
            UnityEditor.Handles.Label(worldCoM + Vector3.up * (gizmoRadius + 0.2f),
                $"CoM: {carController.stats.centerOfMassOffset}");
        }
#endif
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (carController == null || carController.stats == null) return;

        // --- Handle interactivo para mover el CoM configurado en el SO ---
        Vector3 worldCoM = transform.TransformPoint(carController.stats.centerOfMassOffset);

        UnityEditor.EditorGUI.BeginChangeCheck();
        Vector3 newWorldPos = UnityEditor.Handles.PositionHandle(worldCoM, transform.rotation);

        if (UnityEditor.EditorGUI.EndChangeCheck())
        {
            UnityEditor.Undo.RecordObject(carController.stats, "Move Center of Mass");
            Vector3 newLocalOffset = transform.InverseTransformPoint(newWorldPos);
            carController.stats.centerOfMassOffset = newLocalOffset;
            UnityEditor.EditorUtility.SetDirty(carController.stats);
        }

        // --- Esfera verde: el CoM REAL que está usando el Rigidbody en este momento (solo en Play) ---
        if (Application.isPlaying)
        {
            Rigidbody rbRuntime = GetComponent<Rigidbody>();
            if (rbRuntime != null)
            {
                Vector3 worldCoMRuntime = transform.TransformPoint(rbRuntime.centerOfMass);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(worldCoMRuntime, gizmoRadius * 2f);
                UnityEditor.Handles.Label(worldCoMRuntime + Vector3.down * 0.3f, "CoM real (Rigidbody, en Play)");
            }
        }
    }
#endif
}
