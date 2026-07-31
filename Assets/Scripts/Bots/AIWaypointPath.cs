using UnityEngine;

public class AIWaypointPath : MonoBehaviour
{
    [Tooltip("Arrastrá los nodos EN ORDEN. Podés tener muchos, uno cada pocos metros en curvas cerradas.")]
    public Transform[] nodes;
    [Tooltip("Si está tildado, el último nodo conecta de vuelta al primero (circuito cerrado)")]
    public bool loop = true;

    [Header("Debug visual")]
    public Color lineColor = Color.cyan;
    public Color nodeColor = Color.yellow;
    public float nodeGizmoRadius = 1f;
    public bool showNodeIndices = true;

    public int NodeCount => nodes != null ? nodes.Length : 0;

    // --- Progreso continuo a lo largo del path ---
    float[] cumulativeDistances;
    float totalLength = -1f;

    public float TotalLength
    {
        get { EnsureCache(); return totalLength; }
    }

    public struct PathPointInfo
    {
        public float progress;    // distancia acumulada desde el inicio del path hasta el punto proyectado
        public Vector3 direction; // dirección del tramo en ese punto (para wrong-way)
    }

    void EnsureCache()
    {
        if (cumulativeDistances != null) return;

        int count = NodeCount;
        cumulativeDistances = new float[count];
        float acc = 0f;
        int segCount = loop ? count : count - 1;

        for (int i = 0; i < count; i++)
        {
            cumulativeDistances[i] = acc;
            if (i < segCount)
            {
                Transform a = nodes[i];
                Transform b = nodes[(i + 1) % count];
                if (a != null && b != null)
                    acc += Vector3.Distance(a.position, b.position);
            }
        }
        totalLength = acc;
    }

    public PathPointInfo GetClosestPointInfo(Vector3 worldPosition)
    {
        EnsureCache();

        PathPointInfo best = new PathPointInfo();
        float bestDistSqr = float.MaxValue;

        int segCount = loop ? nodes.Length : nodes.Length - 1;

        for (int i = 0; i < segCount; i++)
        {
            Transform a = nodes[i];
            Transform b = nodes[(i + 1) % nodes.Length];
            if (a == null || b == null) continue;

            Vector3 segStart = a.position;
            Vector3 segEnd = b.position;
            Vector3 segVector = segEnd - segStart;
            float segLen = segVector.magnitude;
            if (segLen < 0.001f) continue;

            Vector3 segDir = segVector / segLen;
            Vector3 toPoint = worldPosition - segStart;
            float t = Mathf.Clamp01(Vector3.Dot(toPoint, segDir) / segLen);
            Vector3 projected = segStart + segDir * (t * segLen);

            float distSqr = (worldPosition - projected).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                best.progress = cumulativeDistances[i] + t * segLen;
                best.direction = segDir;
            }
        }

        return best;
    }

    public Transform GetNode(int index)
    {
        if (nodes == null || nodes.Length == 0) return null;
        int wrapped = ((index % nodes.Length) + nodes.Length) % nodes.Length;
        return nodes[wrapped];
    }

    public int GetClosestNodeIndex(Vector3 worldPosition)
    {
        if (nodes == null || nodes.Length == 0) return -1;
        int closest = 0;
        float closestDist = float.MaxValue;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null) continue;
            float dist = Vector3.SqrMagnitude(nodes[i].position - worldPosition);
            if (dist < closestDist) { closestDist = dist; closest = i; }
        }
        return closest;
    }

    void OnDrawGizmos()
    {
        if (nodes == null || nodes.Length == 0) return;

        Gizmos.color = nodeColor;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null) continue;
            Gizmos.DrawWireSphere(nodes[i].position, nodeGizmoRadius);
#if UNITY_EDITOR
            if (showNodeIndices)
                UnityEditor.Handles.Label(nodes[i].position + Vector3.up * (nodeGizmoRadius + 0.5f), i.ToString());
#endif
        }

        Gizmos.color = lineColor;
        int count = loop ? nodes.Length : nodes.Length - 1;
        for (int i = 0; i < count; i++)
        {
            Transform a = nodes[i];
            Transform b = nodes[(i + 1) % nodes.Length];
            if (a == null || b == null) continue;

            Gizmos.DrawLine(a.position, b.position);
            Vector3 mid = (a.position + b.position) * 0.5f;
            Vector3 dir = (b.position - a.position).normalized;
            Vector3 right = Quaternion.Euler(0, 150, 0) * dir;
            Vector3 left = Quaternion.Euler(0, -150, 0) * dir;
            Gizmos.DrawLine(mid, mid + right * 1.5f);
            Gizmos.DrawLine(mid, mid + left * 1.5f);
        }
    }
}