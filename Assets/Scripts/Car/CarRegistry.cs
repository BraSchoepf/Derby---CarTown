using UnityEngine;

[CreateAssetMenu(fileName = "CarRegistry", menuName = "Cars/Car Registry")]
public class CarRegistry : ScriptableObject
{
    [System.Serializable]
    public class CarEntry
    {
        public CarStatsSO stats;
        public GameObject prefab;
    }

    public CarEntry[] cars;

    public GameObject GetPrefabForStats(CarStatsSO stats)
    {
        foreach (var entry in cars)
        {
            if (entry.stats == stats)
                return entry.prefab;
        }
        Debug.LogError($"No se encontró prefab para {stats.name} en CarRegistry");
        return null;
    }
}