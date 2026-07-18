using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Camera minimapCam;
    [Tooltip("Bounds del arena completo, para encuadrar todo sin importar el tamaño del nivel")]
    public Bounds arenaBounds;
    public float height = 50f;

    void Start()
    {
        Vector3 center = arenaBounds.center;
        minimapCam.transform.position = new Vector3(center.x, height, center.z);
        minimapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = Mathf.Max(arenaBounds.extents.x, arenaBounds.extents.z);

        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0, 0, 0, 0); // transparente, si tu fondo del minimapa es una imagen aparte
    }
}