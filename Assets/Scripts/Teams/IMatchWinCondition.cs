using System.Collections.Generic;
using System.Linq;

public interface IMatchWinCondition
{
    bool CheckForWinner(List<DerbyGameManager.PlayerEntry> players, out TeamId? winner);
}

public class LastTeamStandingCondition : IMatchWinCondition
{
    public bool CheckForWinner(List<DerbyGameManager.PlayerEntry> players, out TeamId? winner)
    {
        var aliveTeams = players.Where(p => p.isAlive).Select(p => p.team).Distinct().ToList();

        if (aliveTeams.Count <= 1)
        {
            winner = aliveTeams.Count == 1 ? aliveTeams[0] : (TeamId?)null;
            return true; // la partida terminó
        }

        winner = null;
        return false; // todavía no hay ganador
    }
}

public class TeamScoreThresholdCondition : IMatchWinCondition
{
    public int scoreToWin = 5;
    Dictionary<TeamId, int> teamScores = new Dictionary<TeamId, int>();

    public void AddScore(TeamId team, int amount)
    {
        if (!teamScores.ContainsKey(team)) teamScores[team] = 0;
        teamScores[team] += amount;
    }

    public bool CheckForWinner(List<DerbyGameManager.PlayerEntry> players, out TeamId? winner)
    {
        foreach (var kvp in teamScores)
        {
            if (kvp.Value >= scoreToWin)
            {
                winner = kvp.Key;
                return true;
            }
        }

        winner = null;
        return false;
    }
}