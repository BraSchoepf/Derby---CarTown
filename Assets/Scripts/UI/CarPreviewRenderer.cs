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

        instance = Instantiate(car.previewPrefab, spawnPoint.position, spawnPoint.rotation);
        SetLayerRecursively(instance, previewLayer);
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
}