using UnityEngine;

public class CameraLookTarget : MonoBehaviour
{
    public Rigidbody carRigidbody;
    public float velocityInfluence = 0.5f;
    public float recenterSpeed = 3f; // qué tan rápido vuelve a mirar el forward del auto

    void LateUpdate()
    {
        Vector3 vel = carRigidbody.linearVelocity;
        vel.y = 0;

        Vector3 carForward = carRigidbody.transform.forward;
        float forwardSpeed = Vector3.Dot(carForward, carRigidbody.linearVelocity);

        Vector3 targetForward;

        // Solo mezclar con la velocidad si vas efectivamente para ADELANTE
        // (evita que el drift lateral o la reversa confundan la cámara)
        if (vel.magnitude > 2f && forwardSpeed > 1f)
        {
            targetForward = Vector3.Slerp(carForward, vel.normalized, velocityInfluence);
        }
        else
        {
            targetForward = carForward;
        }

        // Slerp suave hacia el target en vez de asignación directa,
        // esto asegura que SIEMPRE vuelva, nunca se quede "trabada"
        Quaternion targetRotation = Quaternion.LookRotation(targetForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, recenterSpeed * Time.deltaTime);
    }
}