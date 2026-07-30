using UnityEngine;

public class MinimapIconClamper : MonoBehaviour
{
    public Transform trackedPlayer; // el mismo que usa RaceMinimapCamera — asignar el auto del jugador dueño de este minimapa
    public float clampRadius = 24f; // un poco menos que orthoSize de la cámara, para que no toque el borde justo

    Transform realCarTransform; // el auto real al que pertenece este ícono (el padre del MinimapIcon)

    void Awake()
    {
        realCarTransform = transform; // MinimapIcon está en el mismo GameObject que el auto
    }

    Vector3 GetClampedWorldPosition()
    {
        if (trackedPlayer == null) return realCarTransform.position;

        Vector3 offset = realCarTransform.position - trackedPlayer.position;
        Vector2 flatOffset = new Vector2(offset.x, offset.z);

        if (flatOffset.magnitude <= clampRadius)
            return realCarTransform.position; // está dentro del radio visible, posición real

        Vector2 clamped = flatOffset.normalized * clampRadius;
        return new Vector3(trackedPlayer.position.x + clamped.x, realCarTransform.position.y, trackedPlayer.position.z + clamped.y);
    }

    public Vector3 GetIconWorldPosition() => GetClampedWorldPosition();
}