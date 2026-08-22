using UnityEngine;

public class BombDirectionIndicator : MonoBehaviour
{
    public RectTransform arrowIcon; // el ícono de flecha en el HUD, rota sobre su eje Z
    [HideInInspector] public Transform ownCarTransform;
    [HideInInspector] public Camera playerCamera;

    VehicleHealth currentBombCarrier;
    bool subscribed = false;

    void OnEnable() => TrySubscribe();

    void TrySubscribe()
    {
        if (subscribed || BombCarrierManager.Instance == null) return;
        BombCarrierManager.Instance.OnBombCarrierChanged += HandleCarrierChanged;
        subscribed = true;
    }

    void HandleCarrierChanged(VehicleHealth newCarrier) => currentBombCarrier = newCarrier;

    void Update()
    {
        if (!subscribed) TrySubscribe(); // por si el manager arrancó después que este componente

        if (currentBombCarrier == null || ownCarTransform == null || playerCamera == null)
        {
            if (arrowIcon != null) arrowIcon.gameObject.SetActive(false);
            return;
        }

        // Si este jugador ES quien tiene la bomba, no hace falta mostrarle flecha
        if (currentBombCarrier.transform == ownCarTransform)
        {
            arrowIcon.gameObject.SetActive(false);
            return;
        }

        arrowIcon.gameObject.SetActive(true);

        Vector3 toTarget = currentBombCarrier.transform.position - ownCarTransform.position;
        toTarget.y = 0f;

        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0f;

        float angle = Vector3.SignedAngle(cameraForward, toTarget.normalized, Vector3.up);
        arrowIcon.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    void OnDisable()
    {
        if (subscribed && BombCarrierManager.Instance != null)
            BombCarrierManager.Instance.OnBombCarrierChanged -= HandleCarrierChanged;
        subscribed = false;
    }
}