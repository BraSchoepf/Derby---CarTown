using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarCursor : MonoBehaviour
{
    public int playerIndex = 1;
    public CarSelectionGridUI grid;
    public CarPreviewRenderer preview;
    public CarStatsPanelUI statsPanel;

    [Header("Color panel - repetición al mantener")]
    public float holdRepeatDelay = 0.4f;   // pausa inicial antes de empezar a repetir
    public float holdRepeatInterval = 0.12f;

    Vector2Int heldDirection;
    float holdTimer;
    bool isFirstRepeat;


    public ColorSelectionPanelUI colorPanel;

    bool loggedGridWarning = false;

    CarSlotUI current, locked;
    CarStatsSO lastRandomPick;

    public Color SelectedColor => colorPanel != null ? colorPanel.CurrentColor : Color.white;
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
        if (Keyboard.current == null) return;

        if (current == null)
        {
            CarSlotUI first = grid.FirstSlot();
            if (first == null)
            {
                if (!loggedGridWarning)
                {
                    Debug.LogWarning($"[PlayerCarCursor P{playerIndex}] grid.FirstSlot() sigue devolviendo null.", this);
                    loggedGridWarning = true;
                }
                return;
            }
            MoveTo(first);
            return;
        }

        if (IsLocked)
        {
            if (GetConfirm()) { ForceUnlock(); return; } // misma tecla, ahora deselecciona
            HandleColorNavigation();
            return;
        }

        Vector2Int moveDir = ReadDirection();
        if (moveDir != Vector2Int.zero)
            MoveTo(grid.GetNextSlot(current.GridRow, current.GridCol, moveDir.y, moveDir.x));

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
    : (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);


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

    void HandleColorNavigation()
    {
        if (colorPanel == null) return;

        Vector2Int dir = ReadDirectionRaw(); // devuelve la dirección sostenida, no solo el frame en que se apretó

        if (dir == Vector2Int.zero)
        {
            heldDirection = Vector2Int.zero;
            holdTimer = 0f;
            return;
        }

        if (dir != heldDirection)
        {
            // Cambió de dirección o recién empieza a mantener: un paso inmediato, resetea el timer
            heldDirection = dir;
            holdTimer = holdRepeatDelay;
            isFirstRepeat = true;
            ApplyColorMove(dir);
            return;
        }

        // Misma dirección sostenida: cuenta regresiva para repetir
        holdTimer -= Time.deltaTime;
        if (holdTimer <= 0f)
        {
            ApplyColorMove(dir);
            holdTimer = isFirstRepeat ? holdRepeatInterval : holdRepeatInterval;
            isFirstRepeat = false;
        }
    }

    void ApplyColorMove(Vector2Int dir)
    {
        Color color = colorPanel.Move(dir.x, dir.y); // dir.y porque tu ReadDirection usa y=-1 para "arriba"
        preview.SetColor(color);
    }

    Vector2Int ReadDirectionRaw()
    {
        var kb = Keyboard.current;
        if (playerIndex == 1)
        {
            if (kb.aKey.isPressed) return new Vector2Int(-1, 0);
            if (kb.dKey.isPressed) return new Vector2Int(1, 0);
            if (kb.wKey.isPressed) return new Vector2Int(0, -1);
            if (kb.sKey.isPressed) return new Vector2Int(0, 1);
        }
        else
        {
            if (kb.leftArrowKey.isPressed) return new Vector2Int(-1, 0);
            if (kb.rightArrowKey.isPressed) return new Vector2Int(1, 0);
            if (kb.upArrowKey.isPressed) return new Vector2Int(0, -1);
            if (kb.downArrowKey.isPressed) return new Vector2Int(0, 1);
        }
        return Vector2Int.zero;
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

        if (colorPanel != null)
        {
            colorPanel.gameObject.SetActive(true);
            preview.SetColor(colorPanel.CurrentColor); // aplica el color default apenas se ve el auto
        }
    }

    void Unlock()
    {
        ForceUnlock();
    }

    public void ForceUnlock()
    {
        if (locked != null)
        {
            locked.SetLocked(0);
            locked = null;
        }

        if (current != null)
        {
            current.SetHover(playerIndex, false);
            current = null; // fuerza que el próximo Update() vuelva a MoveTo(grid.FirstSlot())
        }

        if (colorPanel != null) colorPanel.gameObject.SetActive(false);
    }
}