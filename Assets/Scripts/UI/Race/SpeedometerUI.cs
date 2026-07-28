using UnityEngine;

public class SpeedometerUI : MonoBehaviour
{
    [Header("Referencias")]
    public RectTransform needle; // pivote en la base de la aguja, ya configurado en el editor

    [Header("Rango del velocímetro")]
    [Tooltip("Velocidad real del auto que corresponde al extremo IZQUIERDO de la escala (normalmente 0)")]
    public float speedAtMinAngle = 0f;
    [Tooltip("Velocidad real del auto que corresponde al extremo DERECHO de la escala")]
    public float speedAtMaxAngle = 80f;

    [Header("Ángulos de la aguja (definidos a mano viendo el fondo)")]
    [Tooltip("Rotación Z de la aguja en velocidad 0")]
    public float minAngle = 90f;
    [Tooltip("Rotación Z de la aguja en velocidad máxima")]
    public float maxAngle = -90f;

    [Header("Suavizado")]
    public float needleSmoothSpeed = 8f;

    CarController targetCar;
    float currentDisplaySpeed;

    public void SetTarget(CarController car)
    {
        targetCar = car;
    }

    void Update()
    {
        if (targetCar == null || needle == null) return;

        float realSpeed = targetCar.CurrentSpeed;
        currentDisplaySpeed = Mathf.Lerp(currentDisplaySpeed, realSpeed, Time.deltaTime * needleSmoothSpeed);

        float t = Mathf.InverseLerp(speedAtMinAngle, speedAtMaxAngle, currentDisplaySpeed);
        t = Mathf.Clamp01(t);

        float angle = Mathf.Lerp(minAngle, maxAngle, t);
        needle.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}