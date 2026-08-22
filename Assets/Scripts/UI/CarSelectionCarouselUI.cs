using UnityEngine;

public class CarSelectionCarouselUI : MonoBehaviour
{
    public CarRegistry registry;
    public CarSlotUI slotPrefab;
    public Transform carouselContainer;
    public bool includeRandomSlot = true;

    [Header("Dueño de este carrusel (1 = P1, 2 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    public float spacingX = 220f;
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
            slot.SetGridPosition(0, i);
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
        RefreshPositions();
    }

    public CarSlotUI Move(int direction)
    {
        if (slots == null || slots.Length == 0) return null;
        currentIndex = Mathf.Clamp(currentIndex + direction, 0, slots.Length - 1);
        RefreshPositions();
        return CurrentSlot;
    }

    public CarSlotUI SelectSlot(CarSlotUI slot)
    {
        int idx = System.Array.IndexOf(slots, slot);
        if (idx < 0) return CurrentSlot;
        currentIndex = idx;
        RefreshPositions();
        return CurrentSlot;
    }

    void RefreshPositions()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int offset = GetShortestOffset(i, currentIndex, slots.Length);
            int absOffset = Mathf.Abs(offset);

            slots[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(offset * spacingX, 0f);
            slots[i].gameObject.SetActive(absOffset <= maxVisibleNeighbors);
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