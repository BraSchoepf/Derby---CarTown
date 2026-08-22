using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum CarSlotType { Car, Random }

public class CarSlotUI : MonoBehaviour, IPointerClickHandler
{
    public CarSlotType slotType;
    public CarStatsSO carStats;

    [Header("Visual")]
    public Image background;
    public Image icon;
    public Sprite defaultSprite;
    public Sprite hoveredSprite;

    [Header("Badges de jugador")]
    public GameObject p1Badge;
    public GameObject p2Badge;

    [Header("Bloqueo por misión (candado)")]
    public GameObject missionLockedOverlay;

    [Header("Dueño de este grid (0 = P1, 1 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    public int GridRow { get; private set; }
    public int GridCol { get; private set; }
    public event System.Action<CarSlotUI> OnClicked;

    bool p1Hover, p2Hover;
    int lockedBy;
    public bool IsLockedByMission { get; private set; }

    public void SetGridPosition(int r, int c) { GridRow = r; GridCol = c; }

    public void SetCarData(CarSlotType type, CarStatsSO stats)
    {
        slotType = type;
        carStats = stats;

        if (type == CarSlotType.Random)
        {
            icon.sprite = null;
            icon.enabled = false;
            IsLockedByMission = false;
        }
        else
        {
            icon.sprite = stats.previewImage;
            icon.enabled = stats.previewImage != null;
            IsLockedByMission = !string.IsNullOrEmpty(stats.unlockRewardId)
                                && !UnlockRegistry.IsUnlocked(stats.unlockRewardId, ownerPlayerSlotIndex);
        }

        if (missionLockedOverlay != null)
            missionLockedOverlay.SetActive(IsLockedByMission);

        Refresh();
    }

    public void SetHover(int player, bool hover)
    {
        if (IsLockedByMission) return;
        if (player == 1) p1Hover = hover; else p2Hover = hover;
        Refresh();
    }

    public void SetLocked(int player) { lockedBy = player; Refresh(); }

    void Refresh()
    {
        if (p1Badge != null) p1Badge.SetActive(p1Hover || lockedBy == 1);
        if (p2Badge != null) p2Badge.SetActive(p2Hover || lockedBy == 2);

        bool highlighted = p1Hover || p2Hover || lockedBy != 0;
        background.sprite = highlighted ? hoveredSprite : defaultSprite;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (IsLockedByMission) return;
        OnClicked?.Invoke(this);
    }
}