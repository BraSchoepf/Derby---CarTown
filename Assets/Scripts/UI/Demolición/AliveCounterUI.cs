using UnityEngine;
using TMPro;
using System.Collections;

public class AliveCounterUI : MonoBehaviour
{
    public TextMeshProUGUI aliveCountText;
    [Tooltip("Formato del texto — {0} se reemplaza por la cantidad de vivos")]
    public string format = "{0} restantes";

    DerbyGameManager derby;

    void Start()
    {
        derby = DerbyGameManager.Instance;
        if (derby == null)
        {
            Debug.LogError("[AliveCounterUI] No se encontró DerbyGameManager.", this);
            return;
        }
        derby.OnPlayerEliminated += HandleEliminated;

        StartCoroutine(InitAfterRegistration());
    }

    IEnumerator InitAfterRegistration()
    {
        // Esperamos a que termine Start() de todos los demás scripts
        // (incluido GameSetup, que registra a los jugadores) antes de leer la lista
        yield return null; // 1 frame de espera alcanza en la mayoría de los casos
        UpdateCount();
    }

    void HandleEliminated(DerbyGameManager.PlayerEntry entry) => UpdateCount();

    void UpdateCount()
    {
        int aliveCount = derby.players.FindAll(p => p.isAlive).Count;
        aliveCountText.text = string.Format(format, aliveCount);
    }

    void OnDestroy()
    {
        if (derby != null) derby.OnPlayerEliminated -= HandleEliminated;
    }
}