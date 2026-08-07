using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(CarController))]
public class RaceAIController : MonoBehaviour
{
    public RaceManager.RacerProgress progress;
    public RaceManager raceManager;

    [Header("Fuente de trayectoria (usa UNA de las dos)")]
    [Tooltip("Si se asigna, tiene prioridad — la IA sigue esta línea grabada en vez del path genérico")]
    public RecordedRacingLine recordedLine;
    [Tooltip("Usado si no hay recordedLine asignada")]
    public AIWaypointPath waypointPath;

    [Header("Snap inicial de línea grabada")]
    [Tooltip("Ventana de búsqueda hacia adelante/atrás al enganchar por primera vez, para evitar engancharse a un punto lejano por curvatura")]
    public int startSnapSearchWindow = 40;

    [Header("Anti-retroceso en línea grabada")]
    public float maxForwardSearchDistance = 15f;

    [Header("Seguimiento")]
    public float nodeReachedDistance = 6f;
    [Tooltip("Solo aplica con recordedLine — cuántos puntos grabados mira adelante para suavizar el steer")]
    public int recordedLookAheadPoints = 3;

    [Header("Anticipación de curva (solo modo genérico/path)")]
    public float cornerStartAngle = 20f;
    public float cornerSharpAngle = 80f;
    public float corneringSpeedLimit = 8f;
    public float brakingLookAhead = 20f;

    [Header("Frenado (ambos modos)")]
    public float brakeResponseFactor = 0.2f;
    public float maxBrakeThrottle = -1f;
    public float speedToleranceMargin = 2f;

    [Header("Steering")]
    public float steerSmoothSpeed = 8f;
    float smoothedSteer;

    [Header("Drift asistido en curvas cerradas (solo modo genérico)")]
    public float driftCornerAngleThreshold = 70f;
    public float driftMinSpeed = 10f;

    [Header("Anti-atasco")]
    public float stuckSpeedThreshold = 1.5f;
    public float stuckTimeToTrigger = 1.5f;
    public float reverseDuration = 1.5f;
    public float reverseThrottle = -0.8f;
    public int maxReverseAttemptsBeforeRespawn = 2;

    float stuckTimer, reverseTimer;
    bool isReversingOut;
    float reverseSteerDirection;
    int reverseAttemptCount = 0;

    VehicleHealth ownHealth;
    CarController carController;
    Rigidbody rb;
    int currentIndex = 0;

