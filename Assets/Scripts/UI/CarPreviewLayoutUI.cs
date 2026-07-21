using UnityEngine;

public class CarPreviewLayoutUI : MonoBehaviour
{
    public RectTransform previewPanelP1;
    public RectTransform previewPanelP2;

    [Header("Tamaño del panel de preview")]
    public float panelSize = 380f;

    [Header("Posición en Multiplayer (offset desde el centro)")]
    public float multiplayerOffsetX = 320f;
    public float multiplayerOffsetY = -175f;

    public void ConfigureLayout(bool multiplayer)
    {
        if (previewPanelP1 == null)
        {
            Debug.LogError("[CarPreviewLayoutUI] 'Preview Panel P1' no está asignado.", this);
            return;
        }

        if (multiplayer)
        {
            SetCenteredFixed(previewPanelP1, new Vector2(-multiplayerOffsetX, multiplayerOffsetY));

            if (previewPanelP2 != null)
            {
                previewPanelP2.gameObject.SetActive(true);
                SetCenteredFixed(previewPanelP2, new Vector2(multiplayerOffsetX, multiplayerOffsetY));
            }
        }
        else
        {
            // Centrado en X, pero mantiene el mismo offset vertical que multiplayer
            SetCenteredFixed(previewPanelP1, new Vector2(0f, multiplayerOffsetY));
            if (previewPanelP2 != null) previewPanelP2.gameObject.SetActive(false);
        }
    }

    void SetCenteredFixed(RectTransform rt, Vector2 offsetFromCenter)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(panelSize, panelSize);
        rt.anchoredPosition = offsetFromCenter;
        rt.localScale = Vector3.one;
    }
}