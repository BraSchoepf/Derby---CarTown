using UnityEngine;

[CreateAssetMenu(fileName = "NewCarStats", menuName = "Cars/Car Stats")]
public class CarStatsSO : ScriptableObject
{
    public enum CarCategory { Sport, Offroad, Drift, Heavy }

    [Header("Identidad")]
    public CarCategory category;
    public string carName;
    public Sprite icon; // para UI de selección de auto

    [Header("Motor")]
    public float maxMotorTorque = 1500f;
    public float maxSpeed = 40f;
    public float accelerationRate = 6f;
    public float decelerationRate = 10f;
    public float brakeForce = 3000f;
    public float brakeDecelStrength = 1.5f;

    [Header("Steering")]
    public float maxSteerAngle = 32f;
    public float driftSteerBoost = 1.3f;
    public float lowSpeedSteerMultiplier = 0.7f;  // ángulo a velocidad 0
    public float fullSteerSpeedThreshold = 10f;    // desde qué velocidad tenés el 100% del ángulo
    public float highSpeedSteerReduction = 0.75f;  // ángulo mínimo a muy alta velocidad
    public float highSpeedThreshold = 30f;

    [Header("Centro de masa")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Fricción de ruedas")]
    public float forwardStiffness = 2.2f;
    public float sidewaysStiffnessFront = 1.7f;
    public float sidewaysStiffnessRear = 1.4f;

    [Header("Ground Check")]
    public LayerMask driftableGroundLayer;

    [Header("Drift - Toggle general")]
    public bool enableDrift = true; // ← un todoterreno pesado podría tener esto en false

    [Header("Drift - Config")]
    public float handbrakeDriftTorque = 2000f;
    public float driftSidewaysStiffness = 0.2f;
    public float driftKickForce = 8f;
    public float maxDriftAngle = 85f;
    public float driftAngularTorque = 15f;
    public float driftAngularDamping = 3f;
    public float maxDriftAngularVelocity = 120f;
    public float driftSustainForce = 1500f;
    public float minDriftAngleForSustain = 8f;
    public float directionChangeKick = 25f;
    public float brakeResetDamping = 12f;
    public float brakeResetThreshold = 0.15f;
    public float handbrakeResetWindow = 0.15f;

    [Header("Auto-Drift a alta velocidad")]
    public bool enableAutoDrift = true;
    public float autoDriftSpeedThreshold = 12f;
    public float autoDriftSteerThreshold = 0.6f;
    public float autoDriftStiffness = 0.5f;

    [Header("Offroad (opcional)")]
    public bool enableOffroadGrip = false; // más agarre en terrenos no-pista
    public float offroadStiffnessMultiplier = 1.3f;
}