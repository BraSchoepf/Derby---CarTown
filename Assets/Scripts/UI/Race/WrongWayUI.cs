using UnityEngine;

public class WrongWayUI : MonoBehaviour
{
    public GameObject wrongWayWarning;
    public RectTransform panelRoot; // el contenedor completo de este panel (para reposicionarlo)

    WrongWayDetector detector;

    public void SetTarget(WrongWayDetector wrongWayDetector)
    {
        detector = wrongWayDetector;
    }

    // Llamado por RaceSetup, mismo patrón que la cámara
    public void ConfigureLayout(bool multiplayer, bool isLeftHalf)
    {
        if (panelRoot == null) return;

        if (!multiplayer)
        {
            // Single player: centrado, ocupa el ancho completo (o el tamaño que prefieras)
            panelRoot.anchorMin = Vector2.zero;
            panelRoot.anchorMax = Vector2.one;
            panelRoot.offsetMin = Vector2.zero;
            panelRoot.offsetMax = Vector2.zero;
        }
        else
        {
            if (isLeftHalf)
            {
                panelRoot.anchorMin = new Vector2(0f, 0f);
                panelRoot.anchorMax = new Vector2(0.5f, 1f);
            }
            else
            {
                panelRoot.anchorMin = new Vector2(0.5f, 0f);
                panelRoot.anchorMax = new Vector2(1f, 1f);
            }
            panelRoot.offsetMin = Vector2.zero;
            panelRoot.offsetMax = Vector2.zero;
        }
    }

    void Update()
    {
        if (detector == null || wrongWayWarning == null) return;
        wrongWayWarning.SetActive(detector.IsGoingWrongWay);
    }
}