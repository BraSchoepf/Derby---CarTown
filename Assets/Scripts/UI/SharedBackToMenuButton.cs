using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SharedBackToMenuButton : MonoBehaviour
{
    public Button backToMenuButton;
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        gameObject.SetActive(false);
        backToMenuButton.onClick.AddListener(BackToMenu);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}