using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance;

    void Awake() => Instance = this;

    public void DoHitStop(float duration = 0.05f)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // casi congelado, no del todo (0 puede trabar física)
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}