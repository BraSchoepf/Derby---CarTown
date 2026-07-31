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
    public GameObject badgeContainer;
    public Image badgeImage;
    public TextMeshProUGUI badgeText;
    public GameObject lockedOverlay;

    System.Action<GameModeSO> onSelected;

    public void Setup(GameModeSO gameMode, System.Action<GameModeSO> onModeSelected)
    {
        mode = gameMode;
        onSelected = onModeSelected;

        if (modeNameText != null) modeNameText.text = mode.modeName;
        if (iconImage != null && mode.icon != null) iconImage.sprite = mode.icon;

        SetupBadge();
        SetupLocked();

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelected?.Invoke(mode));
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
    void SetupLocked()
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(mode.isLocked);
    }
}