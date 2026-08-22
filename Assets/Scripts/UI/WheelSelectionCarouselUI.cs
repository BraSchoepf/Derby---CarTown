using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class WheelSelectionCarouselUI : MonoBehaviour
{
    public WheelVisualSO[] availableWheels;
    public WheelSwatchUI swatchPrefab;
    public Transform carouselContainer;

    [Header("Dueño de este carrusel (1 = P1, 2 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    public float spacingX = 220f;
    public int maxVisibleNeighbors = 2;

    public bool CurrentWheelIsLocked => currentIndex >= 0 && swatches[currentIndex].IsLockedByMission;
    public WheelVisualSO CurrentWheel => currentIndex >= 0 ? availableWheels[currentIndex] : null;

    WheelSwatchUI[] swatches;
    int currentIndex = 0;
    bool built;

    void Awake()
    {
        if (!built) { BuildSwatches(); built = true; }
    }

    void BuildSwatches()
    {
        if (availableWheels == null || availableWheels.Length == 0)
        {
            Debug.LogError("[WheelSelectionCarouselUI] 'Available Wheels' está vacío.", this);
            return;
        }

        bool wasActive = carouselContainer.gameObject.activeSelf;
        carouselContainer.gameObject.SetActive(false);

        swatches = new WheelSwatchUI[availableWheels.Length];
        for (int i = 0; i < availableWheels.Length; i++)
        {
            WheelSwatchUI swatch = Instantiate(swatchPrefab, carouselContainer);
            swatch.ownerPlayerSlotIndex = ownerPlayerSlotIndex;
            swatch.SetWheel(availableWheels[i]);
            swatches[i] = swatch;
        }

        carouselContainer.gameObject.SetActive(wasActive);
        RefreshPositions();
    }

    public WheelVisualSO Move(int direction)
    {
        if (swatches == null || swatches.Length == 0) return null;
        currentIndex = Mathf.Clamp(currentIndex + direction, 0, swatches.Length - 1);
        RefreshPositions();
        return CurrentWheel;
    }

    void RefreshPositions()
    {
        for (int i = 0; i < swatches.Length; i++)
        {
            int offset = GetShortestOffset(i, currentIndex, swatches.Length);
            int absOffset = Mathf.Abs(offset);

            RectTransform rect = swatches[i].GetComponent<RectTransform>();

            rect.anchoredPosition = new Vector2(
                offset * spacingX,
                0f
            );

            swatches[i].gameObject.SetActive(
                absOffset <= maxVisibleNeighbors
            );

            // ESTE ES EL HOVER
            swatches[i].SetSelected(i == currentIndex);
        }
    }

    public void RefreshLockStates()
    {
        if (swatches == null) return;
        for (int i = 0; i < swatches.Length; i++)
            swatches[i].SetWheel(availableWheels[i]);
    }

    int GetShortestOffset(int index, int center, int count)
    {
        int raw = index - center;
        if (raw > count / 2) raw -= count;
        if (raw < -count / 2) raw += count;
        return raw;
    }
}