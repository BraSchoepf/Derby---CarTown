using UnityEngine;

public class CarVFXController : MonoBehaviour
{
    [Header("Referencias")]
    public CarController carController;

    [Header("Puntos de instanciación - Nitro (Transforms vacíos ya colocados)")]
    public Transform nitroVFXPointL;
    public Transform nitroVFXPointR;
    // A futuro: más puntos acá (ej. exhaustSmokePoint, damageFirePoint, etc.)

    CarVFXDataSO vfxData;
    GameObject nitroVFXInstanceL;
    GameObject nitroVFXInstanceR;
    bool nitroVFXActive = false;

    void Awake()
    {
        if (carController == null) carController = GetComponent<CarController>();
    }

    void Start()
    {
        vfxData = carController.stats != null ? carController.stats.vfxData : null;
    }

    void FixedUpdate()
    {
        UpdateWheelTrails();
    }

    void Update()
    {
        UpdateNitroVFX();
    }

    void UpdateNitroVFX()
    {
        if (vfxData == null || vfxData.nitroVFXPrefab == null) return;
        if (nitroVFXPointL == null && nitroVFXPointR == null) return;

        bool shouldShow = carController.IsNitroActive;

        if (shouldShow && !nitroVFXActive)
        {
            nitroVFXInstanceL = SpawnNitroVFX(nitroVFXPointL);
            nitroVFXInstanceR = SpawnNitroVFX(nitroVFXPointR);
            nitroVFXActive = true;
        }
        else if (!shouldShow && nitroVFXActive)
        {
            if (nitroVFXInstanceL != null) Destroy(nitroVFXInstanceL);
            if (nitroVFXInstanceR != null) Destroy(nitroVFXInstanceR);
            nitroVFXActive = false;
        }
    }

    GameObject SpawnNitroVFX(Transform point)
    {
        if (point == null) return null;

        GameObject instance = Instantiate(vfxData.nitroVFXPrefab, point.position, point.rotation, point);
        instance.transform.localScale = Vector3.one * vfxData.nitroVFXScale;
        return instance;
    }

    void UpdateWheelTrails()
    {
        if (vfxData == null) return;

        foreach (var w in carController.wheels)
        {
            if (w.trail == null || w.collider == null) continue;

            bool shouldEmit = false;
            if (w.collider.GetGroundHit(out WheelHit hit))
            {
                float lateralSlip = Mathf.Abs(hit.sidewaysSlip);
                float forwardSlip = Mathf.Abs(hit.forwardSlip);
                shouldEmit = lateralSlip > vfxData.lateralSlipThreshold || forwardSlip > vfxData.brakeSlipThreshold;
            }
            w.trail.emitting = shouldEmit;
        }
    }
}