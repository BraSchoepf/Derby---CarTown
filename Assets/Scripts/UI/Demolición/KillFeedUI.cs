using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class KillFeedUI : MonoBehaviour
{
    public GameObject feedEntryPrefab;
    public Transform feedContainer;
    public float entryLifetime = 4f;
    public int maxVisibleEntries = 5;

    [Header("Íconos fijos (atacante / víctima)")]
    public Sprite attackerIconSprite;
    public Sprite victimIconSprite;

    Queue<GameObject> activeEntries = new Queue<GameObject>();

    void Start()
    {
        DerbyGameManager derby = DerbyGameManager.Instance;
        if (derby == null)
        {
            Debug.LogError("[KillFeedUI] No se encontró DerbyGameManager.", this);
            return;
        }
        derby.OnPlayerEliminated += HandleEliminated;
    }

    void HandleEliminated(DerbyGameManager.PlayerEntry entry)
    {
        AddEntry(entry.killedByName, entry.playerName);
    }

    void AddEntry(string attackerName, string victimName)
    {
        if (feedEntryPrefab == null)
        {
            Debug.LogError("[KillFeedUI] feedEntryPrefab no está asignado en el Inspector.", this);
            return;
        }

        GameObject go = Instantiate(feedEntryPrefab, feedContainer);

        TextMeshProUGUI attackerText = go.transform.Find("AttackerText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI victimText = go.transform.Find("VictimText")?.GetComponent<TextMeshProUGUI>();
        Image attackerIcon = go.transform.Find("AttackerIcon")?.GetComponent<Image>();
        Image victimIcon = go.transform.Find("VictimIcon")?.GetComponent<Image>();

        if (attackerText == null || victimText == null || attackerIcon == null || victimIcon == null)
        {
            Debug.LogError($"[KillFeedUI] El prefab '{feedEntryPrefab.name}' no tiene la estructura esperada (AttackerText / AttackerIcon / VictimIcon / VictimText).", go);
            return;
        }

        attackerText.text = attackerName;
        victimText.text = victimName;
        attackerIcon.sprite = attackerIconSprite;
        victimIcon.sprite = victimIconSprite;

        activeEntries.Enqueue(go);
        StartCoroutine(RemoveAfterDelay(go, entryLifetime));

        while (activeEntries.Count > maxVisibleEntries)
        {
            GameObject oldest = activeEntries.Dequeue();
            if (oldest != null) Destroy(oldest);
        }
    }

    IEnumerator RemoveAfterDelay(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (entry != null) Destroy(entry);
    }

    void OnDestroy()
    {
        DerbyGameManager derby = DerbyGameManager.Instance;
        if (derby != null) derby.OnPlayerEliminated -= HandleEliminated;
    }
}