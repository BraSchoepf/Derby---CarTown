using UnityEngine;

public class TrackBoundaryDetector : MonoBehaviour
{
    [Tooltip("Layer del plano de pasto (o cualquier superficie fuera de pista)")]
    public LayerMask offTrackLayer;

    CarController carController;
    RaceAIController raceAI;
    DriftAIController driftAI;
    PlayerRaceRespawn playerRespawn;
    void Awake()
    {
        carController = GetComponent<CarController>();
        raceAI = GetComponent<RaceAIController>();
        driftAI = GetComponent<DriftAIController>();
        playerRespawn = GetComponent<PlayerRaceRespawn>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & offTrackLayer) == 0) return;

        // Buscar en el momento del choque, no cachear en Awake — los AIController
        // se agregan con AddComponent DESPUÉS del Instantiate, así que cachear
        // temprano siempre encuentra null.
        RaceAIController raceAI = GetComponent<RaceAIController>();
        DriftAIController driftAI = GetComponent<DriftAIController>();
        PlayerRaceRespawn playerRespawn = GetComponent<PlayerRaceRespawn>();

        if (raceAI != null) raceAI.RespawnAtCurrentNode();
        else if (driftAI != null) driftAI.RespawnAtCurrentNode();
        else if (playerRespawn != null) playerRespawn.RespawnAtCurrentNode();
        else
        {
            CarController carController = GetComponent<CarController>();
            if (carController != null) carController.ForceRespawnAtLastPoint();
        }
    }
}