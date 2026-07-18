using UnityEngine;

public class CarSelectionGridUI : MonoBehaviour
{
    public CarRegistry registry;
    public CarSlotUI slotPrefab;
    public Transform gridParent;
    public int columns = 8;
    public bool includeRandomSlot = true;

    public PlayerCarCursor player1Cursor;
    public PlayerCarCursor player2Cursor;

    CarSlotUI[,] grid;
    int rows;

    void Awake() => BuildGrid();

    void BuildGrid()
    {
        int totalSlots = registry.cars.Length + (includeRandomSlot ? 1 : 0);
        rows = Mathf.CeilToInt((float)totalSlots / columns);
        grid = new CarSlotUI[rows, columns];

        int carIndex = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                int flat = r * columns + c;
                if (flat >= totalSlots) continue; // celda vacía, queda null en el array

                CarSlotUI slot = Instantiate(slotPrefab, gridParent);
                slot.SetGridPosition(r, c);

                slot.OnClicked += HandleSlotClicked;

                bool isRandom = includeRandomSlot && flat == 0; // dice en la primera celda, como en la referencia
                if (isRandom) slot.SetCarData(CarSlotType.Random, null);
                else slot.SetCarData(CarSlotType.Car, registry.cars[carIndex++].stats);

                grid[r, c] = slot;
            }
    }

    // Avanza en una dirección saltando celdas null hasta encontrar un slot válido o el borde
    public CarSlotUI GetNextSlot(int fromRow, int fromCol, int deltaRow, int deltaCol)
    {
        int r = fromRow, c = fromCol;
        int maxSteps = Mathf.Max(rows, columns);
        for (int i = 0; i < maxSteps; i++)
        {
            r += deltaRow; c += deltaCol;
            if (r < 0 || r >= rows || c < 0 || c >= columns) return null; // borde: no te movés
            if (grid[r, c] != null) return grid[r, c];
        }
        return null;
    }

    void HandleSlotClicked(CarSlotUI slot)
    {
        if (!player1Cursor.IsLocked) player1Cursor.OnSlotClicked(slot);
        else if (player2Cursor.gameObject.activeSelf && !player2Cursor.IsLocked) player2Cursor.OnSlotClicked(slot);
    }

    public CarSlotUI FirstSlot() => grid[0, 0];
}