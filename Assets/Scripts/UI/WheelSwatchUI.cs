using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WheelSwatchUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;
    public GameObject selectionHighlight;

    WheelVisualSO assignedWheel;
    public event System.Action<WheelSwatchUI> OnClicked;

    void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
    }

    public void SetWheel(WheelVisualSO wheel)
    {
        assignedWheel = wheel;
        if (icon != null && wheel.previewIcon != null)
            icon.sprite = wheel.previewIcon;
    }

    public WheelVisualSO GetWheel() => assignedWheel;

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null) selectionHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(this);
}