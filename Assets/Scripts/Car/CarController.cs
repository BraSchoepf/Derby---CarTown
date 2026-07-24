using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        public WheelCollider collider;
        public Transform visual;
        public bool motor;
        public bool steering;
        public bool brake;
    }

    [Header("Config")]
    public CarStatsSO stats;

    [Header("Wheels")]
    public Wheel[] wheels;

    [Header("Handbrake - Tecla específica del jugador")]
    public Key handbrakeKey = Key.LeftShift;
    public Key handbrakeKeyAlt = Key.None;

    [Header("Control Aéreo")]
    public bool enableAirControl = true;
    public float airPitchTorque = 4f;
    public float airRollTorque = 5f;
    public float airYawTorque = 2f;

    [Tooltip("Techo de velocidad angular mientras está en el aire — evita que rolls descontrolados se retroalimenten con el acelerador")]
    public float maxAirAngularVelocity = 180f; // grados/segundo
    public float airAngularDamping = 1.5f;

    [Header("Detección de estado - anti-parpadeo")]
    public float airborneConfirmTime = 0.15f; // segundos que debe sostenerse el estado antes de confirmarlo
    float airborneStateTimer = 0f;
    bool confirmedAirborne = false;

    [Header("Recuperación de Vuelco (trigger en el techo)")]
    public bool enableFlipRecovery = true;
    public BoxCollider roofCheckBox;
    public LayerMask groundLayerForFlipCheck;
    public float flipRecoveryTorque = 6f;

    [Tooltip("Velocidad lineal máxima para considerar que el auto está 'detenido' y habilitar la recuperación de vuelco")]
    public float maxSpeedForFlipRecovery = 1.5f;
    [Tooltip("Velocidad angular máxima (rad/s) para no confundir un roll en curso con un vuelco quieto")]
    public float maxAngularSpeedForFlipRecovery = 1f;

    bool isRoofTouchingGround = false;

    [Header("IA")]
    public bool isAIControlled = false;
    bool isDead = false;

    [Header("Identidad")]
    [Tooltip("0 = bot/IA, 1 = P1, 2 = P2. Lo setea GameSetup/AISpawner al spawnear.")]
    public int playerIndex = 0;

    [Header("Anti-atasco / Respawn")]
    public float stuckCheckSpeedThreshold = 0.5f;
    public float stuckTimeBeforeRespawn = 4f;
    public bool autoRespawnWhenStuck = true;

    public float CurrentDriftAngle => currentDriftAngle;
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;

    Vector3 spawnPosition;
    Quaternion spawnRotation;
    float stuckRespawnTimer;

    Rigidbody rb;
    bool initialized = false;
    float steerInput;
    float throttleInput;
    bool handbrakeInput;

    float currentTorqueFactor = 0f;
    bool wasDrifting = false;
    float driftEntrySpeed = 0f;
    float currentDriftAngle = 0f;
    float lastSteerSign = 0f;
    bool wasHandbrakePressed = false;
    float handbrakeResetTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!initialized) Initialize(stats);
    }
    public void Initialize(CarStatsSO statsToUse)
    {
        stats = statsToUse;
        rb.centerOfMass = Vector3.zero; // reset por si se llama más de una vez
        rb.centerOfMass += stats.centerOfMassOffset;
        rb.sleepThreshold = 0f;
        ConfigureWheelFriction();
        initialized = true;
    }

    void ConfigureWheelFriction()
    {
        foreach (var w in wheels)
        {
            WheelFrictionCurve forward = w.collider.forwardFriction;
            forward.stiffness = stats.forwardStiffness;
            w.collider.forwardFriction = forward;

            WheelFrictionCurve sideways = w.collider.sidewaysFriction;
            sideways.stiffness = w.steering ? stats.sidewaysStiffnessFront : stats.sidewaysStiffnessRear;
            w.collider.sidewaysFriction = sideways;
        }
    }

    public void OnThrottle(InputValue value)
    {
        if (isDead) return;
        throttleInput = value.Get<float>();
    }

    public void OnHandbrake(InputValue value)
    {
        Debug.Log($"{gameObject.name} OnHandbrake event fired! isPressed={value.isPressed}");
    }

    public void OnSteer(InputValue value)
    {
        if (isDead) return;
        steerInput = value.Get<float>();
    }

    void Update()
    {
        HandleManualRespawnInput();
        UpdateStuckRespawnTimer();
    }

    void FixedUpdate()
    {
        // isDead PRIMERO: si el auto está destruido, frena de verdad y no procesa nada más
        if (isDead)
        {
            foreach (var w in wheels)
            {
                w.collider.motorTorque = 0f;
                w.collider.brakeTorque = stats.brakeForce;
                UpdateWheelVisual(w);
            }
            return;
        }

        ReadHandbrakeInput();
        UpdateHandbrakeResetTimer();

        if (rb.IsSleeping() && (Mathf.Abs(throttleInput) > 0.01f || Mathf.Abs(steerInput) > 0.01f))
            rb.WakeUp();

        int groundedWheels = GroundedWheelCount();
        bool rawAirborne = groundedWheels == 0 && !isRoofTouchingGround;

        if (rawAirborne != confirmedAirborne)
        {
            airborneStateTimer += Time.fixedDeltaTime;
            if (airborneStateTimer >= airborneConfirmTime)
            {
                confirmedAirborne = rawAirborne;
                airborneStateTimer = 0f;
            }
        }
        else
        {
            airborneStateTimer = 0f;
        }

        bool isAirborne = confirmedAirborne;

        if (isAirborne && enableAirControl)
        {
            ApplyAirControl();
            ZeroAllWheelForces();
            return;
        }

        if (IsFlipped() && enableFlipRecovery)
        {
            ApplyFlipRecovery();
            ZeroAllWheelForces();
            return;
        }

        bool grounded = IsOnDriftableGround();
        float forwardSpeed = Vector3.Dot(transform.forward, rb.linearVelocity);
        Vector3 flatVelocity = rb.linearVelocity; flatVelocity.y = 0;

        if (flatVelocity.magnitude > 1f)
            currentDriftAngle = Vector3.SignedAngle(transform.forward, flatVelocity, Vector3.up);

        bool wantsHandbrakeDrift = stats.enableDrift && grounded && handbrakeInput;
        bool autoDrifting = stats.enableDrift && stats.enableAutoDrift && grounded && !handbrakeInput
                             && forwardSpeed > stats.autoDriftSpeedThreshold
                             && Mathf.Abs(steerInput) > stats.autoDriftSteerThreshold;
        bool isDrifting = wantsHandbrakeDrift || autoDrifting;

        HandleDriftKickAndEntry(wantsHandbrakeDrift, forwardSpeed);

        float speedMultiplier = CalculateSpeedSensitiveSteerMultiplier(rb.linearVelocity.magnitude);
        float effectiveSteerAngle = steerInput * stats.maxSteerAngle * speedMultiplier;
        if (isDrifting) effectiveSteerAngle *= stats.driftSteerBoost;

        UpdateAccelerationRamp();

        foreach (var w in wheels)
        {
            if (w.steering)
                w.collider.steerAngle = effectiveSteerAngle;

            HandleMotorAndBrake(w, forwardSpeed, wantsHandbrakeDrift);

            if (wantsHandbrakeDrift && !w.steering)
            {
                w.collider.brakeTorque = stats.handbrakeDriftTorque;
                ApplyFriction(w.collider, stats.driftSidewaysStiffness);
            }
            else if (autoDrifting && !w.steering)
            {
                ApplyFriction(w.collider, stats.autoDriftStiffness);
            }
            else
            {
                ApplyFriction(w.collider, w.steering ? stats.sidewaysStiffnessFront : stats.sidewaysStiffnessRear);
            }

            UpdateWheelVisual(w);
        }

        bool isBraking = throttleInput < -0.01f && forwardSpeed > 0.5f && !handbrakeInput;
        if (isBraking)
        {
            Vector3 brakeDir = -rb.linearVelocity.normalized;
            rb.AddForce(brakeDir * stats.brakeDecelStrength * rb.linearVelocity.magnitude, ForceMode.Acceleration);
        }

        if (isDrifting && Mathf.Abs(forwardSpeed) > 3f)
            ApplyDriftPhysics(forwardSpeed, wantsHandbrakeDrift);
    }
    void ReadHandbrakeInput()
    {
        if (isAIControlled) return;

        handbrakeInput = false;
        if (Keyboard.current != null)
        {
            if (handbrakeKey != Key.None && Keyboard.current[handbrakeKey].isPressed)
                handbrakeInput = true;
            if (handbrakeKeyAlt != Key.None && Keyboard.current[handbrakeKeyAlt].isPressed)
                handbrakeInput = true;
        }
        if (Gamepad.current != null)
            handbrakeInput |= Gamepad.current.rightTrigger.isPressed;
    }

    void UpdateHandbrakeResetTimer()
    {
        bool handbrakeJustPressed = handbrakeInput && !wasHandbrakePressed;
        if (handbrakeJustPressed)
            handbrakeResetTimer = stats.handbrakeResetWindow;

        wasHandbrakePressed = handbrakeInput;

        if (handbrakeResetTimer > 0f)
            handbrakeResetTimer -= Time.fixedDeltaTime;
    }

    float CalculateSpeedSensitiveSteerMultiplier(float speed)
    {
        float lowSpeedFactor = Mathf.Lerp(stats.lowSpeedSteerMultiplier, 1f,
            Mathf.Clamp01(speed / stats.fullSteerSpeedThreshold));

        float highSpeedFactor = speed > stats.highSpeedThreshold
            ? Mathf.Lerp(1f, stats.highSpeedSteerReduction,
                Mathf.Clamp01((speed - stats.highSpeedThreshold) / 20f))
            : 1f;

        return lowSpeedFactor * highSpeedFactor;
    }

    void UpdateAccelerationRamp()
    {
        float target = Mathf.Abs(throttleInput) > 0.01f ? Mathf.Abs(throttleInput) : 0f;
        float rate = target > currentTorqueFactor ? stats.accelerationRate : stats.decelerationRate;
        currentTorqueFactor = Mathf.MoveTowards(currentTorqueFactor, target, rate * Time.fixedDeltaTime);
    }

    void HandleMotorAndBrake(Wheel w, float forwardSpeed, bool handbrakeActive)
    {
        float currentSpeed = rb.linearVelocity.magnitude;

        if (w.motor && !handbrakeActive)
        {
            if (throttleInput > 0.01f && currentSpeed < stats.maxSpeed)
                w.collider.motorTorque = currentTorqueFactor * stats.maxMotorTorque;
            else if (throttleInput < -0.01f && forwardSpeed <= 0.5f)
                w.collider.motorTorque = -currentTorqueFactor * stats.maxMotorTorque;
            else
                w.collider.motorTorque = 0f;
        }
        else
        {
            w.collider.motorTorque = 0f;
        }

        if (handbrakeActive)
            return;

        bool brakingWithS = w.brake && throttleInput < -0.01f && forwardSpeed > 0.5f;

        if (brakingWithS)
            w.collider.brakeTorque = stats.brakeForce;
        else if (Mathf.Abs(throttleInput) < 0.01f)
            w.collider.brakeTorque = (w.brake || w.motor) ? ComputeCoastBrake(currentSpeed) : 0f;
        else
            w.collider.brakeTorque = 0f;
    }

    float ComputeCoastBrake(float currentSpeed)
    {
        if (currentSpeed < 0.3f) return stats.brakeForce;
        return stats.decelerationRate * 40f;
    }

    void HandleDriftKickAndEntry(bool wantsHandbrakeDrift, float forwardSpeed)
    {
        if (wantsHandbrakeDrift && !wasDrifting && Mathf.Abs(forwardSpeed) > 3f)
        {
            float kickDir = steerInput != 0 ? Mathf.Sign(steerInput) : 1f;
            rb.AddForce(transform.right * kickDir * stats.driftKickForce, ForceMode.VelocityChange);
            driftEntrySpeed = rb.linearVelocity.magnitude;
        }
        wasDrifting = wantsHandbrakeDrift;
    }

    void ApplyDriftPhysics(float forwardSpeed, bool handbrakeActive)
    {
        bool angleWithinLimit = Mathf.Abs(currentDriftAngle) < stats.maxDriftAngle
                                 || Mathf.Sign(steerInput) != Mathf.Sign(currentDriftAngle);

        float currentSteerSign = Mathf.Sign(steerInput);
        bool directionReversed = Mathf.Abs(steerInput) > 0.1f
                                  && lastSteerSign != 0f
                                  && currentSteerSign != lastSteerSign;

        bool brakeResetActive = throttleInput < -stats.brakeResetThreshold || handbrakeResetTimer > 0f;

        if (angleWithinLimit)
        {
            float torqueToApply = stats.driftAngularTorque;

            if (directionReversed && !brakeResetActive)
            {
                float angularSpeedToOvercome = rb.angularVelocity.magnitude * Mathf.Rad2Deg;
                torqueToApply += stats.directionChangeKick * Mathf.Clamp01(angularSpeedToOvercome / 60f);
            }

            rb.AddTorque(Vector3.up * steerInput * torqueToApply, ForceMode.Acceleration);
        }

        if (Mathf.Abs(steerInput) > 0.1f)
            lastSteerSign = currentSteerSign;

        Vector3 angVel = rb.angularVelocity;
        float dampingToApply = brakeResetActive ? stats.brakeResetDamping : stats.driftAngularDamping;
        rb.AddTorque(-angVel * dampingToApply, ForceMode.Acceleration);

        if (rb.angularVelocity.magnitude > stats.maxDriftAngularVelocity * Mathf.Deg2Rad)
            rb.angularVelocity = rb.angularVelocity.normalized * stats.maxDriftAngularVelocity * Mathf.Deg2Rad;

        if (!handbrakeActive && Mathf.Abs(currentDriftAngle) > stats.minDriftAngleForSustain)
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            if (currentSpeed < driftEntrySpeed)
            {
                Vector3 dir = rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.normalized : transform.forward;
                float newSpeed = Mathf.MoveTowards(currentSpeed, driftEntrySpeed, stats.driftSustainForce * 0.001f * Time.fixedDeltaTime);
                rb.linearVelocity = dir * newSpeed;
            }
        }
    }

    void ApplyAirControl()
    {
        rb.AddTorque(transform.right * (throttleInput * airPitchTorque), ForceMode.Acceleration);
        rb.AddTorque(-transform.forward * (steerInput * airRollTorque), ForceMode.Acceleration);

        // Damping propio del aire: sin esto, la rotación nunca se frena sola,
        // solo se le sigue sumando torque sin control
        rb.AddTorque(-rb.angularVelocity * airAngularDamping, ForceMode.Acceleration);

        // Clamp duro: pase lo que pase, la rotación en el aire nunca puede superar este techo
        float maxRad = maxAirAngularVelocity * Mathf.Deg2Rad;
        if (rb.angularVelocity.magnitude > maxRad)
            rb.angularVelocity = rb.angularVelocity.normalized * maxRad;
    }

    void ApplyFlipRecovery()
    {
        float rollDirection = steerInput != 0f ? Mathf.Sign(steerInput) : 1f;
        rb.AddTorque(-transform.forward * rollDirection * flipRecoveryTorque, ForceMode.Acceleration);

        if (Mathf.Abs(throttleInput) > 0.1f)
            rb.AddTorque(-transform.forward * Mathf.Sign(throttleInput) * flipRecoveryTorque * 0.5f, ForceMode.Acceleration);

        rb.AddForce(Vector3.up * 2f, ForceMode.Acceleration);
    }

    void ZeroAllWheelForces()
    {
        foreach (var w in wheels)
        {
            w.collider.motorTorque = 0f;
            w.collider.brakeTorque = 0f;
            UpdateWheelVisual(w);
        }
    }

    bool IsOnDriftableGround()
    {
        foreach (var w in wheels)
        {
            if ((w.motor || w.brake) && w.collider.GetGroundHit(out WheelHit hit))
            {
                if (((1 << hit.collider.gameObject.layer) & stats.driftableGroundLayer) != 0)
                    return true;
            }
        }
        return false;
    }

    void ApplyFriction(WheelCollider col, float stiffness)
    {
        WheelFrictionCurve f = col.sidewaysFriction;
        f.stiffness = stiffness;
        col.sidewaysFriction = f;
    }

    void UpdateWheelVisual(Wheel w)
    {
        if (w.visual == null) return;
        w.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        w.visual.position = pos;
        w.visual.rotation = rot;
    }

    public void SetAIInput(float throttle, float steer, bool handbrake)
    {
        if (isDead) return;
        throttleInput = throttle;
        steerInput = steer;
        handbrakeInput = handbrake;
    }

    public void StopAllInputs()
    {
        isDead = true;
        throttleInput = 0f;
        steerInput = 0f;
        handbrakeInput = false;
    }

    int GroundedWheelCount()
    {
        int count = 0;
        foreach (var w in wheels)
        {
            if (w.collider.GetGroundHit(out _)) count++;
        }
        return count;
    }

    bool IsFlipped()
    {
        if (!isRoofTouchingGround) return false;

        bool isSlowEnough = rb.linearVelocity.magnitude < maxSpeedForFlipRecovery;
        bool isNotSpinning = rb.angularVelocity.magnitude < maxAngularSpeedForFlipRecovery;

        return isSlowEnough && isNotSpinning;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & groundLayerForFlipCheck) != 0)
            isRoofTouchingGround = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & groundLayerForFlipCheck) != 0)
            isRoofTouchingGround = false;
    }

    // Usar si el BoxCollider del techo vive en un GameObject hijo separado (con RoofTriggerRelay)
    public void NotifyRoofTrigger(Collider other, bool entering)
    {
        if (((1 << other.gameObject.layer) & groundLayerForFlipCheck) == 0) return;
        isRoofTouchingGround = entering;
    }

    // ---------------- Respawn ----------------

    public void SetSpawnPoint(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
    }

    void HandleManualRespawnInput()
    {
        if (isDead || Keyboard.current == null) return;

        bool respawnPressed = playerIndex == 1
            ? Keyboard.current.rKey.wasPressedThisFrame
            : playerIndex == 2 && Keyboard.current.nKey.wasPressedThisFrame;

        if (respawnPressed) RespawnAtSpawnPoint();
    }

    void UpdateStuckRespawnTimer()
    {
        if (!autoRespawnWhenStuck || isDead) return;

        bool isMoving = rb.linearVelocity.magnitude > stuckCheckSpeedThreshold;

        if (isMoving)
        {
            stuckRespawnTimer = 0f;
            return;
        }

        stuckRespawnTimer += Time.deltaTime;
        if (stuckRespawnTimer >= stuckTimeBeforeRespawn)
        {
            RespawnAtSpawnPoint();
            stuckRespawnTimer = 0f;
        }
    }

    void RespawnAtSpawnPoint()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        stuckRespawnTimer = 0f;
    }
}