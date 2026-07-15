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

    Rigidbody rb;
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
        rb.centerOfMass += stats.centerOfMassOffset;
        rb.sleepThreshold = 0f;
        ConfigureWheelFriction();
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

    public void OnSteer(InputValue value) => steerInput = value.Get<float>();
    public void OnThrottle(InputValue value) => throttleInput = value.Get<float>();

    void FixedUpdate()
    {
        ReadHandbrakeInput();
        UpdateHandbrakeResetTimer();

        if (rb.IsSleeping() && (Mathf.Abs(throttleInput) > 0.01f || Mathf.Abs(steerInput) > 0.01f))
            rb.WakeUp();

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

        // Steering sensible a velocidad
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

        // Frenado directo extra (S), para que se sienta "con mordida"
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
        handbrakeInput = false;
        if (Keyboard.current != null)
            handbrakeInput = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.spaceKey.isPressed;
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

    // ---------------- Steering sensible a velocidad ----------------

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

    // ---------------- Aceleración / Desaceleración ----------------

    void UpdateAccelerationRamp()
    {
        float target = Mathf.Abs(throttleInput) > 0.01f ? Mathf.Abs(throttleInput) : 0f;
        float rate = target > currentTorqueFactor ? stats.accelerationRate : stats.decelerationRate;
        currentTorqueFactor = Mathf.MoveTowards(currentTorqueFactor, target, rate * Time.fixedDeltaTime);
    }

    void HandleMotorAndBrake(Wheel w, float forwardSpeed, bool handbrakeActive)
    {
        float currentSpeed = rb.linearVelocity.magnitude;

        // Motor: se anula por completo si el handbrake está activo
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
            return; // el brakeTorque del handbrake se aplica aparte en el foreach principal

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

    // ---------------- Drift ----------------

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

        // Sustain solo si NO estás frenando con handbrake (para que el freno de mano frene de verdad)
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

    // ---------------- Utilidades ----------------

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
}