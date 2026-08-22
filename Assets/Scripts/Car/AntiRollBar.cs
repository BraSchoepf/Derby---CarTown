using UnityEngine;

public class AntiRollBar : MonoBehaviour
{
    public WheelCollider wheelLeft;
    public WheelCollider wheelRight;
    public float antiRollForce = 5000f;

    Rigidbody rb;

    void Awake() => rb = GetComponentInParent<Rigidbody>();

    void FixedUpdate()
    {
        WheelHit hit;
        float travelLeft = 1f;
        float travelRight = 1f;

        bool groundedLeft = wheelLeft.GetGroundHit(out hit);
        if (groundedLeft)
            travelLeft = (-wheelLeft.transform.InverseTransformPoint(hit.point).y - wheelLeft.radius) / wheelLeft.suspensionDistance;

        bool groundedRight = wheelRight.GetGroundHit(out hit);
        if (groundedRight)
            travelRight = (-wheelRight.transform.InverseTransformPoint(hit.point).y - wheelRight.radius) / wheelRight.suspensionDistance;

        float antiRollForceValue = (travelLeft - travelRight) * antiRollForce;

        if (groundedLeft)
            rb.AddForceAtPosition(wheelLeft.transform.up * -antiRollForceValue, wheelLeft.transform.position);
        if (groundedRight)
            rb.AddForceAtPosition(wheelRight.transform.up * antiRollForceValue, wheelRight.transform.position);
    }
}