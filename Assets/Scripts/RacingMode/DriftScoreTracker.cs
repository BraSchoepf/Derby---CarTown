using UnityEngine;
using System;

[RequireComponent(typeof(CarController))]
public class DriftScoreTracker : MonoBehaviour
{
    [Header("Puntaje")]
    public float minDriftAngleForScoring = 8f;
    public float minSpeedForScoring = 3f;
    public float pointsPerAngleSpeedUnit = 1f;

    [Header("Multiplicador")]
    public float multiplierIncreasePerSecond = 0.5f;
    public float maxMultiplier = 5f;

    public float TotalScore { get; private set; }
    public float CurrentMultiplier { get; private set; } = 1f;
    public float CurrentRunPoints { get; private set; } // puntos del drift actual, aún no "cargados"
    public bool IsDrifting { get; private set; }

    public event Action<float> OnScoreChanged; // TotalScore actualizado
    public event Action<float> OnRunPointsChanged; // puntos del drift en curso (para feedback en vivo)
    public event Action<float> OnDriftEnded; // cuántos puntos se "cargaron" al terminar un drift

    CarController carController;
    VehicleHealth vehicleHealth;

    void Awake()
    {
        carController = GetComponent<CarController>();
        vehicleHealth = GetComponent<VehicleHealth>();
        if (vehicleHealth != null)
            vehicleHealth.OnCollisionDetected += HandleCollision;
    }

    void FixedUpdate()
    {
        float driftAngle = carController.CurrentDriftAngle;
        float speed = carController.CurrentSpeed;

        bool driftingNow = Mathf.Abs(driftAngle) > minDriftAngleForScoring && speed > minSpeedForScoring;

        if (driftingNow)
        {
            IsDrifting = true;

            float pointsThisFrame = Mathf.Abs(driftAngle) * speed * pointsPerAngleSpeedUnit * Time.fixedDeltaTime * CurrentMultiplier;
            CurrentRunPoints += pointsThisFrame;

            CurrentMultiplier = Mathf.Min(maxMultiplier, CurrentMultiplier + multiplierIncreasePerSecond * Time.fixedDeltaTime);

            OnRunPointsChanged?.Invoke(CurrentRunPoints);
        }
        else if (IsDrifting)
        {
            // Se cortó el drift (no por choque, simplemente dejó de resbalar): carga los puntos
            EndDriftRun();
        }
    }

    void HandleCollision(Collision collision)
    {
        if (!IsDrifting) return;

        // Choque durante un drift: se pierden los puntos del run actual (no se cargan) y resetea multiplicador
        CurrentRunPoints = 0f;
        CurrentMultiplier = 1f;
        IsDrifting = false;
        OnRunPointsChanged?.Invoke(0f);
    }

    void EndDriftRun()
    {
        TotalScore += CurrentRunPoints;
        OnScoreChanged?.Invoke(TotalScore);
        OnDriftEnded?.Invoke(CurrentRunPoints);

        CurrentRunPoints = 0f;
        CurrentMultiplier = 1f;
        IsDrifting = false;
    }

    void OnDestroy()
    {
        if (vehicleHealth != null)
            vehicleHealth.OnCollisionDetected -= HandleCollision;
    }
}