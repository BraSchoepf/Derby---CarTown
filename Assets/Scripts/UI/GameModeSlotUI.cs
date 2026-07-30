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

    [Header("Badge opcional")]
    public GameObject badgeContainer; // el objeto que agrupa imagen + texto del badge
    public Image badgeImage;
    public TextMeshProUGUI badgeText;

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

        SetupBadge();

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

    void SetupBadge()
    {
        if (badgeContainer == null) return;

        badgeContainer.SetActive(mode.hasBadge);

        if (!mode.hasBadge) return;

        if (badgeImage != null)
        {
            badgeImage.sprite = mode.badgeSprite;
            badgeImage.gameObject.SetActive(mode.badgeSprite != null);
        }

        if (badgeText != null)
        {
            badgeText.text = mode.badgeText;
            badgeText.gameObject.SetActive(!string.IsNullOrEmpty(mode.badgeText));
        }
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

    public void OnTeamConfigConfirmed()
    {
        onConfirmed?.Invoke(mode, this);
    }
}