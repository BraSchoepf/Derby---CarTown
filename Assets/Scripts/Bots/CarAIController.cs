using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(CarController))]
public class CarAIController : MonoBehaviour
{
    [Header("Target")]
    public float targetReacquireInterval = 1.5f;
    public float directRammingRange = 15f;

    [Header("Path")]
    public float pathRecalculateInterval = 0.75f;
    public float cornerReachedDistance = 3f;

    [Header("Anti-atasco")]
    public float stuckSpeedThreshold = 1.5f;   // velocidad por debajo de la cual "no te estás moviendo"
    public float stuckTimeToTrigger = 1.2f;    // cuánto tiempo tolerar antes de reaccionar
    public float reverseDuration = 1f;         // cuánto dura la maniobra de reversa
    public float reverseStuckThrottle = -1f;

    Transform currentTarget;
    NavMeshPath path;
    int currentCornerIndex;
    float pathTimer;
    float targetTimer;

    float stuckTimer;
    float reverseTimer;
    bool isReversingOut;

    VehicleHealth ownHealth;
    CarController carController;
    Rigidbody rb;

    void Awake()
    {
        ownHealth = GetComponent<VehicleHealth>();
        carController = GetComponent<CarController>();
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
            return; // mientras reversea, ignora target/pathfinding
        }

        targetTimer -= Time.deltaTime;
        if (targetTimer <= 0f || currentTarget == null)
        {
            currentTarget = FindTarget();
            targetTimer = targetReacquireInterval;
        }
        if (currentTarget == null)
        {
            carController.SetAIInput(0f, 0f, false);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
        Vector3 steerTargetPoint;

        if (distToTarget <= directRammingRange)
        {
            steerTargetPoint = PredictTargetPosition();
        }
        else
        {
            pathTimer -= Time.deltaTime;
            if (pathTimer <= 0f)
            {
                NavMesh.CalculatePath(transform.position, currentTarget.position, NavMesh.AllAreas, path);
                currentCornerIndex = 1;
                pathTimer = pathRecalculateInterval;
            }
            steerTargetPoint = GetCurrentCorner();
        }

        DriveToward(steerTargetPoint);
    }

    Vector3 GetCurrentCorner()
    {
        if (path.corners.Length == 0) return currentTarget.position;

        if (currentCornerIndex < path.corners.Length)
        {
            float distToCorner = Vector3.Distance(transform.position, path.corners[currentCornerIndex]);
            if (distToCorner < cornerReachedDistance && currentCornerIndex < path.corners.Length - 1)
                currentCornerIndex++;
        }
        return path.corners[Mathf.Min(currentCornerIndex, path.corners.Length - 1)];
    }

    Vector3 PredictTargetPosition()
    {
        Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
        if (targetRb == null) return currentTarget.position;

        float timeToImpact = Vector3.Distance(transform.position, currentTarget.position)
                              / Mathf.Max(1f, rb.linearVelocity.magnitude);
        return currentTarget.position + targetRb.linearVelocity * Mathf.Min(timeToImpact, 1.5f);
    }

    void DriveToward(Vector3 worldPoint)
    {
        Vector3 toTarget = (worldPoint - transform.position);
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float steer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        // Siempre para adelante — reducimos el acelerador si el objetivo quedó casi
        // detrás nuestro, para no perder tracción girando muy cerrado a fondo.
        float absAngle = Mathf.Abs(angleToTarget);
        float throttle = absAngle > 150f ? 0.3f : 1f;

        carController.SetAIInput(throttle, steer, false);
    }

    Transform FindTarget()
    {
        DerbyGameManager derby = FindObjectOfType<DerbyGameManager>();
        if (derby == null) return null;

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var entry in derby.players)
        {
            if (!entry.isAlive || entry.health.transform == transform) continue;
            float dist = Vector3.Distance(transform.position, entry.health.transform.position);
            if (dist < closestDist) { closestDist = dist; closest = entry.health.transform; }
        }
        return closest;
    }

    void UpdateStuckDetection()
    {
        bool tryingToMove = rb.linearVelocity.magnitude < stuckSpeedThreshold; // auto casi quieto...
                                                                               // ...pero el controller SÍ está pidiendo acelerar (sino, "quieto porque decidió frenar" no cuenta como atasco)
        bool wantsToMove = true; // el AI siempre pide throttle salvo cuando no hay target

        if (tryingToMove && wantsToMove && currentTarget != null)
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

        // Reversa con steer invertido al azar (izq/der) para no quedar re-atascado
        // exactamente en el mismo ángulo contra la misma pared
        float steer = reverseSteerDirection;
        carController.SetAIInput(reverseStuckThrottle, steer, false);

        if (reverseTimer <= 0f)
        {
            isReversingOut = false;
            pathTimer = 0f; // fuerza recalcular el path apenas termina de reversear
        }
    }

    float reverseSteerDirection;

}