#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MissionDebugTools
{
    [MenuItem("Tools/Reset Mission Progress")]
    static void ResetMissionsAndUnlocks()
    {
        UnlockRegistry.ResetAll(); // borra PlayerPrefs correctos (_P1, _P2) Y el diccionario en memoria

        if (MissionManager.Instance != null)
            MissionManager.Instance.ResetAllProgress();
        else
            PlayerPrefs.DeleteKey("MissionProgress"); // fallback si no hay instancia corriendo (fuera de Play)

        PlayerPrefs.Save();
        Debug.Log("[MissionDebugTools] Progreso de misiones Y desbloqueos reseteados.");
    }
}
#endif