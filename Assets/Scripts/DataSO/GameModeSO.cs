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

    [Header("Badge opcional (ej: 'NUEVO', ícono especial)")]
    public bool hasBadge = false;
    public Sprite badgeSprite;
    public string badgeText;
    public bool isLocked = false;

    [Header("Knockback entre autos (VehicleImpactFeedback)")]
    [Tooltip("Si está tildado, este modo activa el knockback fuerte al chocar autos. Si NO está tildado, VehicleImpactFeedback queda desactivado por completo en este modo.")]
    public bool enableKnockbackFeedback = false;
    public float knockbackMultiplierOverride = 4f;
    public float maxKnockbackForceOverride = 15f;
}