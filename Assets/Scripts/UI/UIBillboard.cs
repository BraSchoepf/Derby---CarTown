using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    public Camera targetCamera;

    void LateUpdate()
    {
        if (targetCamera == null) return;
        transform.rotation = targetCamera.transform.rotation;
    }
}
