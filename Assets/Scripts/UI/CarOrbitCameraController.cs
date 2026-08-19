using UnityEngine;
using UnityEngine.InputSystem;

public class CarOrbitCameraController : MonoBehaviour
{
    public Camera targetCamera;
    public Transform pivot;          // punto medio entre los dos autos, ubicado a mano en la escena
    public float distance = 6f;
    public float sensitivity = 0.2f;
    public float minPitch = -20f, maxPitch = 40f;

    float yaw, pitch;

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        if (delta != Vector2.zero)
            ApplyLookDelta(delta);
    }

    void ApplyLookDelta(Vector2 delta)
    {
        yaw += delta.x * sensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
        UpdateCameraTransform();
    }

    void UpdateCameraTransform()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        targetCamera.transform.position = pivot.position + rot * new Vector3(0, 0, -distance);
        targetCamera.transform.LookAt(pivot);
    }
}