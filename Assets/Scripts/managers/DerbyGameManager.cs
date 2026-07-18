using UnityEngine;
using System.Collections.Generic;

public class DerbyGameManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerEntry
    {
        public string playerName;
        public VehicleHealth health;
        public bool isAlive = true;
    }

    public List<PlayerEntry> players = new List<PlayerEntry>();
    public System.Action<string> OnMatchWon; // nombre del ganador

    bool matchEnded = false;

    public void RegisterPlayer(string name, VehicleHealth health)
    {
        var entry = new PlayerEntry { playerName = name, health = health };
        players.Add(entry);
        health.OnVehicleDestroyed += () => HandlePlayerDestroyed(entry);
    }

    void HandlePlayerDestroyed(PlayerEntry entry)
    {
        entry.isAlive = false;
        Debug.Log($"{entry.playerName} fue eliminado");
        CheckForWinner();
    }

    void CheckForWinner()
    {
        if (matchEnded) return;

        var aliveCount = players.FindAll(p => p.isAlive).Count;

        if (aliveCount <= 1)
        {
            matchEnded = true;
            var winner = players.Find(p => p.isAlive);
            string winnerName = winner != null ? winner.playerName : "Empate";
            Debug.Log($"Partida terminada. Ganador: {winnerName}");
            OnMatchWon?.Invoke(winnerName);
        }
    }
}