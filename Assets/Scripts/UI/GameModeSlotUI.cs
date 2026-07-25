using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameModeSlotUI : MonoBehaviour
{
    [Header("Datos")]
    public GameModeSO mode;

    [Header("UI del slot")]
    public Button selectButton;
    public TextMeshProUGUI modeNameText;
    public Image iconImage;

    [Header("Team config inline (solo si mode.supportsTeams)")]
    public GameObject teamConfigContainer;
    public TeamConfigUI teamConfigUI;

    System.Action<GameModeSO, GameModeSlotUI> onConfirmed;
    bool isExpanded = false;

    public void Setup(GameModeSO gameMode, System.Action<GameModeSO, GameModeSlotUI> onModeConfirmed)
    {
        mode = gameMode;
        onConfirmed = onModeConfirmed;

        if (modeNameText != null) modeNameText.text = mode.modeName;
        if (iconImage != null && mode.icon != null) iconImage.sprite = mode.icon;

        if (teamConfigContainer != null)
            teamConfigContainer.SetActive(false);

        if (teamConfigUI != null)
        {
            teamConfigUI.currentMode = mode;
            teamConfigUI.parentSlot = this;
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSlotClicked);
    }

    void OnSlotClicked()
    {
        if (!mode.supportsTeams)
        {
            onConfirmed?.Invoke(mode, this);
            return;
        }

        isExpanded = !isExpanded;
        teamConfigContainer.SetActive(isExpanded);
    }

    // Llamado por el botón "Confirmar" dentro del TeamConfigUI de este slot
    public void OnTeamConfigConfirmed()
    {
        onConfirmed?.Invoke(mode, this);
    }
}