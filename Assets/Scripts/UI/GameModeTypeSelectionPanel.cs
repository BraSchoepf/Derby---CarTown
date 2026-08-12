using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    readonly List<GameModeSlotUI> pooledSlots = new List<GameModeSlotUI>();

    void Awake()
    {
        teamConfigUI.OnConfirmed += ConfirmTeamConfig;
        teamConfigUI.OnCancelled += CancelTeamConfig;
    }

    public void PopulateModes(GameModeSO[] modes, System.Action<GameModeSO> onConfirmed)
    {
        onModeFullyConfirmed = onConfirmed;
        ShowModeList();

        bool wasActive = slotContainer.gameObject.activeSelf;
        slotContainer.gameObject.SetActive(false);

        for (int i = 0; i < modes.Length; i++)
        {
            GameModeSlotUI slot = GetOrCreateSlot(i);
            slot.Setup(modes[i], OnModeSelected);
            slot.gameObject.SetActive(true);
        }

        for (int i = modes.Length; i < pooledSlots.Count; i++)
            pooledSlots[i].gameObject.SetActive(false);

        slotContainer.gameObject.SetActive(wasActive);
    }

    GameModeSlotUI GetOrCreateSlot(int index)
    {
        if (index < pooledSlots.Count)
            return pooledSlots[index];

        GameModeSlotUI slot = Instantiate(slotPrefab, slotContainer);
        pooledSlots.Add(slot);
        return slot;
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
        ShowModeList();
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
            teamConfigUI.OnCancelled -= CancelTeamConfig;
        }
    }
}