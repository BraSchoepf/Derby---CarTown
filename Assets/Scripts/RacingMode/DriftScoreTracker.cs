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

    [Header("Ventana de gracia (para enganchar otro drift)")]
    [Tooltip("Segundos después de dejar de driftear antes de cargar los puntos definitivamente")]
    public float chainGraceWindow = 0.6f;

    [Header("Choque - qué cuenta como reset")]
    public float minCollisionForceToReset = 4f;

    [Header("Feedback de choque (color)")]
    public float redFlashDuration = 0.8f;

    public float TotalScore { get; private set; }
    public float CurrentMultiplier { get; private set; } = 1f;
    public float CurrentRunPoints { get; private set; }
    public bool IsDrifting { get; private set; }
    public bool IsInGraceWindow { get; private set; }
    public bool IsFlashingRed { get; private set; }

    public event Action<float> OnScoreChanged;
    public event Action<float> OnRunPointsChanged;
    public event Action<float> OnDriftEnded;
    public event Action OnPointsLost; // dispara el flash rojo en la UI

    CarController carController;
    float graceTimer;
    float redFlashTimer;

    void Awake()
    {
        carController = GetComponent<CarController>();
    }

    void FixedUpdate()
    {
        float driftAngle = carController.CurrentDriftAngle;
        float speed = carController.CurrentSpeed;
        bool grounded = carController.IsGrounded;

        bool driftingNow = grounded
                            && Mathf.Abs(driftAngle) > minDriftAngleForScoring
                            && speed > minSpeedForScoring;

        if (driftingNow)
        {
            // Retomó el drift — cancela la ventana de gracia si estaba corriendo,
            // el combo sigue vivo sin perder lo acumulado
            IsInGraceWindow = false;
            graceTimer = 0f;
            IsDrifting = true;

            float pointsThisFrame = Mathf.Abs(driftAngle) * speed * pointsPerAngleSpeedUnit * Time.fixedDeltaTime * CurrentMultiplier;
            CurrentRunPoints += pointsThisFrame;

            CurrentMultiplier = Mathf.Min(maxMultiplier, CurrentMultiplier + multiplierIncreasePerSecond * Time.fixedDeltaTime);

            OnRunPointsChanged?.Invoke(CurrentRunPoints);
        }
        else if (IsDrifting && grounded)
        {
            // Dejó de driftear en el piso: no carga todavía, entra en ventana de gracia
            if (!IsInGraceWindow)
            {
                IsInGraceWindow = true;
                graceTimer = chainGraceWindow;
            }
        }
        else if (!grounded)
        {
            // En el aire: pausa todo, sin tocar la ventana de gracia
            IsDrifting = false;
        }

        if (IsInGraceWindow)
        {
            graceTimer -= Time.fixedDeltaTime;
            if (graceTimer <= 0f)
            {
                IsInGraceWindow = false;
                EndDriftRun(); // se cumplió el lapso sin retomar: recién ACÁ se carga
            }
        }

        UpdateRedFlash();
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minCollisionForceToReset) return;

        if (CurrentRunPoints > 0f || CurrentMultiplier > 1f || IsInGraceWindow)
        {
            CurrentRunPoints = 0f;
            CurrentMultiplier = 1f;
            IsDrifting = false;
            IsInGraceWindow = false;
            graceTimer = 0f;

            OnRunPointsChanged?.Invoke(0f);
            OnPointsLost?.Invoke(); // dispara el feedback visual rojo
            redFlashTimer = redFlashDuration;
            IsFlashingRed = true;
        }
    }

    void UpdateRedFlash()
    {
        if (!IsFlashingRed) return;

        redFlashTimer -= Time.fixedDeltaTime;
        if (redFlashTimer <= 0f)
            IsFlashingRed = false;
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
}