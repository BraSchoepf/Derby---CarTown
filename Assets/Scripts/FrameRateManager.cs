using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    public static FrameRateManager Instance;

    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool disableVSync = true;

    private void Awake()
    {
        // Evita duplicados
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplySettings();
    }

    private void ApplySettings()
    {
        if (disableVSync)
            QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = targetFrameRate;
    }
}