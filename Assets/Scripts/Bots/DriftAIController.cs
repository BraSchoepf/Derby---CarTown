using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(CarController))]
public class DriftAIController : MonoBehaviour
{
    public RaceManager.RacerProgress progress;
    public RaceManager raceManager;

    [Header("Anticipación")]
    public float cornerCutFactor = 0.3f;
    public float lookAheadTriggerDistance = 14f;

    [Header("Drift activo")]
    [Tooltip("Ángulo de curva a partir del cual la IA intenta driftear en vez de solo girar")]
    public float driftTriggerCornerAngle = 30f;
    public float driftEntrySpeed = 12f; // velocidad mínima para intentar entrar en drift
    public float driftLookAhead = 20f;

    [Header("Anti-atasco")]
    public float stuckSpeedThreshold = 1.5f;
    public float stuckTimeToTrigger = 1.5f;
    public float reverseDuration = 1f;
    public float reverseThrottle = -0.8f;

    VehicleHealth ownHealth;
    CarController carController;
    Rigidbody rb;

    float stuckTimer;
    float reverseTimer;
    bool isReversingOut;
    float reverseSteerDirection;

    void Awake()
    {
        ownHealth = GetComponent<VehicleHealth>();
        carController = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        carController.isAIControlled = true;
        reverseSteerDirection = Random.value > 0.5f ? 1f : -1f;
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

        if (raceManager == null || raceManager.activeCourse == null) return;
        var checkpoints = raceManager.activeCourse.checkpoints;
        if (checkpoints == null || checkpoints.Length == 0) return;

        int idx = progress.currentCheckpointIndex;
        Transform currentCp = checkpoints[idx];
        Transform nextCp = checkpoints[(idx + 1) % checkpoints.Length];

        Vector3 targetPoint = GetLookAheadTarget(currentCp, nextCp);
        float cornerAngle = GetUpcomingCornerAngle(idx, checkpoints);
        float distToCheckpoint = Vector3.Distance(transform.position, currentCp.position);

        DriveWithDriftIntent(targetPoint, cornerAngle, distToCheckpoint);
    }

    Vector3 GetLookAheadTarget(Transform currentCp, Transform nextCp)
    {
        float distToCurrent = Vector3.Distance(transform.position, currentCp.position);
        if (distToCurrent > lookAheadTriggerDistance) return currentCp.position;

        float t = Mathf.Clamp01(1f - (distToCurrent / lookAheadTriggerDistance)) * cornerCutFactor;
        return Vector3.Lerp(currentCp.position, nextCp.position, t);
    }

    float GetUpcomingCornerAngle(int idx, Transform[] checkpoints)
    {
        int nextIdx = (idx + 1) % checkpoints.Length;
        int nextNextIdx = (idx + 2) % checkpoints.Length;

        Vector3 dirA = checkpoints[nextIdx].position - checkpoints[idx].position;
        Vector3 dirB = checkpoints[nextNextIdx].position - checkpoints[nextIdx].position;
        dirA.y = 0f; dirB.y = 0f;
        if (dirA.sqrMagnitude < 0.01f || dirB.sqrMagnitude < 0.01f) return 0f;

        return Vector3.Angle(dirA.normalized, dirB.normalized);
    }

    void DriveWithDriftIntent(Vector3 targetPoint, float cornerAngle, float distToCheckpoint)
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float steer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        float currentSpeed = carController.CurrentSpeed;

        // A diferencia de RaceAIController: NO frena en curvas, busca ENTRAR en drift
        bool wantsToDrift = cornerAngle > driftTriggerCornerAngle
                             && currentSpeed > driftEntrySpeed
                             && distToCheckpoint < driftLookAhead;

        float throttle = Mathf.Abs(angleToTarget) > 150f ? 0.3f : 1f;
        bool handbrake = wantsToDrift;

        carController.SetAIInput(throttle, steer, handbrake);
    }

    void UpdateStuckDetection()
    {
        bool tryingToMove = rb.linearVelocity.magnitude < stuckSpeedThreshold;
        if (tryingToMove)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeToTrigger)
            {
                isReversingOut = true;
                reverseTimer = reverseDuration;
                reverseSteerDirection = Random.value > 0.5f ? 1f : -1f;
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;
    }

    void HandleReverseOut()
    {
        reverseTimer -= Time.deltaTime;
        carController.SetAIInput(reverseThrottle, reverseSteerDirection, false);
        if (reverseTimer <= 0f) isReversingOut = false;
    }
}