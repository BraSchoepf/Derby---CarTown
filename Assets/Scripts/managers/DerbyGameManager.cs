using UnityEngine;
using System.Collections.Generic;
using System;

public class DerbyGameManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerEntry
    {
        public string playerName;
        public VehicleHealth health;
        public bool isAlive = true;
        public int killCount = 0;
        public int placement = 0;       // 1 = ganador, se asigna al morir o al terminar la partida
        public float survivalStartTime;
        public string killedByName = "";
        public int humanSlotIndex = -1; // 0 = P1, 1 = P2, -1 = bot (sin panel de UI propio)
    }

    public List<PlayerEntry> players = new List<PlayerEntry>();
    public System.Action<string> OnMatchWon; // nombre del ganador
    public Action<PlayerEntry> OnPlayerEliminated;  // dispara el panel de derrota
    public Action<PlayerEntry> OnPlayerWon;         // dispara el panel de victoria

    bool matchEnded = false;

    public static DerbyGameManager Instance;

    void Awake() => Instance = this;

    public void RegisterPlayer(string name, VehicleHealth health, int humanSlotIndex = -1)
    {
        var entry = new PlayerEntry
        {
            playerName = name,
            health = health,
            survivalStartTime = Time.time,
            humanSlotIndex = humanSlotIndex
        };
        players.Add(entry);
        health.OnVehicleDestroyedByAttacker += (attacker) => HandlePlayerDestroyed(entry, attacker);
    }

    void HandlePlayerDestroyed(PlayerEntry entry, VehicleHealth attacker)
    {
        entry.isAlive = false;

        int stillAlive = players.FindAll(p => p.isAlive).Count;
        entry.placement = stillAlive + 1; // el resto que sigue en pie te supera en puesto

        PlayerEntry attackerEntry = attacker != null ? players.Find(p => p.health == attacker) : null;
        entry.killedByName = attackerEntry != null ? attackerEntry.playerName : "Choque";

        if (attackerEntry != null && attackerEntry != entry)
            attackerEntry.killCount++;

        Debug.Log($"{entry.playerName} fue eliminado — puesto {entry.placement}, por {entry.killedByName}");
        OnPlayerEliminated?.Invoke(entry);
        CheckForWinner();
    }

    void CheckForWinner()
    {
        if (matchEnded) return;

        var alive = players.FindAll(p => p.isAlive);
        if (alive.Count <= 1)
        {
            matchEnded = true;
            PlayerEntry winner = alive.Count == 1 ? alive[0] : null;

            if (winner != null)
            {
                winner.placement = 1;
                OnPlayerWon?.Invoke(winner);
            }

            string winnerName = winner != null ? winner.playerName : "Empate";
            Debug.Log($"Partida terminada. Ganador: {winnerName}");
            OnMatchWon?.Invoke(winnerName);
        }
    }
}