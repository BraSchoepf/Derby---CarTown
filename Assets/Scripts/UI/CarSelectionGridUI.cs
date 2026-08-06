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
                if (flat >= totalSlots) continue;

                CarSlotUI slot = Instantiate(slotPrefab, gridParent);
                slot.SetGridPosition(r, c);
                slot.OnClicked += HandleSlotClicked;

                bool isRandom = includeRandomSlot && flat == 0;
                if (isRandom)
                {
                    slot.SetCarData(CarSlotType.Random, null);
                }
                else
                {
                    CarStatsSO car = registry.cars[carIndex++].stats; // se declara ACÁ, antes de usarla
                    slot.SetCarData(CarSlotType.Car, car);
                    // SetCarData ya calcula IsLockedByMission internamente (según el fix de CarSlotUI),
                    // no hace falta calcularlo de nuevo acá
                }

                grid[r, c] = slot;
            }
    }

    // Avanza en una dirección saltando celdas null hasta encontrar un slot válido o el borde
    public CarSlotUI GetNextSlot(int fromRow, int fromCol, int deltaRow, int deltaCol)
    {
        int totalRows = grid.GetLength(0);
        int totalCols = grid.GetLength(1);

        if (deltaCol != 0) // movimiento horizontal — wrap dentro de la MISMA fila
        {
            int col = fromCol;
            for (int i = 0; i < totalCols; i++)
            {
                col = ((col + deltaCol) % totalCols + totalCols) % totalCols; // wrap circular
                if (grid[fromRow, col] != null) return grid[fromRow, col];
            }
        }

        if (deltaRow != 0) // movimiento vertical — wrap dentro de la MISMA columna
        {
            int row = fromRow;
            for (int i = 0; i < totalRows; i++)
            {
                row = ((row + deltaRow) % totalRows + totalRows) % totalRows;
                if (grid[row, fromCol] != null) return grid[row, fromCol];
            }
        }

        return null;
    }

    void HandleSlotClicked(CarSlotUI slot)
    {
        if (!player1Cursor.IsLocked) player1Cursor.OnSlotClicked(slot);
        else if (player2Cursor.gameObject.activeSelf && !player2Cursor.IsLocked) player2Cursor.OnSlotClicked(slot);
    }

    public CarSlotUI FirstSlot()
    {
        if (grid == null || grid.Length == 0) return null;
        return grid[0, 0];
    }
}