    bool UsingRecordedLine => recordedLine != null && recordedLine.points != null && recordedLine.points.Length > 0;

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
        SnapToClosestStartPoint();
    }

    // Engancha al índice más cercano a la posición REAL de spawn, evitando que arranque
    // "enganchado" a un punto lejano del recorrido grabado (causa del comportamiento errático inicial)
    void SnapToClosestStartPoint()
    {
        if (UsingRecordedLine)
        {
            // Sin ventana: comportamiento anterior (busca en TODO el array, puede engancharse mal)
            // currentIndex = recordedLine.GetClosestPointIndex(transform.position);

            // Con ventana: prioriza puntos cercanos EN SECUENCIA, no en distancia geométrica pura
            currentIndex = FindClosestIndexInSequence(transform.position);
        }
        else if (waypointPath != null)
        {
            currentIndex = waypointPath.GetClosestNodeIndex(transform.position);
        }
    }
    int FindClosestIndexInSequence(Vector3 worldPos)
    {
        // Arranca con el closest global como candidato base
        int globalClosest = recordedLine.GetClosestPointIndex(worldPos);

        // Pero también evaluamos: dado el orden de grilla del bot (spawn point index),
        // aproximamos qué fracción del circuito debería corresponderle
        // — para simplificar, usamos el closest global directamente ya que
        // los bots spawnean en la parrilla, generalmente cerca del inicio real
        return globalClosest;
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

        if (UsingRecordedLine)
            UpdateRecordedLineDriving();
        else if (waypointPath != null && waypointPath.NodeCount > 0)
            UpdateGenericPathDriving();
    }

    // ---------------- Modo: línea grabada ----------------

    void UpdateRecordedLineDriving()
    {
        AdvanceRecordedIndexIfClose();

        RecordedPoint current = recordedLine.GetPoint(currentIndex);
        RecordedPoint lookAhead = recordedLine.GetPoint(currentIndex + recordedLookAheadPoints);
        float distToCurrent = Vector3.Distance(transform.position, current.position);

        DriveToward(lookAhead.position, current.speed, distToCurrent, cornerAngle: 0f, forceTargetSpeed: true);
    }

    void AdvanceRecordedIndexIfClose()
    {
        RecordedPoint current = recordedLine.GetPoint(currentIndex);
        float dist = Vector3.Distance(transform.position, current.position);

        if (dist < nodeReachedDistance)
        {
            currentIndex = (currentIndex + 1) % recordedLine.points.Length;
            return;
        }

        // Si estamos MUY lejos del punto actual (más de lo esperado — probablemente
        // nos salteamos un punto, por ejemplo tras un choque que nos empujó adelante),
        // buscamos hacia ADELANTE (nunca hacia atrás) el punto más cercano dentro de un rango razonable
        if (dist > maxForwardSearchDistance)
        {
            int bestForwardIndex = FindBestForwardIndex();
            if (bestForwardIndex != -1)
                currentIndex = bestForwardIndex;
        }
    }

    int FindBestForwardIndex()
    {
        int searchRange = 30; // cuántos puntos adelante revisa como candidatos
        float closestDist = float.MaxValue;
        int bestIndex = -1;

        for (int i = 1; i <= searchRange; i++)
        {
            int idx = (currentIndex + i) % recordedLine.points.Length;
            float d = Vector3.Distance(transform.position, recordedLine.GetPoint(idx).position);
            if (d < closestDist)
            {
                closestDist = d;
                bestIndex = idx;
            }
        }

        return bestIndex;
    }

    // ---------------- Modo: path genérico (fallback) ----------------

    void UpdateGenericPathDriving()
    {
        AdvanceGenericIndexIfClose();

        Transform currentNode = waypointPath.GetNode(currentIndex);
        if (currentNode == null) return;

        float distToNode = Vector3.Distance(transform.position, currentNode.position);
        float cornerAngle = GetNextCornerAngle();

        DriveToward(currentNode.position, corneringSpeedLimit, distToNode, cornerAngle, forceTargetSpeed: false);
    }

    void AdvanceGenericIndexIfClose()
    {
        Transform currentNode = waypointPath.GetNode(currentIndex);
        if (currentNode == null) return;

        float dist = Vector3.Distance(transform.position, currentNode.position);
        if (dist < nodeReachedDistance)
            currentIndex++;
    }

    float GetNextCornerAngle()
    {
        Transform a = waypointPath.GetNode(currentIndex);
        Transform b = waypointPath.GetNode(currentIndex + 1);
        Transform c = waypointPath.GetNode(currentIndex + 2);
        if (a == null || b == null || c == null) return 0f;

        Vector3 dirA = b.position - a.position; dirA.y = 0f;
        Vector3 dirB = c.position - b.position; dirB.y = 0f;
        if (dirA.sqrMagnitude < 0.01f || dirB.sqrMagnitude < 0.01f) return 0f;

        return Vector3.Angle(dirA.normalized, dirB.normalized);
    }

    float GetTargetSpeedForCorner(float cornerAngle, float maxSpeed)
    {
        if (cornerAngle <= cornerStartAngle) return maxSpeed;
        float t = Mathf.InverseLerp(cornerStartAngle, cornerSharpAngle, cornerAngle);
        return Mathf.Lerp(maxSpeed, corneringSpeedLimit, t);
    }

    // ---------------- Conducción compartida por ambos modos ----------------

    void DriveToward(Vector3 targetPoint, float targetSpeedOrLimit, float distToNode, float cornerAngle, bool forceTargetSpeed)
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float rawSteer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
        smoothedSteer = Mathf.Lerp(smoothedSteer, rawSteer, Time.deltaTime * steerSmoothSpeed);

        float currentSpeed = carController.CurrentSpeed;
        float maxSpeed = carController.EffectiveMaxSpeed;

        // forceTargetSpeed=true (línea grabada): la velocidad grabada YA es el objetivo real
        // forceTargetSpeed=false (path genérico): hay que calcularla según la curvatura detectada
        float targetSpeed = forceTargetSpeed
            ? targetSpeedOrLimit
            : GetTargetSpeedForCorner(cornerAngle, maxSpeed);

        bool withinBrakingRange = forceTargetSpeed || distToNode < brakingLookAhead;
        float throttle;
        bool handbrake = false;

        float speedExcess = currentSpeed - targetSpeed;
        if (withinBrakingRange && speedExcess > speedToleranceMargin)
        {
            throttle = Mathf.Clamp(-speedExcess * brakeResponseFactor, maxBrakeThrottle, 0f);
        }
        else
        {
            float absAngle = Mathf.Abs(angleToTarget);
            throttle = absAngle > 150f ? 0.3f : 1f;
        }

        if (!forceTargetSpeed
            && cornerAngle > driftCornerAngleThreshold
            && currentSpeed > driftMinSpeed
            && distToNode < brakingLookAhead * 0.6f)
        {
            handbrake = true;
        }

        carController.SetAIInput(throttle, smoothedSteer, handbrake);
    }

    // ---------------- Anti-atasco (sin cambios respecto al sólido) ----------------

    public void RespawnAtCurrentNode()
    {
        Vector3 pos; Quaternion facing;

        if (UsingRecordedLine)
        {
            RecordedPoint node = recordedLine.GetPoint(currentIndex);
            RecordedPoint nextNode = recordedLine.GetPoint(currentIndex + 1);
            pos = node.position;
            facing = Quaternion.LookRotation((nextNode.position - node.position).normalized, Vector3.up);
        }
        else
        {
            Transform node = waypointPath.GetNode(currentIndex);
            Transform nextNode = waypointPath.GetNode(currentIndex + 1);
            if (node == null) return;
            pos = node.position;
            facing = nextNode != null
                ? Quaternion.LookRotation((nextNode.position - node.position).normalized, Vector3.up)
                : node.rotation;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(pos, facing);
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
                    RespawnAtCurrentNode();
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