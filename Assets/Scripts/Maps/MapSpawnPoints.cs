using UnityEngine;

public class MapSpawnPoints : MonoBehaviour
{
    [Header("Demolición")]
    [Tooltip("Puntos de spawn para jugadores en modo Demolición (distribuidos por el arena)")]
    public Transform[] demolitionPlayerSpawnPoints;
    public Transform[] demolitionAISpawnPoints;

    [Header("Racing")]
    [Tooltip("Puntos de spawn tipo grid de largada, en orden de posición de salida")]
    public Transform[] racePlayerSpawnPoints;
    public Transform[] raceAISpawnPoints;
}