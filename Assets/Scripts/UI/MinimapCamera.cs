using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Camera minimapCam;
    public float height = 50f;

    void Start()
    {
        ArenaBoundsMarker marker = FindObjectOfType<ArenaBoundsMarker>();
        if (marker == null)
        {
            Debug.LogError("[MinimapCamera] No hay ArenaBoundsMarker en la escena.", this);
            return;
        }

        Bounds arenaBounds = marker.GetBounds();
        Vector3 center = arenaBounds.center;

        minimapCam.transform.position = new Vector3(center.x, height, center.z);
        minimapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = Mathf.Max(arenaBounds.extents.x, arenaBounds.extents.z);
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0, 0, 0, 0);
    }
}