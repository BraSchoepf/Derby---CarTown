using UnityEngine;

public class MapSpawnPoints : MonoBehaviour
{
    [Header("Jugadores (orden: [0] = P1, [1] = P2)")]
    public Transform[] playerSpawnPoints;

    [Header("Bots")]
    public Transform[] aiSpawnPoints;
}