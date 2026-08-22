using UnityEngine;

public class WrongWayDetector : MonoBehaviour
{
    [Header("Config")]
    public float wrongWayAngleThreshold = 110f;
    public float minSpeedToCheck = 3f;
    public float gracePeriodBeforeWarning = 1.5f;

    AIWaypointPath path;
    Rigidbody rb;
    float wrongWayTimer;

    public bool IsGoingWrongWay { get; private set; }

    public void Initialize(AIWaypointPath waypointPath)
    {
        path = waypointPath;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (path == null || rb == null) return;

        if (rb.linearVelocity.magnitude < minSpeedToCheck)
        {
            ResetWrongWay();
            return;
        }

        var info = path.GetClosestPointInfo(transform.position);

        Vector3 carForward = transform.forward; carForward.y = 0f;
        Vector3 pathDir = info.direction; pathDir.y = 0f;

        float angle = Vector3.Angle(carForward.normalized, pathDir.normalized);

        if (angle > wrongWayAngleThreshold)
        {
            wrongWayTimer += Time.deltaTime;
            if (wrongWayTimer >= gracePeriodBeforeWarning)
                IsGoingWrongWay = true;
        }
        else
        {
            ResetWrongWay();
        }
    }

    void ResetWrongWay()
    {
        wrongWayTimer = 0f;
        IsGoingWrongWay = false;
    }
}