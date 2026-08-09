using UnityEngine;

public class ShaderSelectionPanelUI : MonoBehaviour
{
    public CarShaderVariantSO[] availableShaders;
    public ShaderSwatchUI swatchPrefab;
    public Transform swatchContainer;
    public int columns = 4;

    [Tooltip("1 = P1, 2 = P2 — determina qué desbloqueos consultar")]
    public int ownerPlayerSlotIndex = 1;

    public event System.Action<CarShaderVariantSO> OnShaderSelected;

    ShaderSwatchUI[] spawnedSwatches;
    int currentIndex = 0;

    void Awake() => BuildSwatches();

    void BuildSwatches()
    {
        if (availableShaders == null || availableShaders.Length == 0)
        {
            Debug.LogError("[ShadersSelectionPanelUI] 'Available Shaders' está vacío.", this);
            return;
        }

        spawnedSwatches = new ShaderSwatchUI[availableShaders.Length];
        for (int i = 0; i < availableShaders.Length; i++)
        {
            ShaderSwatchUI swatch = Instantiate(swatchPrefab, swatchContainer);
            swatch.SetVariant(availableShaders[i], ownerPlayerSlotIndex);

            int index = i;
            swatch.OnClicked += (s) => SelectIndex(index);

            spawnedSwatches[i] = swatch;
        }

        // Arranca en el primer material DESBLOQUEADO, no necesariamente el índice 0
        int firstUnlocked = System.Array.FindIndex(spawnedSwatches, s => !s.IsLocked);
        SelectIndex(firstUnlocked >= 0 ? firstUnlocked : 0, notify: false);
    }

    public CarShaderVariantSO Move(int deltaCol, int deltaRow)
    {
        int row = currentIndex / columns;
        int col = currentIndex % columns;
        int totalRows = Mathf.CeilToInt((float)availableShaders.Length / columns);

        int newCol = Mathf.Clamp(col + deltaCol, 0, columns - 1);
        int newRow = Mathf.Clamp(row + deltaRow, 0, totalRows - 1);
        int newIndex = Mathf.Clamp(newRow * columns + newCol, 0, availableShaders.Length - 1);

        // Saltea slots bloqueados en la dirección del movimiento, en vez de dejarte "elegir" algo bloqueado
        if (spawnedSwatches[newIndex].IsLocked)
            return CurrentShader; // se queda donde estaba, no avanza a un slot bloqueado

        SelectIndex(newIndex);
        return CurrentShader;
    }

    void SelectIndex(int index, bool notify = true)
    {
        currentIndex = index;
        RefreshHighlight();
        if (notify) OnShaderSelected?.Invoke(CurrentShader);
    }

    void RefreshHighlight()
    {
        for (int i = 0; i < spawnedSwatches.Length; i++)
            spawnedSwatches[i].SetSelected(i == currentIndex);
    }

    public CarShaderVariantSO CurrentShader => availableShaders[currentIndex];
}
