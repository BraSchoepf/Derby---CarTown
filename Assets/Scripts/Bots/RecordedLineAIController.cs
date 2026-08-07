using UnityEngine;

[RequireComponent(typeof(VehicleHealth))]
[RequireComponent(typeof(CarController))]
public class RecordedLineAIController : MonoBehaviour
{
    public RaceManager.RacerProgress progress;
    public RecordedRacingLine racingLine;

    [Header("Seguimiento")]
    public float nodeReachedDistance = 4f; // más chico que antes, porque hay muchos más puntos
    public int lookAheadPoints = 3; // mira un poco adelante del punto más cercano, para suavizar el steer

    [Header("Frenado")]
    public float brakeResponseFactor = 0.2f;
    public float maxBrakeThrottle = -1f;
    public float speedToleranceMargin = 2f; // no frena si el exceso es menor a esto

    [Header("Steering")]
    public float steerSmoothSpeed = 8f;
    float smoothedSteer;

    [Header("Anti-atasco")]
    public float stuckSpeedThreshold = 1.5f;
    public float stuckTimeToTrigger = 1.5f;
    public float reverseDuration = 1.5f;
    public float reverseThrottle = -0.8f;

    float stuckTimer, reverseTimer;
    bool isReversingOut;
    float reverseSteerDirection;

    VehicleHealth ownHealth;
    CarController carController;
    Rigidbody rb;
    int currentIndex = 0;

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
        if (racingLine != null && racingLine.points.Length > 0)
            currentIndex = racingLine.GetClosestPointIndex(transform.position);
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

        if (racingLine == null || racingLine.points.Length == 0) return;

        AdvanceIndexIfClose();

        RecordedPoint current = racingLine.GetPoint(currentIndex);
        RecordedPoint lookAhead = racingLine.GetPoint(currentIndex + lookAheadPoints);

        DriveToward(lookAhead.position, current.speed);
    }

    void AdvanceIndexIfClose()
    {
        RecordedPoint current = racingLine.GetPoint(currentIndex);
        float dist = Vector3.Distance(transform.position, current.position);
        if (dist < nodeReachedDistance)
            currentIndex++;
    }

    void DriveToward(Vector3 targetPoint, float targetSpeed)
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float rawSteer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
        smoothedSteer = Mathf.Lerp(smoothedSteer, rawSteer, Time.deltaTime * steerSmoothSpeed);

        float currentSpeed = carController.CurrentSpeed;
        float speedExcess = currentSpeed - targetSpeed;

        float throttle;
        if (speedExcess > speedToleranceMargin)
            throttle = Mathf.Clamp(-speedExcess * brakeResponseFactor, maxBrakeThrottle, 0f);
        else
            throttle = 1f;

        carController.SetAIInput(throttle, smoothedSteer, false);
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