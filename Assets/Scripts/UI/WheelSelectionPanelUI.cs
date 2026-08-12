using UnityEngine;

public class WheelSelectionPanelUI : MonoBehaviour
{
    public WheelVisualSO[] availableWheels;
    public WheelSwatchUI swatchPrefab;
    public Transform swatchContainer;
    public int columns = 4;

    [Header("Dueño de este panel (0 = P1, 1 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    public event System.Action<WheelVisualSO> OnWheelSelected;
    public bool CurrentWheelIsLocked => currentIndex >= 0 && spawnedSwatches[currentIndex].IsLockedByMission;

    WheelSwatchUI[] spawnedSwatches;
    int currentIndex = -1;
    bool built; 

    void Awake()
    {
        if (!built)
        {
            BuildSwatches();
            built = true;
        }
    }

    void BuildSwatches()
    {
        if (availableWheels == null || availableWheels.Length == 0)
        {
            Debug.LogError("[WheelSelectionPanelUI] 'Available Wheels' está vacío.", this);
            return;
        }

        bool wasActive = swatchContainer.gameObject.activeSelf;
        swatchContainer.gameObject.SetActive(false);

        spawnedSwatches = new WheelSwatchUI[availableWheels.Length];
        for (int i = 0; i < availableWheels.Length; i++)
        {
            WheelSwatchUI swatch = Instantiate(swatchPrefab, swatchContainer);
            swatch.ownerPlayerSlotIndex = ownerPlayerSlotIndex;
            swatch.SetWheel(availableWheels[i]);
            int index = i;
            swatch.OnClicked += (s) => SelectIndex(index);
            spawnedSwatches[i] = swatch;
        }

        swatchContainer.gameObject.SetActive(wasActive);

        RefreshHighlight();
    }

    public WheelVisualSO Move(int deltaCol, int deltaRow)
    {
        if (currentIndex < 0)
        {
            SelectIndex(0);
            return CurrentWheel;
        }

        int row = currentIndex / columns;
        int col = currentIndex % columns;
        int totalRows = Mathf.CeilToInt((float)availableWheels.Length / columns);

        int newCol = ((col + deltaCol) % columns + columns) % columns;
        int newRow = ((row + deltaRow) % totalRows + totalRows) % totalRows;

        int newIndex = newRow * columns + newCol;
        newIndex = Mathf.Clamp(newIndex, 0, availableWheels.Length - 1);

        SelectIndex(newIndex);
        return CurrentWheel;
    }

    void SelectIndex(int index, bool notify = true)
    {
        currentIndex = index;
        RefreshHighlight();
        if (notify) OnWheelSelected?.Invoke(CurrentWheel);
    }

    void RefreshHighlight()
    {
        for (int i = 0; i < spawnedSwatches.Length; i++)
            spawnedSwatches[i].SetSelected(i == currentIndex);
    }

    public void RefreshLockStates()
    {
        if (spawnedSwatches == null) return;
        for (int i = 0; i < spawnedSwatches.Length; i++)
            spawnedSwatches[i].SetWheel(availableWheels[i]);
    }

    public WheelVisualSO CurrentWheel => currentIndex >= 0 ? availableWheels[currentIndex] : null;
}