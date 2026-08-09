using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
public class ShaderSwatchUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;
    public GameObject selectionHighlight;
    public GameObject lockedOverlay;

    CarShaderVariantSO assignedVariant;
    public bool IsLocked { get; private set; }
    public event System.Action<ShaderSwatchUI> OnClicked;

    void Awake() { if (icon == null) icon = GetComponent<Image>(); }

    public void SetVariant(CarShaderVariantSO variant, int ownerPlayerSlotIndex)
    {
        assignedVariant = variant;
        if (icon != null && variant.previewIcon != null) icon.sprite = variant.previewIcon;

        IsLocked = !string.IsNullOrEmpty(variant.unlockRewardId)
                   && !UnlockRegistry.IsUnlocked(variant.unlockRewardId, ownerPlayerSlotIndex);

        if (lockedOverlay != null) lockedOverlay.SetActive(IsLocked);
    }

    public CarShaderVariantSO GetVariant() => assignedVariant;
    public void SetSelected(bool selected) { if (selectionHighlight != null) selectionHighlight.SetActive(selected); }
    public void OnPointerClick(PointerEventData e) { if (!IsLocked) OnClicked?.Invoke(this); }
}