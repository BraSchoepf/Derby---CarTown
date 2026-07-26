using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(CarController))]
public class RaceAIController : MonoBehaviour
{
    public RaceManager.RacerProgress progress;
    public RaceManager raceManager;

    [Header("Anticipación de curva")]
    [Tooltip("Cuánto se adelanta el punto objetivo hacia el próximo checkpoint, para suavizar la trayectoria")]
    public float cornerCutFactor = 0.35f;
    [Tooltip("Distancia mínima para empezar a mirar el checkpoint siguiente")]
    public float lookAheadTriggerDistance = 12f;

    [Header("Frenado en curvas")]
    [Tooltip("Ángulo (grados) entre el segmento actual y el próximo a partir del cual se considera 'curva cerrada'")]
    public float sharpCornerAngle = 52f;
    [Tooltip("Velocidad objetivo al tomar una curva cerrada")]
    public float corneringSpeedLimit = 12f;
    [Tooltip("Distancia antes del checkpoint donde empieza a frenar para una curva cerrada")]
    public float brakingLookAhead = 18f;

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


    float stuckTimer;
    float reverseTimer;
    bool isReversingOut;
    float reverseSteerDirection;

    VehicleHealth ownHealth;
    CarController carController;
    Rigidbody rb;

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

        if (isReversingOut)
        {
            HandleReverseOut();
            return;
        }

        if (raceManager == null || raceManager.activeCourse == null) return;

        var checkpoints = raceManager.activeCourse.checkpoints;
        if (checkpoints == null || checkpoints.Length == 0) return;

        int idx = progress.currentCheckpointIndex;
        Transform currentCp = checkpoints[idx];
        Transform nextCp = checkpoints[(idx + 1) % checkpoints.Length];

        Vector3 targetPoint = GetLookAheadTarget(currentCp, nextCp);
        float cornerAngle = GetUpcomingCornerAngle(idx, checkpoints);

        DriveToward(targetPoint, currentCp.position, cornerAngle);
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
                reverseSteerDirection = Random.value > 0.5f ? 1f : -1f; // nuevo random cada vez, por si vuelve a trabarse en otro ángulo
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    void HandleReverseOut()
    {
        reverseTimer -= Time.deltaTime;
        carController.SetAIInput(reverseThrottle, reverseSteerDirection, false);

        if (reverseTimer <= 0f)
            isReversingOut = false;
    }

    // Punto objetivo: mezcla entre el checkpoint actual y el siguiente, según qué tan cerca
    // estás del actual — así el auto empieza a girar ANTES de llegar al punto exacto,
    // en vez de apuntar directo al checkpoint y girar de golpe al pasar por encima.
    Vector3 GetLookAheadTarget(Transform currentCp, Transform nextCp)
    {
        float distToCurrent = Vector3.Distance(transform.position, currentCp.position);

        if (distToCurrent > lookAheadTriggerDistance)
            return currentCp.position; // todavía lejos, apuntá directo al checkpoint actual

        float t = Mathf.Clamp01(1f - (distToCurrent / lookAheadTriggerDistance)) * cornerCutFactor;
        return Vector3.Lerp(currentCp.position, nextCp.position, t);
    }

    // Ángulo entre el segmento (checkpoint actual -> siguiente) y (siguiente -> el de después) —
    // cuanto más grande, más cerrada es la curva que se viene.
    float GetUpcomingCornerAngle(int idx, Transform[] checkpoints)
    {
        int nextIdx = (idx + 1) % checkpoints.Length;
        int nextNextIdx = (idx + 2) % checkpoints.Length;

        Vector3 dirA = (checkpoints[nextIdx].position - checkpoints[idx].position);
        Vector3 dirB = (checkpoints[nextNextIdx].position - checkpoints[nextIdx].position);
        dirA.y = 0f; dirB.y = 0f;

        if (dirA.sqrMagnitude < 0.01f || dirB.sqrMagnitude < 0.01f) return 0f;

        return Vector3.Angle(dirA.normalized, dirB.normalized);
    }

    void DriveToward(Vector3 targetPoint, Vector3 currentCheckpointPos, float upcomingCornerAngle)
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float steer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        float currentSpeed = carController.CurrentSpeed;
        float distToCheckpoint = Vector3.Distance(transform.position, currentCheckpointPos);

        // --- Frenado anticipado en curvas cerradas ---
        bool approachingSharpCorner = upcomingCornerAngle > sharpCornerAngle
                                       && distToCheckpoint < brakingLookAhead;

        float throttle;
        bool handbrake = false;

        if (approachingSharpCorner && currentSpeed > corneringSpeedLimit)
        {
            throttle = -0.4f; // frena activamente, no solo suelta el acelerador
        }
        else
        {
            float absAngle = Mathf.Abs(angleToTarget);
            throttle = absAngle > 150f ? 0.3f : 1f;
        }

        // --- Drift asistido en curvas muy cerradas, a velocidad alta ---
        if (upcomingCornerAngle > driftCornerAngleThreshold
            && currentSpeed > driftMinSpeed
            && distToCheckpoint < brakingLookAhead * 0.6f)
        {
            handbrake = true;
        }

        // --- Corrección si se alejó demasiado de la línea de carrera ---
        float perpendicularDeviation = GetPerpendicularDeviation(currentCheckpointPos, targetPoint);
        if (Mathf.Abs(perpendicularDeviation) > maxTrackDeviation)
        {
            steer = Mathf.Clamp(steer * 1.5f, -1f, 1f); // corrección más agresiva
        }

        carController.SetAIInput(throttle, steer, handbrake);
    }

    float GetPerpendicularDeviation(Vector3 checkpointPos, Vector3 lookAheadPos)
    {
        Vector3 trackDir = (lookAheadPos - checkpointPos);
        trackDir.y = 0f;
        if (trackDir.sqrMagnitude < 0.01f) return 0f;
        trackDir.Normalize();

        Vector3 toCar = transform.position - checkpointPos;
        toCar.y = 0f;

        Vector3 trackRight = Vector3.Cross(Vector3.up, trackDir);
        return Vector3.Dot(toCar, trackRight);
    }
}