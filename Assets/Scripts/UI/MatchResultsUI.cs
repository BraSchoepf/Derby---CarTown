using UnityEngine;
using System.Collections;

public class MatchResultsUI : MonoBehaviour
{
    public ResultPanelUI panelP1;
    public ResultPanelUI panelP2;
    public GameSetup gameSetup;

    [Header("Colapso a fullscreen tras eliminación")]
    [Tooltip("Segundos que se muestra el split-screen con el panel de derrota antes de expandir al jugador restante")]
    public float delayBeforeFullscreen = 3f;

    DerbyGameManager derby;
    bool alreadyCollapsed = false;

    void Start()
    {
        derby = FindObjectOfType<DerbyGameManager>();
        if (derby == null)
        {
            Debug.LogError("[MatchResultsUI] No se encontró DerbyGameManager en la escena.", this);
            return;
        }

        derby.OnPlayerEliminated += HandleEliminated;
        derby.OnPlayerWon += HandleWon;

        bool multiplayer = GameSession.Instance != null
                            && GameSession.Instance.selectedMode == GameMode.MultiplayerSplitScreen;
        ConfigureLayout(multiplayer);
    }

    void ConfigureLayout(bool multiplayer)
    {
        if (multiplayer)
        {
            SetRect(panelP1.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            SetRect(panelP2.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(1f, 1f));
        }
        else
        {
            SetRect(panelP1.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            if (panelP2 != null) panelP2.gameObject.SetActive(false);
        }
    }

    void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void HandleEliminated(DerbyGameManager.PlayerEntry entry)
    {
        ResultPanelUI panel = GetPanelFor(entry);
        panel?.ShowDefeat(entry);

        // Solo colapsamos en multiplayer, cuando el eliminado es humano,
        // y queda exactamente un humano vivo (el otro sigue jugando)
        bool multiplayer = GameSession.Instance != null
                            && GameSession.Instance.selectedMode == GameMode.MultiplayerSplitScreen;

        if (!multiplayer || alreadyCollapsed || entry.humanSlotIndex == -1) return;

        int survivingSlotIndex = entry.humanSlotIndex == 0 ? 1 : 0;
        var survivingEntry = derby.players.Find(p => p.humanSlotIndex == survivingSlotIndex);

        if (survivingEntry != null && survivingEntry.isAlive)
        {
            alreadyCollapsed = true;
            StartCoroutine(CollapseAfterDelay(survivingSlotIndex, panel));
        }
    }

    IEnumerator CollapseAfterDelay(int survivingSlotIndex, ResultPanelUI eliminatedPanel)
    {
        yield return new WaitForSeconds(delayBeforeFullscreen);

        gameSetup.ExpandToFullscreen(survivingSlotIndex);

        if (eliminatedPanel != null)
            eliminatedPanel.gameObject.SetActive(false);

        // El panel del sobreviviente (que todavía no se activó, sigue jugando)
        // necesita quedar pre-configurado a fullscreen para cuando finalmente muera/gane
        ResultPanelUI survivingPanel = survivingSlotIndex == 0 ? panelP1 : panelP2;
        SetRect(survivingPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
    }

    void HandleWon(DerbyGameManager.PlayerEntry winner)
    {
        ResultPanelUI panel = GetPanelFor(winner);
        panel?.ShowVictory(winner);
    }

    ResultPanelUI GetPanelFor(DerbyGameManager.PlayerEntry entry)
    {
        if (entry.humanSlotIndex == 0) return panelP1;
        if (entry.humanSlotIndex == 1) return panelP2;
        return null;
    }
}