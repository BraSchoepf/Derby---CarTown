using UnityEngine;

public class WheelSelectionPanelUI : MonoBehaviour
{
    public WheelVisualSO[] availableWheels;
    public WheelSwatchUI swatchPrefab;
    public Transform swatchContainer;
    public int columns = 4;

    public event System.Action<WheelVisualSO> OnWheelSelected;

    WheelSwatchUI[] spawnedSwatches;
    int currentIndex = 0;

    void Awake() => BuildSwatches();

    void BuildSwatches()
    {
        if (availableWheels == null || availableWheels.Length == 0)
        {
            Debug.LogError("[WheelSelectionPanelUI] 'Available Wheels' está vacío.", this);
            return;
        }

        spawnedSwatches = new WheelSwatchUI[availableWheels.Length];
        for (int i = 0; i < availableWheels.Length; i++)
        {
            WheelSwatchUI swatch = Instantiate(swatchPrefab, swatchContainer);
            swatch.SetWheel(availableWheels[i]);

            int index = i;
            swatch.OnClicked += (s) => SelectIndex(index);

            spawnedSwatches[i] = swatch;
        }
        SelectIndex(0, notify: false);
    }

    public WheelVisualSO Move(int deltaCol, int deltaRow)
    {
        int row = currentIndex / columns;
        int col = currentIndex % columns;
        int totalRows = Mathf.CeilToInt((float)availableWheels.Length / columns);

        int newCol = Mathf.Clamp(col + deltaCol, 0, columns - 1);
        int newRow = Mathf.Clamp(row + deltaRow, 0, totalRows - 1);
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

    public WheelVisualSO CurrentWheel => availableWheels[currentIndex];
}