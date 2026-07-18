using UnityEngine;

public class MinimapIcon : MonoBehaviour
{
    public GameObject iconPrefab;
    public float fixedHeight = 15f; // altura constante sobre el arena, no relativa al auto
    public bool rotateWithCar = true;
    public float yawOffset = 0f;

    Transform iconInstance;

    void Start()
    {
        GameObject go = Instantiate(iconPrefab, transform.position, Quaternion.Euler(90f, 0f, 0f));
        iconInstance = go.transform;
    }

    void LateUpdate()
    {
        if (iconInstance == null) return;

        // Posición: solo X/Z del auto, altura siempre fija — nunca hereda vuelco
        iconInstance.position = new Vector3(transform.position.x, fixedHeight, transform.position.z);

        if (!rotateWithCar)
        {
            iconInstance.rotation = Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        // Heading real: aplanamos el forward del auto sobre el plano XZ,
        // así un vuelco (rotación en X/Z) no afecta el yaw que mostramos
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude > 0.0001f) // evita error si el forward queda apuntando recto hacia arriba/abajo
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