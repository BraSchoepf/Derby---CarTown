using UnityEngine;

// Va en cada Transform de checkpoint del mapa (el mismo array que RaceCourseSet.CourseVariant.checkpoints)
[RequireComponent(typeof(Collider))]
public class RaceCheckpointDetector : MonoBehaviour
{
    [HideInInspector] public int checkpointIndex = -1; // asignado por RaceManager al inicializar la carrera

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (checkpointIndex < 0) return; // todavía no inicializado por RaceManager

        RaceCarIdentity identity = other.GetComponentInParent<RaceCarIdentity>();
        if (identity == null) return;

        RaceManager.Instance?.OnCheckpointReached(identity.Progress, checkpointIndex);
    }
}