using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ArenaBoundsMarker : MonoBehaviour
{
    BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true; // no colisiona con nada, es solo referencia de tamaño
    }

    public Bounds GetBounds() => GetComponent<BoxCollider>().bounds;

    // Dibuja el área exacta en la vista Scene, así ajustás el tamaño viendo el arena real debajo
    void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0.3f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(col.center, col.size);
    }
}