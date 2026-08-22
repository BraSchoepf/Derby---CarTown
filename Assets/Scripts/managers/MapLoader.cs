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
    RaceCourseSet raceCourseSet;

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

        spawnPoints = FindInScene<MapSpawnPoints>(loadedMapScene);
        if (spawnPoints == null)
            Debug.LogError($"[MapLoader] La escena '{sceneName}' no tiene MapSpawnPoints.", this);

        // RaceCourseSet es opcional — solo lo necesitan mapas compatibles con modos de Racing.
        // No es un error si un mapa de Demolición no lo tiene.
        raceCourseSet = FindInScene<RaceCourseSet>(loadedMapScene);

        if (loadingScreen != null) loadingScreen.SetActive(false);

        IsMapReady = true;
        OnMapReady?.Invoke();
    }

    T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>();
            if (found != null) return found;
        }
        return null;
    }

    public Transform GetPlayerSpawn(int slotIndex, GameModeCategory category)
    {
        Transform[] points = category == GameModeCategory.Racing
            ? spawnPoints.racePlayerSpawnPoints
            : spawnPoints.demolitionPlayerSpawnPoints;

        return spawnPoints != null && slotIndex < points.Length ? points[slotIndex] : null;
    }

    public Transform[] GetAISpawnPoints(GameModeCategory category)
    {
        if (spawnPoints == null) return new Transform[0];

        return category == GameModeCategory.Racing
            ? spawnPoints.raceAISpawnPoints
            : spawnPoints.demolitionAISpawnPoints;
    }

    public RaceCourseSet GetRaceCourseSet() => raceCourseSet;
}