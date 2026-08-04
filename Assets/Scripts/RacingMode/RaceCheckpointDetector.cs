using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RaceCheckpointDetector : MonoBehaviour
{
    [HideInInspector] public int checkpointIndex = -1;

    [Header("VFX (opcional)")]
    public CheckpointVFX vfx;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (checkpointIndex < 0) return;

        RaceCarIdentity identity = other.GetComponentInParent<RaceCarIdentity>();
        if (identity == null) return;

        bool wasValid = RaceManager.Instance != null
                        && RaceManager.Instance.OnCheckpointReached(identity.Progress, checkpointIndex);

        if (wasValid && vfx != null)
        {
            int humanSlotIndex = identity.Progress.humanSlotIndex;
            if (humanSlotIndex >= 0) // solo jugadores humanos disparan el VFX, no bots
                vfx.OnPassedCorrectly(humanSlotIndex);
        }
    }
}