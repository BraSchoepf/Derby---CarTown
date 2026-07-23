using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class DerbyGameManager : MonoBehaviour
{
    public static DerbyGameManager Instance;

    [System.Serializable]
    public class PlayerEntry
    {
        public string playerName;
        public VehicleHealth health;
        public bool isAlive = true;
        public int killCount = 0;
        public int placement = 0;
        public float survivalStartTime;
        public string killedByName = "";
        public int humanSlotIndex = -1;
        public TeamId team;
    }

    public List<PlayerEntry> players = new List<PlayerEntry>();

    public Action<string> OnMatchWon;
    public Action<PlayerEntry> OnPlayerEliminated;
    public Action<PlayerEntry> OnPlayerWon;

    bool matchEnded = false;
    bool teamsEnabled = false;

    void Awake() => Instance = this;

    public void SetTeamsEnabled(bool enabled) => teamsEnabled = enabled;

    public void RegisterPlayer(string name, VehicleHealth health, int humanSlotIndex = -1, TeamId team = default)
    {
        var entry = new PlayerEntry
        {
            playerName = name,
            health = health,
            survivalStartTime = Time.time,
            humanSlotIndex = humanSlotIndex,
            team = team
        };
        players.Add(entry);
        health.OnVehicleDestroyedByAttacker += (attacker) => HandlePlayerDestroyed(entry, attacker);
    }

    void HandlePlayerDestroyed(PlayerEntry entry, VehicleHealth attacker)
    {
        entry.isAlive = false;

        int stillAlive = players.FindAll(p => p.isAlive).Count;
        entry.placement = stillAlive + 1;

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

        if (teamsEnabled)
            CheckForTeamWinner();
        else
            CheckForIndividualWinner();
    }

    void CheckForIndividualWinner()
    {
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

    void CheckForTeamWinner()
    {
        var aliveTeams = players.Where(p => p.isAlive).Select(p => p.team).Distinct().ToList();

        if (aliveTeams.Count <= 1)
        {
            matchEnded = true;
            TeamId? winningTeam = aliveTeams.Count == 1 ? aliveTeams[0] : (TeamId?)null;

            if (winningTeam.HasValue)
            {
                foreach (var member in players.Where(p => p.team == winningTeam.Value))
                {
                    member.placement = 1;
                    OnPlayerWon?.Invoke(member);
                }
            }

            string winnerName = winningTeam.HasValue ? $"Equipo {winningTeam.Value}" : "Empate";
            Debug.Log($"Partida terminada. Ganador: {winnerName}");
            OnMatchWon?.Invoke(winnerName);
        }
    }
}