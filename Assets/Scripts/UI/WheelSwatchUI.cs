using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WheelSwatchUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;
    public GameObject selectionHighlight;

    WheelVisualSO assignedWheel;
    public event System.Action<WheelSwatchUI> OnClicked;

    [Header("Dueño de este panel (0 = P1, 1 = P2)")]
    public int ownerPlayerSlotIndex = 0;

    public GameObject missionLockedOverlay;
    public bool IsLockedByMission { get; private set; }
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
    }

    public WheelVisualSO GetWheel() => assignedWheel;

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null) selectionHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(this);
}