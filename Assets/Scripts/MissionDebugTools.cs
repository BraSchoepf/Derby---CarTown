#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MissionDebugTools
{
    [MenuItem("Tools/Reset Mission Progress")]
    static void ResetMissionsAndUnlocks()
    {
        PlayerPrefs.DeleteKey("MissionProgress");
        PlayerPrefs.DeleteKey("UnlockedRewards");
        PlayerPrefs.Save();
        Debug.Log("[MissionDebugTools] Progreso de misiones Y desbloqueos reseteados.");
    }
}
#endif