using UnityEngine;
using System.Linq;

public class BombCarrierManager : MonoBehaviour
{
    public static BombCarrierManager Instance;

    [Header("Config")]
    public float explosionTimer = 15f;
    public float minTransferForce = 1f; // bajado de 3f — casi cualquier contacto real transfiere
    public float timeAddedOnTransfer = 3f;


    [Header("Visual de la bomba")]
    public GameObject bombPrefab;
    [Tooltip("Offset local sobre el auto donde se posiciona la bomba (ej: encima del techo)")]
    public Vector3 bombLocalOffset = new Vector3(0f, 1.2f, 0f);

    public System.Action<VehicleHealth> OnBombCarrierChanged;
    public System.Action<float> OnTimerTick;

    float currentTimer;
    VehicleHealth currentCarrier;
    bool roundActive = false;
    GameObject bombInstance;

    public VehicleHealth CurrentCarrier => currentCarrier;

    void Awake() => Instance = this;

    public void StartBombRound()
    {
        var alivePlayers = DerbyGameManager.Instance.players.Where(p => p.isAlive).ToList();
        if (alivePlayers.Count == 0) return;

        if (bombInstance == null && bombPrefab != null)
            bombInstance = Instantiate(bombPrefab);

        var chosen = alivePlayers[Random.Range(0, alivePlayers.Count)];
        currentTimer = explosionTimer; // el timer inicial de la ronda se setea ACÁ, una sola vez
        AssignCarrier(chosen.health, addTime: false);
    }

    void Update()
    {
        if (!roundActive || currentCarrier == null) return;

        currentTimer -= Time.deltaTime;
        OnTimerTick?.Invoke(Mathf.Max(0f, currentTimer));

        if (currentTimer <= 0f)
            Explode();
    }

    public bool TryTransferBomb(VehicleHealth from, VehicleHealth to, float impactForce)
    {
        if (!roundActive || from != currentCarrier || to == currentCarrier) return false;
        if (impactForce < minTransferForce) return false;

        AssignCarrier(to, addTime: true);
        return true;
    }
    void AssignCarrier(VehicleHealth newCarrier, bool addTime)
    {
        if (currentCarrier != null)
        {
            var oldBombComponent = currentCarrier.GetComponent<BombCarrier>();
            if (oldBombComponent != null) oldBombComponent.SetCarrying(false);
        }

        currentCarrier = newCarrier;

        if (addTime)
            currentTimer += timeAddedOnTransfer; // suma sobre lo que quedaba, NO resetea

        roundActive = true;

        var newBombComponent = currentCarrier.GetComponent<BombCarrier>();
        if (newBombComponent != null) newBombComponent.SetCarrying(true);

        if (bombInstance != null)
        {
            bombInstance.transform.SetParent(currentCarrier.transform);
            bombInstance.transform.localPosition = bombLocalOffset;
            bombInstance.transform.localRotation = Quaternion.identity;
        }

        OnBombCarrierChanged?.Invoke(currentCarrier);
    }

    void Explode()
    {
        roundActive = false;

        VehicleHealth exploded = currentCarrier;
        currentCarrier = null;

        exploded.ForceEliminateNoKillCredit();

        var alivePlayers = DerbyGameManager.Instance.players.Where(p => p.isAlive).ToList();
        if (alivePlayers.Count == 0)
        {
            // No queda nadie más — la partida ya terminó, destruimos la bomba
            if (bombInstance != null) Destroy(bombInstance);
            return;
        }

        var closest = alivePlayers
            .OrderBy(p => Vector3.Distance(p.health.transform.position, exploded.transform.position))
            .First();
        currentTimer = explosionTimer; // nueva ronda tras explosión: timer completo de nuevo
        AssignCarrier(closest.health, addTime: false);
    }
}