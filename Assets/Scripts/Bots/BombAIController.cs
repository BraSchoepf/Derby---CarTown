using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(BombCarrier))]
public class BombAIController : MonoBehaviour
{
    [Header("Persecución (cuando TIENE la bomba)")]
    public float targetReacquireInterval = 1f;
    public float directRammingRange = 15f;

    [Header("Huida (cuando NO tiene la bomba)")]
    public float fleeDistance = 20f; // qué tan lejos intenta mantenerse del portador
    public float fleeReacquireInterval = 0.5f;

    [Header("Path")]
    public float pathRecalculateInterval = 0.75f;
    public float cornerReachedDistance = 3f;

    [Header("Anti-atasco")]
    public float stuckSpeedThreshold = 1.5f;
    public float stuckTimeToTrigger = 1.2f;
    public float reverseDuration = 1f;
    public float reverseStuckThrottle = -1f;

    VehicleHealth ownHealth;
    CarController carController;
    BombCarrier bombCarrier;
    Rigidbody rb;

    Transform currentTarget;
    NavMeshPath path;
    int currentCornerIndex;
    float pathTimer;
    float targetTimer;

    float stuckTimer;
    float reverseTimer;
    bool isReversingOut;
    float reverseSteerDirection;

    bool IHaveBomb => BombCarrierManager.Instance != null
                       && BombCarrierManager.Instance.CurrentCarrier == ownHealth;

    void Awake()
    {
        ownHealth = GetComponent<VehicleHealth>();
        carController = GetComponent<CarController>();
        bombCarrier = GetComponent<BombCarrier>();
        rb = GetComponent<Rigidbody>();
        carController.isAIControlled = true;
        path = new NavMeshPath();
        reverseSteerDirection = Random.value > 0.5f ? 1f : -1f;
    }

    void Update()
    {
        if (ownHealth.IsDestroyed)
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

        if (IHaveBomb)
            UpdateChaseMode();
        else
            UpdateFleeMode();
    }

    // --- Modo persecución: igual lógica que CarAIController, target = auto vivo más cercano ---
    void UpdateChaseMode()
    {
        targetTimer -= Time.deltaTime;
        if (targetTimer <= 0f || currentTarget == null)
        {
            currentTarget = FindClosestOther();
            targetTimer = targetReacquireInterval;
        }
        if (currentTarget == null)
        {
            carController.SetAIInput(0f, 0f, false);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
        Vector3 steerTargetPoint = distToTarget <= directRammingRange
            ? PredictTargetPosition(currentTarget)
            : GetPathCorner(currentTarget.position);

        DriveToward(steerTargetPoint, reverse: false);
    }

    // --- Modo huida: aleja al bot del portador de la bomba ---
    void UpdateFleeMode()
    {
        VehicleHealth carrier = BombCarrierManager.Instance?.CurrentCarrier;
        if (carrier == null || carrier == ownHealth)
        {
            carController.SetAIInput(0f, 0f, false);
            return;
        }

        float distToCarrier = Vector3.Distance(transform.position, carrier.transform.position);

        if (distToCarrier >= fleeDistance)
        {
            // Ya está a salvo, avanza libre pero sin objetivo específico (deambula suave)
            carController.SetAIInput(0.3f, 0f, false);
            return;
        }

        // Punto opuesto al portador, proyectado a cierta distancia — hacia dónde huir
        Vector3 awayDir = (transform.position - carrier.transform.position).normalized;
        Vector3 fleeTarget = transform.position + awayDir * fleeDistance;

        DriveToward(fleeTarget, reverse: false);
    }

    Transform FindClosestOther()
    {
        DerbyGameManager derby = DerbyGameManager.Instance;
        if (derby == null) return null;

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var entry in derby.players)
        {
            if (!entry.isAlive || entry.health == ownHealth) continue;

            float dist = Vector3.Distance(transform.position, entry.health.transform.position);
            if (dist < closestDist) { closestDist = dist; closest = entry.health.transform; }
        }
        return closest;
    }

    Vector3 GetPathCorner(Vector3 destination)
    {
        pathTimer -= Time.deltaTime;
        if (pathTimer <= 0f)
        {
            NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            currentCornerIndex = 1;
            pathTimer = pathRecalculateInterval;
        }

        if (path.corners.Length == 0) return destination;

        if (currentCornerIndex < path.corners.Length)
        {
            float distToCorner = Vector3.Distance(transform.position, path.corners[currentCornerIndex]);
            if (distToCorner < cornerReachedDistance && currentCornerIndex < path.corners.Length - 1)
                currentCornerIndex++;
        }
        return path.corners[Mathf.Min(currentCornerIndex, path.corners.Length - 1)];
    }

    Vector3 PredictTargetPosition(Transform target)
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb == null) return target.position;

        float timeToImpact = Vector3.Distance(transform.position, target.position)
                              / Mathf.Max(1f, rb.linearVelocity.magnitude);
        return target.position + targetRb.linearVelocity * Mathf.Min(timeToImpact, 1.5f);
    }

    void DriveToward(Vector3 worldPoint, bool reverse)
    {
        Vector3 toTarget = worldPoint - transform.position;
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float steer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        float absAngle = Mathf.Abs(angleToTarget);
        float throttle = absAngle > 150f ? 0.3f : 1f;

        carController.SetAIInput(throttle, steer, false);
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
        carController.SetAIInput(reverseStuckThrottle, reverseSteerDirection, false);

        if (reverseTimer <= 0f)
        {
            isReversingOut = false;
            pathTimer = 0f;
        }
    }
}