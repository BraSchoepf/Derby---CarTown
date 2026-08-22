using UnityEngine;

public class RaceMinimapCamera : MonoBehaviour
{
    public Camera minimapCam;
    public Transform trackedPlayer; // el auto de ESTE jugador (P1 o P2)
    public float height = 50f;
    public float orthoSize = 25f; // "zoom" del minimapa — más chico = más cerca/detallado

    void LateUpdate()
    {
        if (trackedPlayer == null) return;

        minimapCam.transform.position = new Vector3(trackedPlayer.position.x, height, trackedPlayer.position.z);
        minimapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = orthoSize;
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0, 0, 0, 0);
    }
}