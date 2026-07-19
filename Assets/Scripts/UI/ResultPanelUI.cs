using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultPanelUI : MonoBehaviour
{
    [Header("Encabezados (mutuamente excluyentes)")]
    public GameObject victoryHeader;
    public GameObject defeatHeader;

    [Header("Resumen")]
    public TextMeshProUGUI placementText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI killedByText; // solo visible en derrota

    [Header("Navegación")]
    public Button backToMenuButton;
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMenu);
        gameObject.SetActive(false); // arranca oculto, lo prende DerbyGameManager al eliminar/ganar
    }

    public void ShowVictory(DerbyGameManager.PlayerEntry entry)
    {
        gameObject.SetActive(true);
        victoryHeader.SetActive(true);
        defeatHeader.SetActive(false);
        killedByText.gameObject.SetActive(false);
        FillSummary(entry);
    }

    public void ShowDefeat(DerbyGameManager.PlayerEntry entry)
    {
        gameObject.SetActive(true);
        victoryHeader.SetActive(false);
        defeatHeader.SetActive(true);
        killedByText.gameObject.SetActive(true);
        killedByText.text = $"Eliminado por: {entry.killedByName}";
        FillSummary(entry);
    }

    void FillSummary(DerbyGameManager.PlayerEntry entry)
    {
        placementText.text = $"Puesto {entry.placement}°";
        killsText.text = $"Eliminaciones: {entry.killCount}";

        float survived = Time.time - entry.survivalStartTime;
        int minutes = Mathf.FloorToInt(survived / 60f);
        int seconds = Mathf.FloorToInt(survived % 60f);
        survivalTimeText.text = $"Tiempo en pie: {minutes:00}:{seconds:00}";
    }

    void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}