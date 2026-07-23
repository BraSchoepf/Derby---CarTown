using UnityEngine;

[CreateAssetMenu(fileName = "NewCarStats", menuName = "Cars/Car Stats")]
public class CarStatsSO : ScriptableObject
{
    public enum CarCategory { Sport, Offroad, Drift, Heavy }

    [Header("Identidad")]
    public CarCategory category;
    public string carName;
    public Sprite previewImage;

    [Header("UI de Selección")]
    [Tooltip("Prefab del auto en 3D, usado por el CarPreviewRenderer (turntable)")]
    public GameObject previewPrefab;
    [Range(0f, 1f)] public float displaySpeedStat = 0.5f;
    [Range(0f, 1f)] public float displayWeightStat = 0.5f;
    [Range(0f, 1f)] public float displayResistanceStat = 0.5f;

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
    public float lowSpeedSteerMultiplier = 0.85f;
    public float fullSteerSpeedThreshold = 6f;
    public float highSpeedSteerReduction = 0.8f;
    public float highSpeedThreshold = 38f;

    [Header("Centro de masa")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Suspensión")]
    public float suspensionSpring = 43000f;
    public float suspensionDamper = 5000f;
    public float suspensionDistance = 0.28f;

    [Header("Forward Friction (tracción)")]
    public float forwardExtremumSlip = 0.35f;
    public float forwardExtremumValue = 1.5f;
    public float forwardAsymptoteSlip = 0.8f;
    public float forwardAsymptoteValue = 1.0f;
    public float forwardStiffness = 1.6f;

    [Header("Sideways Friction (agarre lateral)")]
    public float sidewaysExtremumSlip = 0.3f;
    public float sidewaysExtremumValue = 1.3f;
    public float sidewaysAsymptoteSlip = 0.5f;
    public float sidewaysAsymptoteValue = 1.0f;
    public float sidewaysStiffnessFront = 1.9f;
    public float sidewaysStiffnessRear = 1.75f;

    [Header("Ground Check")]
    public LayerMask driftableGroundLayer;

    [Header("Drift - Toggle general")]
    public bool enableDrift = true;

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
    public bool enableOffroadGrip = false;
    public float offroadStiffnessMultiplier = 1.3f;
}