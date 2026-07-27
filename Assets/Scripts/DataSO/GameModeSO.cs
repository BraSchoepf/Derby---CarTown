using UnityEngine;

public enum GameModeCategory { Racing, Demolition }

[CreateAssetMenu(fileName = "NewGameMode", menuName = "Modes/Game Mode")]
public class GameModeSO : ScriptableObject
{
    [Header("Identidad")]
    public string modeName;
    public Sprite icon;
    public GameModeCategory category;

    public DrivingProfileSO drivingProfile;

    [Header("Reglas generales")]
    public bool enableDamage = true;
    public bool requiresCheckpoints = false;
    public int lapsDefault = 1;
    public bool usesBombMechanic = false;

    [Header("Equipos")]
    public bool supportsTeams = false;
    public int[] teamSizeOptions;

    [Header("Bots")]
    public bool allowBots = true;
    [Tooltip("Si allowBots es true, cuántos bots como máximo puede spawnear este modo (0 = ilimitado, usa todos los spawn points disponibles)")]
    public int maxBots = 0;

    public bool enableEdgeDetection = false;
    public bool isDriftScoringMode = false;
}