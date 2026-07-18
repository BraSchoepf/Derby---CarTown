using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum CarSlotType { Car, Random }

public class CarSlotUI : MonoBehaviour, IPointerClickHandler
{
    public CarSlotType slotType;
    public CarStatsSO carStats; // null si es el slot Random

    [Header("Visual")]
    public Image background;
    public Image icon; // ← este faltaba
    public Color idleColor, p1Color, p2Color, bothColor, lockedColor;

    [Header("Badges de jugador")]
    public GameObject p1Badge;
    public GameObject p2Badge;

    public int GridRow { get; private set; }
    public int GridCol { get; private set; }
    public event System.Action<CarSlotUI> OnClicked;

    bool p1Hover, p2Hover;
    int lockedBy;

    public void SetGridPosition(int r, int c) { GridRow = r; GridCol = c; }

    public void SetCarData(CarSlotType type, CarStatsSO stats)
    {
        slotType = type;
        carStats = stats;

        if (type == CarSlotType.Random)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
        else
        {
            icon.sprite = stats.previewImage;
            icon.enabled = stats.previewImage != null;
        }
    }

    public void SetHover(int player, bool hover)
    {
        if (player == 1) p1Hover = hover; else p2Hover = hover;
        Refresh();
    }

    public void SetLocked(int player) { lockedBy = player; Refresh(); }

    void Refresh()
    {
        if (p1Badge != null) p1Badge.SetActive(p1Hover || lockedBy == 1);
        if (p2Badge != null) p2Badge.SetActive(p2Hover || lockedBy == 2);

        if (lockedBy != 0) { background.color = lockedColor; return; }
        background.color = (p1Hover && p2Hover) ? bothColor
                          : p1Hover ? p1Color
                          : p2Hover ? p2Color
                          : idleColor;
    }

    public void OnPointerClick(PointerEventData e) => OnClicked?.Invoke(this);
}