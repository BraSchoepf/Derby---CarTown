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

    [Header("Dueño de este grid (1 = P1, 2 = P2)")]
    public int ownerPlayerSlotIndex = 1;

    CarSlotUI[,] grid;
    int rows;
    bool built; 

    void Awake()
    {
        if (!built)
        {
            BuildGrid();
            built = true;
        }
    }

    void BuildGrid()
    {
        int totalSlots = registry.cars.Length + (includeRandomSlot ? 1 : 0);
        rows = Mathf.CeilToInt((float)totalSlots / columns);
        grid = new CarSlotUI[rows, columns];

        bool wasActive = gridParent.gameObject.activeSelf;
        gridParent.gameObject.SetActive(false);

        int carIndex = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                int flat = r * columns + c;
                if (flat >= totalSlots) continue;

                CarSlotUI slot = Instantiate(slotPrefab, gridParent);
                slot.SetGridPosition(r, c);
                slot.ownerPlayerSlotIndex = ownerPlayerSlotIndex;
                slot.OnClicked += HandleSlotClicked;

                bool isRandom = includeRandomSlot && flat == 0;
                if (isRandom)
                {
                    slot.SetCarData(CarSlotType.Random, null);
                }
                else
                {
                    CarStatsSO car = registry.cars[carIndex++].stats;
                    slot.SetCarData(CarSlotType.Car, car);
                }

                grid[r, c] = slot;
            }

        gridParent.gameObject.SetActive(wasActive);
    }

    // Avanza en una dirección saltando celdas null hasta encontrar un slot válido o el borde
    public CarSlotUI GetNextSlot(int fromRow, int fromCol, int deltaRow, int deltaCol)
    {
        int totalRows = grid.GetLength(0);
        int totalCols = grid.GetLength(1);

        if (deltaCol != 0)
        {
            int col = fromCol;
            for (int i = 0; i < totalCols; i++)
            {
                col = ((col + deltaCol) % totalCols + totalCols) % totalCols;
                if (grid[fromRow, col] != null) return grid[fromRow, col];
            }
        }

        if (deltaRow != 0)
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


    public void RefreshLockStates()
    {
        if (grid == null) return;
        foreach (var slot in grid)
        {
            if (slot != null && slot.slotType == CarSlotType.Car)
                slot.SetCarData(slot.slotType, slot.carStats);
        }
    }
}