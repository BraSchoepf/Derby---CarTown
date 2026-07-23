using UnityEngine;

[RequireComponent(typeof(CarController))]
public class RaceAIController : MonoBehaviour
{
    public RaceManager.RacerProgress progress; // asignado por RaceSetup al spawnear
    public RaceManager raceManager;

    CarController carController;
    Rigidbody rb;

    void Awake()
    {
        carController = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        carController.isAIControlled = true;
    }

    void Update()
    {
        if (raceManager == null || raceManager.activeCourse == null || progress == null) return;
        if (progress.finished) { carController.SetAIInput(0f, 0f, false); return; }

        Transform target = GetCurrentTargetCheckpoint();
        if (target == null) return;

        DriveToward(target.position);
    }

    Transform GetCurrentTargetCheckpoint()
    {
        var checkpoints = raceManager.activeCourse.checkpoints;
        if (checkpoints.Length == 0) return null;
        return checkpoints[progress.currentCheckpointIndex];
    }

    void DriveToward(Vector3 worldPoint)
    {
        Vector3 toTarget = worldPoint - transform.position;
        toTarget.y = 0f;

        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float steer = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        float absAngle = Mathf.Abs(angleToTarget);
        float throttle = absAngle > 150f ? 0.3f : 1f;

        carController.SetAIInput(throttle, steer, false);
    }
}