using UnityEngine;
using UnityEngine.UI;

public class GameModeTypeSelectionPanel : MonoBehaviour
{
    [Header("Sub-secciones (mismo panel, se togglean entre sí)")]
    public GameObject modeListSection;
    public GameObject teamConfigSection;

    [Header("Lista de modos")]
    public GameModeSlotUI slotPrefab;
    public Transform slotContainer;

    [Header("Team Config")]
    public TeamConfigUI teamConfigUI;

    System.Action<GameModeSO> onModeFullyConfirmed;
    GameModeSO pendingMode;

    void Awake()
    {
        teamConfigUI.OnConfirmed += ConfirmTeamConfig; // ← se suscribe una sola vez
        teamConfigUI.OnCancelled += CancelTeamConfig;
    }

    public void PopulateModes(GameModeSO[] modes, System.Action<GameModeSO> onConfirmed)
    {
        onModeFullyConfirmed = onConfirmed;

        ShowModeList();

        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        foreach (var mode in modes)
        {
            GameModeSlotUI slot = Instantiate(slotPrefab, slotContainer);
            slot.Setup(mode, OnModeSelected);
        }

        ShowModeList();

        LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer.GetComponent<RectTransform>());
    }

    void OnModeSelected(GameModeSO mode)
    {
        if (!mode.supportsTeams)
        {
            onModeFullyConfirmed?.Invoke(mode);
            return;
        }

        pendingMode = mode;
        teamConfigUI.currentMode = mode;
        ShowTeamConfig();
    }

    void ConfirmTeamConfig()
    {
        onModeFullyConfirmed?.Invoke(pendingMode);
    }

    void CancelTeamConfig()
    {
        pendingMode = null;
        ShowModeList(); // vuelve a la lista de modos, sin confirmar nada
    }
    void ShowModeList()
    {
        modeListSection.SetActive(true);
        teamConfigSection.SetActive(false);
    }

    void ShowTeamConfig()
    {
        modeListSection.SetActive(false);
        teamConfigSection.SetActive(true);
    }

    public void BackToModeList()
    {
        ShowModeList();
    }

    void OnDestroy()
    {
        if (teamConfigUI != null)
        {
            teamConfigUI.OnConfirmed -= ConfirmTeamConfig;
            teamConfigUI.OnCancelled -= CancelTeamConfig; // ← agregar
        }
    }
}