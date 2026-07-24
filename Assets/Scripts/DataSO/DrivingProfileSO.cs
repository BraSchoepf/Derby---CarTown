using UnityEngine;

[CreateAssetMenu(fileName = "NewDrivingProfile", menuName = "Modes/Driving Profile")]
public class DrivingProfileSO : ScriptableObject
{
    public string profileName;

    [Header("Motor")]
    public float maxSpeedMultiplier = 1f;
    public float accelerationRateMultiplier = 1f;
    public float brakeForceMultiplier = 1f;

    [Header("Steering")]
    public float maxSteerAngleMultiplier = 1f;

    [Header("Drift")]
    [Tooltip("Si está tildado, fuerza enableDrift=true sin importar la config base del auto")]
    public bool forceDriftEnabled = false;
    [Tooltip("Si está tildado, fuerza enableDrift=false — útil para modos tipo Circuito/Sprint")]
    public bool forceDriftDisabled = false;
    public float driftSteerBoostMultiplier = 1f;
    [Tooltip("Multiplica el umbral de auto-drift — valores MENORES a 1 hacen que el auto entre en drift más fácil")]
    public float autoDriftSteerThresholdMultiplier = 1f;
    [Tooltip("Multiplica la fricción lateral base — valores MENORES a 1 = más resbaladizo")]
    public float sidewaysStiffnessMultiplier = 1f;
    public float driftSustainForceMultiplier = 1f;
    public float driftKickForceMultiplier = 1f;
    public float driftAngularTorqueMultiplier = 1f;

    public void ApplyTo(CarStatsSO stats)
    {
        stats.maxSpeed *= maxSpeedMultiplier;
        stats.accelerationRate *= accelerationRateMultiplier;
        stats.brakeForce *= brakeForceMultiplier;

        stats.maxSteerAngle *= maxSteerAngleMultiplier;

        if (forceDriftEnabled) stats.enableDrift = true;
        if (forceDriftDisabled) stats.enableDrift = false;

        stats.driftSteerBoost *= driftSteerBoostMultiplier;
        stats.autoDriftSteerThreshold *= autoDriftSteerThresholdMultiplier;
        stats.sidewaysStiffnessFront *= sidewaysStiffnessMultiplier;
        stats.sidewaysStiffnessRear *= sidewaysStiffnessMultiplier;
        stats.driftSustainForce *= driftSustainForceMultiplier;
        stats.driftKickForce *= driftKickForceMultiplier;
        stats.driftAngularTorque *= driftAngularTorqueMultiplier;
    }
}