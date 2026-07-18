using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarCursor : MonoBehaviour
{
    public int playerIndex = 1;
    public CarSelectionGridUI grid;
    public CarPreviewRenderer preview;
    public CarStatsPanelUI statsPanel;

    CarSlotUI current, locked;
    CarStatsSO lastRandomPick;

    public bool IsLocked => locked != null;
    public CarStatsSO SelectedCar =>
        locked == null ? null :
        locked.slotType == CarSlotType.Random ? lastRandomPick : locked.carStats;

    void Start()
    {
        if (grid == null)
        {
            Debug.LogError($"[PlayerCarCursor P{playerIndex}] Falta asignar 'Grid' en el Inspector.", this);
            enabled = false;
            return;
        }
        MoveTo(grid.FirstSlot());
    }

    void Update()
    {
        if (Keyboard.current == null) return; // no hay teclado conectado, no hay nada que leer

        if (current == null)
        {
            CarSlotUI first = grid.FirstSlot();
            if (first == null)
            {
                Debug.LogWarning($"[PlayerCarCursor P{playerIndex}] grid.FirstSlot() sigue devolviendo null — el grid no tiene slots construidos todavía.", this);
                return;
            }
            MoveTo(first);
            return; // este frame solo reanclamos, no leemos movimiento todavía
        }

        if (IsLocked)
        {
            if (GetCancel()) Unlock();
            return;
        }

        Vector2Int dir = ReadDirection();
        if (dir != Vector2Int.zero)
            MoveTo(grid.GetNextSlot(current.GridRow, current.GridCol, dir.y, dir.x));

        if (GetConfirm()) Lock();
    }

    Vector2Int ReadDirection()
    {
        var kb = Keyboard.current;
        if (playerIndex == 1)
        {
            if (kb.wKey.wasPressedThisFrame) return new Vector2Int(0, -1);
            if (kb.sKey.wasPressedThisFrame) return new Vector2Int(0, 1);
            if (kb.aKey.wasPressedThisFrame) return new Vector2Int(-1, 0);
            if (kb.dKey.wasPressedThisFrame) return new Vector2Int(1, 0);
        }
        else
        {
            if (kb.upArrowKey.wasPressedThisFrame) return new Vector2Int(0, -1);
            if (kb.downArrowKey.wasPressedThisFrame) return new Vector2Int(0, 1);
            if (kb.leftArrowKey.wasPressedThisFrame) return new Vector2Int(-1, 0);
            if (kb.rightArrowKey.wasPressedThisFrame) return new Vector2Int(1, 0);
        }
        return Vector2Int.zero;
    }

    bool GetConfirm() => playerIndex == 1
        ? Keyboard.current.spaceKey.wasPressedThisFrame
        : Keyboard.current.enterKey.wasPressedThisFrame;

    bool GetCancel() => playerIndex == 1
        ? Keyboard.current.escapeKey.wasPressedThisFrame
        : Keyboard.current.backspaceKey.wasPressedThisFrame;

    void MoveTo(CarSlotUI slot)
    {
        if (slot == null) return;
        current?.SetHover(playerIndex, false);
        current = slot;
        current.SetHover(playerIndex, true);

        CarStatsSO carToShow = current.slotType == CarSlotType.Random ? null : current.carStats;
        if (preview != null) preview.ShowCar(carToShow);
        if (statsPanel != null) statsPanel.ShowStats(carToShow);
    }

    public void OnSlotClicked(CarSlotUI slot)
    {
        if (IsLocked) return;
        MoveTo(slot);
        Lock();
    }

    void Lock()
    {
        locked = current;
        lastRandomPick = locked.slotType == CarSlotType.Random
            ? grid.registry.cars[Random.Range(0, grid.registry.cars.Length)].stats
            : null;
        locked.SetLocked(playerIndex);

        CarStatsSO carToShow = SelectedCar;
        if (preview != null) preview.ShowCar(carToShow);
        if (statsPanel != null) statsPanel.ShowStats(carToShow);
    }

    void Unlock() { locked.SetLocked(0); locked = null; }
}