using UnityEngine;

public class SpeedometerUI : MonoBehaviour
{
    [Header("Referencias")]
    public RectTransform needle; // ahora se desplaza en X, ya no rota

    [Header("Rango del velocímetro")]
    [Tooltip("Velocidad real del auto que corresponde al extremo IZQUIERDO de la escala (normalmente 0)")]
    public float speedAtMinAngle = 0f;
    [Tooltip("Velocidad real del auto que corresponde al extremo DERECHO de la escala")]
    public float speedAtMaxAngle = 80f;

    [Header("Posición horizontal (definida a mano viendo el fondo)")]
    [Tooltip("Posición X local del indicador en velocidad 0 (extremo izquierdo)")]
    public float minPosX = -150f;
    [Tooltip("Posición X local del indicador en velocidad máxima (extremo derecho)")]
    public float maxPosX = 150f;

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

        float posX = Mathf.Lerp(minPosX, maxPosX, t);

        Vector2 pos = needle.anchoredPosition;
        pos.x = posX;
        needle.anchoredPosition = pos;
    }
}