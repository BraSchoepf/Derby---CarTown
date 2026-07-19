using UnityEngine;
public enum MinimapOwnerType { Player1, Player2, Bot }
public class MinimapIcon : MonoBehaviour
{
    [Header("Íconos exclusivos por tipo")]
    public GameObject p1IconPrefab;
    public GameObject p2IconPrefab;
    public GameObject botIconPrefab;
    public GameObject deadIconPrefab; // uno solo, para cualquier tipo

    public MinimapOwnerType ownerType = MinimapOwnerType.Player1;

    public float fixedHeight = 15f;
    public bool rotateWithCar = true;
    public float yawOffset = 0f;

    Transform iconInstance;
    VehicleHealth health;
    bool showingDeadIcon = false;

    void Awake()
    {
        health = GetComponent<VehicleHealth>();
        if (health != null)
            health.OnVehicleDestroyed += SwitchToDeadIcon;
    }

    void Start()
    {
        if (iconInstance == null) SpawnIcon();
    }

    public void SetOwner(MinimapOwnerType type)
    {
        ownerType = type;
        if (iconInstance == null) SpawnIcon();
    }

    void SwitchToDeadIcon()
    {
        if (showingDeadIcon || deadIconPrefab == null) return;

        if (iconInstance != null) Destroy(iconInstance.gameObject);

        GameObject go = Instantiate(deadIconPrefab, transform.position, Quaternion.Euler(90f, 0f, 0f));
        iconInstance = go.transform;
        showingDeadIcon = true;
        rotateWithCar = false; // un auto muerto no tiene heading relevante, queda fijo
    }

    void SpawnIcon()
    {
        if (iconInstance != null) return;

        GameObject prefabToUse = ownerType switch
        {
            MinimapOwnerType.Player1 => p1IconPrefab,
            MinimapOwnerType.Player2 => p2IconPrefab,
            MinimapOwnerType.Bot => botIconPrefab,
            _ => null
        };

        if (prefabToUse == null)
        {
            Debug.LogError($"[MinimapIcon] Falta asignar el prefab de ícono para {ownerType}.", this);
            return;
        }

        GameObject go = Instantiate(prefabToUse, transform.position, Quaternion.Euler(90f, 0f, 0f));
        iconInstance = go.transform;
    }

    void LateUpdate()
    {
        if (iconInstance == null) return;
        iconInstance.position = new Vector3(transform.position.x, fixedHeight, transform.position.z);

        if (!rotateWithCar)
        {
            iconInstance.rotation = Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude > 0.0001f)
        {
            float yaw = Quaternion.LookRotation(flatForward).eulerAngles.y;
            iconInstance.rotation = Quaternion.Euler(90f, yaw + yawOffset, 0f);
        }
    }

    void OnDestroy()
    {
        if (iconInstance != null) Destroy(iconInstance.gameObject);
    }
}