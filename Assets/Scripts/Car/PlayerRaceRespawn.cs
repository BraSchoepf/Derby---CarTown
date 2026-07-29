using UnityEngine;

public class PlayerRaceRespawn : MonoBehaviour
{
    public AIWaypointPath waypointPath;

    int currentNodeIndex = 0;
    float nodeAdvanceCheckInterval = 0.3f;
    float checkTimer = 0f;

    void Start()
    {
        if (waypointPath != null)
            currentNodeIndex = waypointPath.GetClosestNodeIndex(transform.position);
    }

    void Update()
    {
        if (waypointPath == null || waypointPath.NodeCount == 0) return;

        // No hace falta chequear cada frame — el jugador no necesita precisión de IA,
        // solo una referencia razonable de "por dónde va" para el respawn
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            currentNodeIndex = waypointPath.GetClosestNodeIndex(transform.position);
            checkTimer = nodeAdvanceCheckInterval;
        }
    }

    public void RespawnAtCurrentNode()
    {
        Transform node = waypointPath.GetNode(currentNodeIndex);
        Transform nextNode = waypointPath.GetNode(currentNodeIndex + 1);
        if (node == null) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Quaternion facing = nextNode != null
            ? Quaternion.LookRotation((nextNode.position - node.position).normalized, Vector3.up)
            : node.rotation;

        transform.SetPositionAndRotation(node.position, facing);

        CarController controller = GetComponent<CarController>();
    }
}