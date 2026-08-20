using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpawnInvincibility : MonoBehaviour
{
    [Header("Duración")]
    [Tooltip("Segundos de inmunidad tras respawnear: no choca ni es chocado por otros autos, ni recibe/transfiere bomba, ni recibe daño")]
    public float duration = 2f;

    [Header("Layers")]
    [Tooltip("Layer normal del auto (debe coincidir con la Layer real asignada al auto y sus hijos)")]
    public string normalCarLayerName = "Car";

    [Tooltip("Layer temporal configurada en Project Settings > Physics para NO colisionar con 'Car' (ni consigo misma)")]
    public string invincibleCarLayerName = "CarSpawnProtected";

    bool isInvincible = false;
    float timer = 0f;

    int normalLayerIndex;
    int invincibleLayerIndex;

    public bool IsInvincible => isInvincible;

    void Awake()
    {
        normalLayerIndex = LayerMask.NameToLayer(normalCarLayerName);
        invincibleLayerIndex = LayerMask.NameToLayer(invincibleCarLayerName);
    }

    void Update()
    {
        if (!isInvincible) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
            EndInvincibility();
    }

    public void Activate()
    {
        if (duration <= 0f) return;

        if (invincibleLayerIndex == -1)
        {
            Debug.LogWarning($"[{name}] La layer '{invincibleCarLayerName}' no existe. Creala en Edit > Project Settings > Tags and Layers.", this);
            return;
        }

        isInvincible = true;
        timer = duration;

        SetLayerRecursively(transform, normalLayerIndex, invincibleLayerIndex);
    }

    void EndInvincibility()
    {
        isInvincible = false;
        SetLayerRecursively(transform, invincibleLayerIndex, normalLayerIndex);
    }

    void SetLayerRecursively(Transform root, int fromLayer, int toLayer)
    {
        if (root.gameObject.layer == fromLayer)
            root.gameObject.layer = toLayer;

        foreach (Transform child in root)
            SetLayerRecursively(child, fromLayer, toLayer);
    }
}