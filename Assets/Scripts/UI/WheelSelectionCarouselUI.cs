using UnityEngine;
using System.Collections.Generic;

public class WheelSelectionCarouselUI : MonoBehaviour
{
    public WheelVisualSO[] availableWheels;
    public WheelSwatchUI swatchPrefab;
    public Transform carouselContainer;

    [Header("Dueño de este carrusel (0 = P1, 1 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    [Header("Layout")]
    public float spacingX = 220f;
    public float sideScale = 0.7f;
    public float sideAlpha = 0.4f;
    public int maxVisibleNeighbors = 2;

    public event System.Action<WheelVisualSO> OnWheelSelected;
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
            int index = i;
            swatch.OnClicked += (s) => SelectIndex(index);
            swatches[i] = swatch;
        }

        carouselContainer.gameObject.SetActive(wasActive);
        SelectIndex(0, notify: false);
    }

    public WheelVisualSO Move(int direction)
    {
        if (swatches == null || swatches.Length == 0) return null;
        SelectIndex((currentIndex + direction + swatches.Length) % swatches.Length);
        return CurrentWheel;
    }

    void SelectIndex(int index, bool notify = true)
    {
        currentIndex = index;
        RefreshVisuals();
        if (notify) OnWheelSelected?.Invoke(CurrentWheel);
    }

    void RefreshVisuals()
    {
        var order = new List<int>();
        for (int i = 0; i < swatches.Length; i++) if (swatches[i] != null) order.Add(i);
        order.Sort((a, b) =>
        {
            int offsetA = Mathf.Abs(GetShortestOffset(a, currentIndex, swatches.Length));
            int offsetB = Mathf.Abs(GetShortestOffset(b, currentIndex, swatches.Length));
            return offsetB.CompareTo(offsetA);
        });

        foreach (int i in order)
        {
            int offset = GetShortestOffset(i, currentIndex, swatches.Length);
            int absOffset = Mathf.Abs(offset);
            RectTransform rt = swatches[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(offset * spacingX, 0f);

            float scaleT = Mathf.Clamp01((float)absOffset / maxVisibleNeighbors);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, sideScale, scaleT);

            var canvasGroup = swatches[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = swatches[i].gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = absOffset > maxVisibleNeighbors ? 0f : Mathf.Lerp(1f, sideAlpha, scaleT);
            canvasGroup.blocksRaycasts = absOffset == 0;

            swatches[i].transform.SetAsLastSibling();
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