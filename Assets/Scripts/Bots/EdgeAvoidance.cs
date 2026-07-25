using UnityEngine;

[RequireComponent(typeof(CarController))]
public class EdgeAvoidance : MonoBehaviour
{
    [Header("Detección de borde (raycast de piso hacia adelante)")]
    public float edgeCheckDistance = 5f;
    [Tooltip("SOLO la layer del piso real del arena (la misma 'Grounded' que usás para detección de vuelco)")]
    public LayerMask groundLayer;
    public float groundCheckRayLength = 5f; // qué tan abajo buscamos piso antes de asumir "no hay nada"

    [Header("Detección adaptativa a velocidad")]
    public float speedDistanceMultiplier = 0.3f;

    [Header("Retirada de borde")]
    public float edgeRetreatDuration = 0.9f;
    public float reorientAngleThreshold = 25f;
    public float reorientMaxThrottle = 0.5f;

    enum EdgeState { None, Retreating, Reorienting }
    EdgeState edgeState = EdgeState.None;
    float edgeRetreatTimer;

    CarController carController;
    CarAIController aiController;
    Rigidbody rb;

    void Awake()
    {
        carController = GetComponent<CarController>();
        aiController = GetComponent<CarAIController>();
        rb = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (aiController == null) return;

        switch (edgeState)
        {
            case EdgeState.Retreating:
                HandleRetreat();
                return;
            case EdgeState.Reorienting:
                HandleReorient();
                return;
            case EdgeState.None:
                if (IsEdgeAhead())
                {
                    edgeState = EdgeState.Retreating;
                    edgeRetreatTimer = edgeRetreatDuration;
                    rb.linearVelocity *= 0.4f;
                    HandleRetreat();
                }
                return;
        }
    }

    bool IsEdgeAhead()
    {
        float dynamicDistance = edgeCheckDistance + rb.linearVelocity.magnitude * speedDistanceMultiplier;

        // Punto en el aire, adelante del auto, desde donde chequeamos si hay piso debajo
        Vector3 checkPoint = transform.position + transform.forward * dynamicDistance;
        Vector3 rayOrigin = checkPoint + Vector3.up * 1f;

        bool groundFound = Physics.Raycast(rayOrigin, Vector3.down, groundCheckRayLength, groundLayer);
        return !groundFound; // si NO hay piso ahí, es el borde/vacío
    }

    void HandleRetreat()
    {
        edgeRetreatTimer -= Time.deltaTime;
        float steer = CalculateSteerTowardTarget();
        carController.SetAIInput(-1f, steer, true);

        // Clave: no salir de Retreating solo porque venció el timer — hay que confirmar
        // que YA NO hay borde adelante, sino puede volver a acelerar directo al vacío
        if (edgeRetreatTimer <= 0f && !IsEdgeAhead())
            edgeState = EdgeState.Reorienting;
        else if (edgeRetreatTimer <= 0f)
            edgeRetreatTimer = 0.3f; // sigue retrocediendo un poco más si el borde sigue ahí
    }

    void HandleReorient()
    {
        float steer = CalculateSteerTowardTarget();
        float angleToTarget = Mathf.Abs(steer) * 45f;
        float throttle = angleToTarget > reorientAngleThreshold ? reorientMaxThrottle : 1f;
        carController.SetAIInput(throttle, steer, false);

        if (angleToTarget <= reorientAngleThreshold)
            edgeState = EdgeState.None;
    }

    float CalculateSteerTowardTarget()
    {
        Transform target = aiController.CurrentTarget;
        if (target == null) return 0f;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        return Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
    }

    void OnDrawGizmosSelected()
    {
        float dynamicDistance = edgeCheckDistance;
        Vector3 checkPoint = transform.position + transform.forward * dynamicDistance;
        Vector3 rayOrigin = checkPoint + Vector3.up * 1f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundCheckRayLength);
        Gizmos.DrawWireSphere(checkPoint, 0.3f);
    }
}