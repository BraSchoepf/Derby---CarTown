using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RingOutZone : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[RingOutZone] Trigger tocado por: {other.name}");
        VehicleHealth health = other.GetComponentInParent<VehicleHealth>();
        if (health == null || health.IsDestroyed)
        {
            Debug.Log($"[RingOutZone] health null o ya destruido: health={health}, isDestroyed={health?.IsDestroyed}");
            return;
        }

        CarController controller = health.GetComponent<CarController>();
        if (controller != null) controller.StopAllInputs(); // el jugador pierde control apenas cae

        // Ring-out: eliminación directa, sin atribuir "quién te empujó" como kill del atacante,
        // salvo que quieras que si te dieron un empujón, se lleve el crédito - ver nota abajo
        health.DestroyVehicle();
        Debug.Log($"[RingOutZone] {other.name} eliminado por ring-out");
    }
}