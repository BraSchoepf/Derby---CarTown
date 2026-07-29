using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(CarController))]
public class RaceAIController : MonoBehaviour
{
    public RaceManager.RacerProgress progress;
    public RaceManager raceManager;
    public AIWaypointPath waypointPath;

    [Header("Anticipación de curva")]
    [Tooltip("Cuánto se adelanta el punto objetivo hacia el próximo checkpoint, para suavizar la trayectoria")]
    public float cornerCutFactor = 0.35f;
    [Tooltip("Distancia mínima para empezar a mirar el checkpoint siguiente")]
    public float lookAheadTriggerDistance = 16f;

    [Header("Frenado en curvas")]
    [Tooltip("Ángulo (grados) entre el segmento actual y el próximo a partir del cual se considera 'curva cerrada'")]
    public float sharpCornerAngle = 40f;
    [Tooltip("Velocidad objetivo al tomar una curva cerrada")]
    public float corneringSpeedLimit = 8f;
    [Tooltip("Distancia antes del checkpoint donde empieza a frenar para una curva cerrada")]
    public float brakingLookAhead = 20f;

    [Header("Drift asistido en curvas muy cerradas")]
    public float driftCornerAngleThreshold = 70f;
    public float driftMinSpeed = 10f;

    [Header("Mantenerse en pista")]
    [Tooltip("Distancia perpendicular a la línea de checkpoints por encima de la cual se corrige agresivamente")]
    public float maxTrackDeviation = 8f;

    [Header("Anti-atasco")]
    public float stuckSpeedThreshold = 1.5f;
    public float stuckTimeToTrigger = 1.5f;
    public float reverseDuration = 1.5f;
    public float reverseThrottle = -0.8f;
    public int maxReverseAttemptsBeforeRespawn = 2;


    float stuckTimer;
    float reverseTimer;
    bool isReversingOut;
    float reverseSteerDirection;
    int reverseAttemptCount = 0;

    VehicleHealth ownHealth;
    CarController carController;
    Rigidbody rb;

    int currentNodeIndex = 0;

    void Awake()
    {
        ownHealth = GetComponent<VehicleHealth>();
        carController = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        carController.isAIControlled = true;
        carController.autoRespawnWhenStuck = false;
        reverseSteerDirection = Random.value > 0.5f ? 1f : -1f;
    }

    void Start()
    {
        if (waypointPath != null)
            currentNodeIndex = waypointPath.GetClosestNodeIndex(transform.position);
    }

    void Update()
    {
        if (ownHealth.IsDestroyed || progress == null || progress.finished)
        {
            carController.SetAIInput(0f, 0f, false);
            return;
        }

        UpdateStuckDetection();
        if (isReversingOut) { HandleReverseOut(); return; }

        if (waypointPath == null || waypointPath.NodeCount == 0) return;

        AdvanceNodeIfClose();

        Transform currentNode = waypointPath.GetNode(currentNodeIndex);
        Transform nextNode = waypointPath.GetNode(currentNodeIndex + 1);
        if (currentNode == null || nextNode == null) return;

        Vector3 targetPoint = GetLookAheadTarget(currentNode, nextNode);
        float cornerAngle = GetUpcomingCornerAngle();
        float distToNode = Vector3.Distance(transform.position, currentNode.position);

        DriveToward(targetPoint, currentNode.position, cornerAngle, distToNode);
    }

    void AdvanceNodeIfClose()
    {
        Transform currentNode = waypointPath.GetNode(currentNodeIndex);
        if (currentNode == null) return;

        float dist = Vector3.Distance(transform.position, currentNode.position);
        if (dist < lookAheadTriggerDistance * 0.5f)
            currentNodeIndex++;
    }

    Vector3 GetLookAheadTarget(Transform currentNode, Transform nextNode)
    {
        float distToCurrent = Vector3.Distance(transform.position, currentNode.position);
        if (distToCurrent > lookAheadTriggerDistance) return currentNode.position;

        float t = Mathf.Clamp01(1f - (distToCurrent / lookAheadTriggerDistance)) * cornerCutFactor;
        return Vector3.Lerp(currentNode.position, nextNode.position, t);
    }

    float GetUpcomingCornerAngle()
    {
        Transform a = waypointPath.GetNode(currentNodeIndex);
        Transform b = waypointPath.GetNode(currentNodeIndex + 1);
        Transform c = waypointPath.GetNode(currentNodeIndex + 2);
        if (a == null || b == null || c == null) return 0f;

        Vector3 dirA = b.position - a.position; dirA.y = 0f;
        Vector3 dirB = c.position - b.position; dirB.y = 0f;
        if (dirA.sqrMagnitude < 0.01f || dirB.sqrMagnitude < 0.01f) return 0f;

        return Vector3.Angle(dirA.normalized, dirB.normalized);
    }

    void DriveToward(Vector3 targetPoint, Vector3 currentNodePos, float upcomingCornerAngle, float distToNode)
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float steer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        float currentSpeed = carController.CurrentSpeed;

        bool approachingSharpCorner = upcomingCornerAngle > sharpCornerAngle && distToNode < brakingLookAhead;

        float throttle;
        bool handbrake = false;

        if (approachingSharpCorner && currentSpeed > corneringSpeedLimit)
        {
            throttle = -0.4f;
        }
        else
        {
            float absAngle = Mathf.Abs(angleToTarget);
            throttle = absAngle > 150f ? 0.3f : 1f;
        }

        if (upcomingCornerAngle > driftCornerAngleThreshold
            && currentSpeed > driftMinSpeed
            && distToNode < brakingLookAhead * 0.6f)
        {
            handbrake = true;
        }

        carController.SetAIInput(throttle, steer, handbrake);
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
    }
    void UpdateStuckDetection()
    {
        bool tryingToMove = rb.linearVelocity.magnitude < stuckSpeedThreshold;
        if (tryingToMove)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeToTrigger)
            {
                reverseAttemptCount++;

                if (reverseAttemptCount > maxReverseAttemptsBeforeRespawn)
                {
                    reverseAttemptCount = 0;
                    RespawnAtCurrentNode(); // ← antes era carController.ForceRespawnAtLastPoint()
                    stuckTimer = 0f;
                    return;
                }

                isReversingOut = true;
                reverseTimer = reverseDuration;
                reverseSteerDirection = Random.value > 0.5f ? 1f : -1f;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
            reverseAttemptCount = 0;
        }
    }

    void HandleReverseOut()
    {
        reverseTimer -= Time.deltaTime;
        carController.SetAIInput(reverseThrottle, reverseSteerDirection, false);
        if (reverseTimer <= 0f) isReversingOut = false;
    }
}