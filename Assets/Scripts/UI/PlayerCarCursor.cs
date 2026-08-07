using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarCursor : MonoBehaviour
{
    public int playerIndex = 1;
    public CarSelectionGridUI grid;
    public CarPreviewRenderer preview;
    public CarStatsPanelUI statsPanel;

    [Header("Customization Tabs")]
    public CustomizationTabsUI tabs;
    public WheelSelectionPanelUI wheelPanel;

    [Header("Color panel - repetición al mantener")]
    public float holdRepeatDelay = 0.4f;   // pausa inicial antes de empezar a repetir
    public float holdRepeatInterval = 0.12f;

    Vector2Int heldDirection;
    float holdTimer;
    bool isFirstRepeat;

    Vector2Int heldWheelDirection;
    float wheelHoldTimer;
    public ColorSelectionPanelUI colorPanel;

    CarSlotUI current, locked;
    CarStatsSO lastRandomPick;

    public WheelVisualSO SelectedWheelVisual => wheelPanel != null ? wheelPanel.CurrentWheel : null;
    public Color SelectedColor => colorPanel != null ? colorPanel.CurrentColor : Color.white;
    public bool IsLocked => locked != null;
    public CarStatsSO SelectedCar =>
        locked == null ? null :
        locked.slotType == CarSlotType.Random ? lastRandomPick : locked.carStats;

    void Start()
    {
        if (tabs != null && tabs.ownerPlayerIndex != playerIndex)
        {
            Debug.LogError($"[PlayerCarCursor P{playerIndex}] El campo 'Tabs' apunta a un CustomizationTabsUI de P{tabs.ownerPlayerIndex} — están cruzados. Revisá el Inspector.", this);
        }

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
            if (first == null) { /* ...guard existente... */ return; }
            MoveTo(first);
            return;
        }

        if (tabs.IsSubPanelOpen)
        {
            if (tabs.CurrentTab == CustomizationTabsUI.Tab.Car)
            {
                Vector2Int moveDir = ReadDirection();
                if (moveDir != Vector2Int.zero)
                    MoveTo(grid.GetNextSlot(current.GridRow, current.GridCol, moveDir.y, moveDir.x));

                if (GetConfirm())
                {
                    if (current.IsLockedByMission)
                        return;

                    Lock();
                    tabs.CloseCurrentPanel();
                }
                return;
            }

            if (GetConfirm())
            {
                tabs.CloseCurrentPanel();
                return;
            }

            if (tabs.CurrentTab == CustomizationTabsUI.Tab.Color)
                HandleColorNavigation();
            else if (tabs.CurrentTab == CustomizationTabsUI.Tab.Wheels)
                HandleWheelNavigation();

            return;
        }

        Vector2Int tabDir = ReadDirection();
        if (tabDir.x != 0) tabs.MoveTabHover(tabDir.x);

        if (GetConfirm())
        {
            tabs.ConfirmHoveredTab();
            return;
        }
    }

    bool IsSlotLockedForMe(CarSlotUI slot)
    {
        if (slot.slotType == CarSlotType.Random) return false;
        if (string.IsNullOrEmpty(slot.carStats.unlockRewardId)) return false;
        return !UnlockRegistry.IsUnlocked(slot.carStats.unlockRewardId, playerIndex - 1); // playerIndex es 1/2, slot es 0/1
    }

    void HandleCustomizationMode()
    {
        if (tabs.IsSubPanelOpen)
        {
            if (GetConfirm())
            {
                tabs.CloseCurrentPanel();
                return;
            }

            if (tabs.CurrentTab == CustomizationTabsUI.Tab.Color)
                HandleColorNavigation();
            else if (tabs.CurrentTab == CustomizationTabsUI.Tab.Wheels)
                HandleWheelNavigation();

            // Tab.Car no tiene navegación propia por ahora — solo mostrar info, cerrar con confirm
        }
        else
        {
            // Estamos en la pantalla de botones de tabs — navegamos entre ellos
            Vector2Int moveDir = ReadDirection();
            if (moveDir.x != 0)
                tabs.MoveTabHover(moveDir.x);

            if (GetConfirm())
                tabs.ConfirmHoveredTab();
        }
    }

    void HandleCarGridNavigation()
    {
        Vector2Int moveDir = ReadDirection();
        if (moveDir != Vector2Int.zero)
            MoveTo(grid.GetNextSlot(current.GridRow, current.GridCol, moveDir.y, moveDir.x));

        if (GetConfirm())
        {
            if (IsSlotLockedForMe(current)) return;
            Lock();
        }
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

    void ApplyWheelMove(Vector2Int dir)
    {
        WheelVisualSO wheel = wheelPanel.Move(dir.x, dir.y);
        if (wheelPanel.CurrentWheelIsLocked) return; // no aplicar visualmente una rueda bloqueada
        preview.SetWheel(wheel);
    }
    void HandleWheelNavigation()
    {
        if (wheelPanel == null) return;

        Vector2Int dir = ReadDirectionRaw();

        if (dir == Vector2Int.zero)
        {
            heldWheelDirection = Vector2Int.zero;
            wheelHoldTimer = 0f;
            return;
        }

        if (dir != heldWheelDirection)
        {
            heldWheelDirection = dir;
            wheelHoldTimer = holdRepeatDelay;
            ApplyWheelMove(dir);
            return;
        }

        wheelHoldTimer -= Time.deltaTime;
        if (wheelHoldTimer <= 0f)
        {
            ApplyWheelMove(dir);
            wheelHoldTimer = holdRepeatInterval;
        }
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
        if (current == null || current.IsLockedByMission)
            return;

        locked = current;
        lastRandomPick = locked.slotType == CarSlotType.Random
            ? grid.registry.cars[Random.Range(0, grid.registry.cars.Length)].stats
            : null;
        locked.SetLocked(playerIndex);

        CarStatsSO carToShow = SelectedCar;
        if (preview != null) preview.ShowCar(carToShow);
        if (statsPanel != null) statsPanel.ShowStats(carToShow);

        if (tabs != null) tabs.gameObject.SetActive(true); // asegura que el panel de tabs esté visible
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
            current = null;
        }
    }
}