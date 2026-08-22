using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WheelSwatchUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual")]
    public Image background;
    public Image icon;
    public Sprite defaultSprite;
    public Sprite hoveredSprite;

    [Header("Dueño de este panel (0 = P1, 1 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    public GameObject missionLockedOverlay;
    public bool IsLockedByMission { get; private set; }

    WheelVisualSO assignedWheel;
    public event System.Action<WheelSwatchUI> OnClicked;

    bool isSelected;

    void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
    }

    public void SetWheel(WheelVisualSO wheel)
    {
        assignedWheel = wheel;

        if (icon != null && wheel.previewIcon != null)
            icon.sprite = wheel.previewIcon;

        IsLockedByMission = !string.IsNullOrEmpty(wheel.unlockRewardId)
                              && !UnlockRegistry.IsUnlocked(wheel.unlockRewardId, ownerPlayerSlotIndex);

        if (missionLockedOverlay != null) missionLockedOverlay.SetActive(IsLockedByMission);

        Refresh();
    }

    public WheelVisualSO GetWheel() => assignedWheel;

    public void SetSelected(bool selected)
    {
        Debug.Log($"[WheelSwatchUI] {gameObject.name} SetSelected({selected}) — background={background}, hoveredSprite={hoveredSprite}");
        isSelected = selected;
        Refresh();
    }

    void Refresh()
    {
        if (background == null) return;
        background.sprite = isSelected ? hoveredSprite : defaultSprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsLockedByMission) return;
        OnClicked?.Invoke(this);
    }
}