using UnityEngine;

public enum GameMode { SinglePlayer, MultiplayerSplitScreen }

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    public GameMode selectedMode = GameMode.SinglePlayer;
    public CarStatsSO player1Car;
    public CarStatsSO player2Car; // null si es single player

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameSession duplicado detectado, destruyendo este y conservando el existente");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("GameSession.Awake — este es el Instance activo");
    }
}