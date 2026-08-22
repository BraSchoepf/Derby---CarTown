using UnityEngine;

// Va en el prefab del auto — identifica a qué RacerProgress corresponde este auto durante la carrera
public class RaceCarIdentity : MonoBehaviour
{
    public RaceManager.RacerProgress Progress { get; private set; }

    public void Initialize(RaceManager.RacerProgress progress)
    {
        Progress = progress;
    }
}