using UnityEngine;

public class CarPreviewRenderer : MonoBehaviour
{
    public Camera previewCamera;
    public Transform spawnPoint;
    public float rotationSpeed = 30f;
    [SerializeField] string previewLayerName = "CarPreview";

    GameObject instance;
    int previewLayer;

    void Awake() => previewLayer = LayerMask.NameToLayer(previewLayerName);

    public void ShowCar(CarStatsSO car)
    {
        if (instance != null) Destroy(instance);
        if (car == null || car.previewPrefab == null) return;

        // Solo tomamos la posición del spawnPoint; la rotación viene del prefab tal cual está,
        // así el auto no hereda el tilt que le pongas a la cámara.
        instance = Instantiate(car.previewPrefab, spawnPoint.position, car.previewPrefab.transform.rotation);
        SetLayerRecursively(instance, previewLayer);
        FreezePhysicsForPreview(instance);
    }

    void FreezePhysicsForPreview(GameObject obj)
    {
        // El auto real usa Rigidbody para andar en el juego;
        // acá solo lo mostramos girando, no queremos que la física lo mueva.
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;   // deja de reaccionar a gravedad/colisiones
            rb.useGravity = false;
        }
    }

    void Update()
    {
        if (instance != null)
            instance.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
    public void SetColor(Color color)
    {
        if (instance == null) return;
        var applier = instance.GetComponentInChildren<CarColorApplier>();
        if (applier != null) applier.SetColor(color);
    }
}