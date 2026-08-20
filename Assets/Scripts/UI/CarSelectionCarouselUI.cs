using UnityEngine;
using System.Collections.Generic;

public class CarSelectionCarouselUI : MonoBehaviour
{
    public CarRegistry registry;
    public CarSlotUI slotPrefab;
    public Transform carouselContainer;
    public bool includeRandomSlot = true;

    [Header("Dueño de este carrusel (1 = P1, 2 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    [Header("Layout")]
    public float spacingX = 220f;
    public float sideScale = 0.7f;
    public float sideAlpha = 0.4f;
    public int maxVisibleNeighbors = 2;

    public event System.Action<CarSlotUI> OnSlotClicked;
    public CarSlotUI CurrentSlot => (slots != null && slots.Length > 0) ? slots[currentIndex] : null;

    CarSlotUI[] slots;
    int currentIndex = 0;
    bool built;

    void Awake()
    {
        if (!built) { BuildSlots(); built = true; }
    }

    void BuildSlots()
    {
        int totalSlots = registry.cars.Length + (includeRandomSlot ? 1 : 0);
        slots = new CarSlotUI[totalSlots];

        bool wasActive = carouselContainer.gameObject.activeSelf;
        carouselContainer.gameObject.SetActive(false);

        int carIndex = 0;
        for (int i = 0; i < totalSlots; i++)
        {
            CarSlotUI slot = Instantiate(slotPrefab, carouselContainer);
            slot.SetGridPosition(0, i); // fila única — se mantiene por compatibilidad con CarSlotUI
            slot.ownerPlayerSlotIndex = ownerPlayerSlotIndex;
            slot.OnClicked += HandleSlotClicked;

            bool isRandom = includeRandomSlot && i == 0;
            if (isRandom)
                slot.SetCarData(CarSlotType.Random, null);
            else
                slot.SetCarData(CarSlotType.Car, registry.cars[carIndex++].stats);

            slots[i] = slot;
        }

        carouselContainer.gameObject.SetActive(wasActive);
        RefreshVisuals();
    }

    public CarSlotUI Move(int direction)
    {
        if (slots == null || slots.Length == 0) return null;
        currentIndex = (currentIndex + direction + slots.Length) % slots.Length;
        RefreshVisuals();
        return CurrentSlot;
    }

    public CarSlotUI SelectSlot(CarSlotUI slot)
    {
        int idx = System.Array.IndexOf(slots, slot);
        if (idx < 0) return CurrentSlot;
        currentIndex = idx;
        RefreshVisuals();
        return CurrentSlot;
    }

    void RefreshVisuals()
    {
        var order = new List<int>();
        for (int i = 0; i < slots.Length; i++) if (slots[i] != null) order.Add(i);
        order.Sort((a, b) =>
        {
            int offsetA = Mathf.Abs(GetShortestOffset(a, currentIndex, slots.Length));
            int offsetB = Mathf.Abs(GetShortestOffset(b, currentIndex, slots.Length));
            return offsetB.CompareTo(offsetA);
        });

        foreach (int i in order)
        {
            int offset = GetShortestOffset(i, currentIndex, slots.Length);
            int absOffset = Mathf.Abs(offset);
            RectTransform rt = slots[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(offset * spacingX, 0f);

            float scaleT = Mathf.Clamp01((float)absOffset / maxVisibleNeighbors);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, sideScale, scaleT);

            var canvasGroup = slots[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = slots[i].gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = absOffset > maxVisibleNeighbors ? 0f : Mathf.Lerp(1f, sideAlpha, scaleT);
            canvasGroup.blocksRaycasts = absOffset == 0;

            slots[i].transform.SetAsLastSibling();
        }
    }

    void HandleSlotClicked(CarSlotUI slot) => OnSlotClicked?.Invoke(slot);

    public void RefreshLockStates()
    {
        if (slots == null) return;
        foreach (var slot in slots)
            if (slot != null && slot.slotType == CarSlotType.Car)
                slot.SetCarData(slot.slotType, slot.carStats);
    }

    int GetShortestOffset(int index, int center, int count)
    {
        int raw = index - center;
        if (raw > count / 2) raw -= count;
        if (raw < -count / 2) raw += count;
        return raw;
    }
}