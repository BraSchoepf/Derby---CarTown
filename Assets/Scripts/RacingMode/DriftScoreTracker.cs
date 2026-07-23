using UnityEngine;

public class DriftScoreTracker : MonoBehaviour
{
    public CarController carController; // lee ángulo/velocidad actuales
    public float minDriftAngleForScoring = 8f; // mismo umbral que ya usás internamente para "está driftando"

    float currentDriftScore;
    float multiplier = 1f;
    float multiplierIncreasePerSecond = 0.5f;
    bool isDrifting;

    void FixedUpdate()
    {
        float driftAngle = carController.CurrentDriftAngle; // exponer este getter en CarController
        float speed = carController.CurrentSpeed;

        bool driftingNow = Mathf.Abs(driftAngle) > minDriftAngleForScoring && speed > 3f;

        if (driftingNow)
        {
            if (!isDrifting) { isDrifting = true; }

            float pointsThisFrame = Mathf.Abs(driftAngle) * speed * Time.fixedDeltaTime * multiplier;
            currentDriftScore += pointsThisFrame;
            multiplier += multiplierIncreasePerSecond * Time.fixedDeltaTime;
        }
        else if (isDrifting)
        {
            // Dejó de driftear: "carga" los puntos acumulados (ya sumados arriba), resetea multiplicador
            isDrifting = false;
            multiplier = 1f;
        }
    }

    // Llamado desde VehicleHealth.OnCollisionEnter (o un evento nuevo) cuando hay choque
    public void OnCollisionPenalty()
    {
        multiplier = 1f; // el choque resetea multiplicador, pero NO borra puntos ya acumulados
    }
}