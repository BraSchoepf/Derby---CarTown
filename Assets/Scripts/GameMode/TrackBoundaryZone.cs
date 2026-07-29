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

        if (raceAI != null) raceAI.RespawnAtCurrentNode();
        else if (driftAI != null) driftAI.RespawnAtCurrentNode();
        else if (playerRespawn != null) playerRespawn.RespawnAtCurrentNode();
        else carController.ForceRespawnAtLastPoint(); // fallback, no debería usarse en Racing
    }
}