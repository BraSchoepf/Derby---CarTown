using UnityEngine;
using UnityEngine.UI;
using System;

public class ColorSelectionPanelUI : MonoBehaviour
{
    public Color[] availableColors;
    public Button swatchButtonPrefab;
    public Transform swatchContainer;
    public int columns = 4;

    public event Action<Color> OnColorSelected;

    Button[] spawnedSwatches;
    int currentIndex = 0;

    void Awake() => BuildSwatches();

    void BuildSwatches()
    {
        spawnedSwatches = new Button[availableColors.Length];
        for (int i = 0; i < availableColors.Length; i++)
        {
            Button b = Instantiate(swatchButtonPrefab, swatchContainer);
            b.image.color = availableColors[i];
            int index = i;
            b.onClick.AddListener(() => SelectIndex(index));
            spawnedSwatches[i] = b;
        }
        SelectIndex(0, notify: false);
    }

    public Color Move(int deltaCol, int deltaRow)
    {
        int row = currentIndex / columns;
        int col = currentIndex % columns;
        int totalRows = Mathf.CeilToInt((float)availableColors.Length / columns);

        int newCol = Mathf.Clamp(col + deltaCol, 0, columns - 1);
        int newRow = Mathf.Clamp(row + deltaRow, 0, totalRows - 1);
        int newIndex = newRow * columns + newCol;

        // Si la última fila está incompleta, no dejamos caer en un índice inexistente
        newIndex = Mathf.Clamp(newIndex, 0, availableColors.Length - 1);

        SelectIndex(newIndex);
        return CurrentColor;
    }

    void SelectIndex(int index, bool notify = true)
    {
        currentIndex = index;
        RefreshHighlight();
        if (notify) OnColorSelected?.Invoke(CurrentColor);
    }

    void RefreshHighlight()
    {
        for (int i = 0; i < spawnedSwatches.Length; i++)
        {
            var outline = spawnedSwatches[i].GetComponent<Outline>();
            if (outline != null) outline.enabled = (i == currentIndex);
        }
    }

    public Color CurrentColor => availableColors[currentIndex];
}