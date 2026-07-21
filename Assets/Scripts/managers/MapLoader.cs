using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MapLoader : MonoBehaviour
{
    public static MapLoader Instance;

    [Header("UI de carga (opcional)")]
    public GameObject loadingScreen;
    public UnityEngine.UI.Slider loadingBar;

    public System.Action OnMapReady;

    Scene loadedMapScene;
    MapSpawnPoints spawnPoints;
    public bool IsMapReady { get; private set; }

    void Awake()
    {
        Instance = this;
        StartCoroutine(LoadMapRoutine());
    }

    IEnumerator LoadMapRoutine()
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);

        string sceneName = GameSession.Instance != null ? GameSession.Instance.selectedMapSceneName : null;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[MapLoader] No hay mapa seleccionado en GameSession.", this);
            yield break;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!op.isDone)
        {
            if (loadingBar != null) loadingBar.value = op.progress;
            yield return null;
        }

        loadedMapScene = SceneManager.GetSceneByName(sceneName);

        spawnPoints = FindSpawnPointsInScene(loadedMapScene);
        if (spawnPoints == null)
            Debug.LogError($"[MapLoader] La escena '{sceneName}' no tiene MapSpawnPoints.", this);

        if (loadingScreen != null) loadingScreen.SetActive(false);

        IsMapReady = true;
        OnMapReady?.Invoke();
    }

    MapSpawnPoints FindSpawnPointsInScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<MapSpawnPoints>();
            if (found != null) return found;
        }
        return null;
    }

    public Transform GetPlayerSpawn(int slotIndex) =>
        spawnPoints != null && slotIndex < spawnPoints.playerSpawnPoints.Length
            ? spawnPoints.playerSpawnPoints[slotIndex]
            : null;

    public Transform[] GetAISpawnPoints() =>
        spawnPoints != null ? spawnPoints.aiSpawnPoints : new Transform[0];
}