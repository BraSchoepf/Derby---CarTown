using UnityEngine;
using UnityEngine.UI;

public class MapCarouselUI : MonoBehaviour
{
    public MapDataSO[] allMaps;
    MapDataSO[] maps;
    public Image slotPrefab;
    public Transform carouselContainer;

    [Header("Layout")]
    public float spacingX = 220f;
    public float sideScale = 0.7f;
    public float sideAlpha = 0.4f;
    [Tooltip("Cuántos vecinos a cada lado se ven antes de desaparecer del todo")]
    public int maxVisibleNeighbors = 2;

    Image[] spawnedSlots;
    int currentIndex = 0;

    public MapDataSO CurrentMap => maps[currentIndex];

    void BuildSlots()
    {
        spawnedSlots = new Image[maps.Length];
        for (int i = 0; i < maps.Length; i++)
        {
            Image slot = Instantiate(slotPrefab, carouselContainer);
            slot.sprite = maps[i].previewImage;
            spawnedSlots[i] = slot;
        }
        RefreshVisuals();
    }

    public void Move(int direction)
    {
        currentIndex = (currentIndex + direction + maps.Length) % maps.Length;
        RefreshVisuals();
    }

    void RefreshVisuals()
    {
        // Primero calculamos todos los offsets, después ordenamos de "más lejos" a "más cerca"
        // y recién ahí asignamos sibling index en ESE orden — así el centro siempre
        // se asigna último y queda garantizado arriba de todo, sin pelear con el loop.
        var order = new System.Collections.Generic.List<int>();
        for (int i = 0; i < spawnedSlots.Length; i++)
        {
            if (spawnedSlots[i] != null) order.Add(i);
        }
        order.Sort((a, b) =>
        {
            int offsetA = Mathf.Abs(GetShortestOffset(a, currentIndex, maps.Length));
            int offsetB = Mathf.Abs(GetShortestOffset(b, currentIndex, maps.Length));
            return offsetB.CompareTo(offsetA); // descendente: el más lejos primero, el centro (0) al final
        });

        foreach (int i in order)
        {
            int offset = GetShortestOffset(i, currentIndex, maps.Length);
            int absOffset = Mathf.Abs(offset);

            RectTransform rt = spawnedSlots[i].rectTransform;
            rt.anchoredPosition = new Vector2(offset * spacingX, 0f);

            float scaleT = Mathf.Clamp01((float)absOffset / maxVisibleNeighbors);
            float scale = Mathf.Lerp(1f, sideScale, scaleT);
            rt.localScale = Vector3.one * scale;

            var canvasGroup = spawnedSlots[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = spawnedSlots[i].gameObject.AddComponent<CanvasGroup>();

            float alpha = absOffset > maxVisibleNeighbors ? 0f : Mathf.Lerp(1f, sideAlpha, scaleT);
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = absOffset == 0;

            // Ahora sí: como procesamos en orden "lejos → cerca", cada SetSiblingIndex
            // al final del proceso mueve al frente, y el último en ejecutarse (el centro) queda arriba de todo
            spawnedSlots[i].transform.SetAsLastSibling();
        }
    }
    public void SetAvailableMaps(GameModeSO mode)
    {
        if (spawnedSlots != null)
            foreach (var slot in spawnedSlots)
                if (slot != null) Destroy(slot.gameObject);

        maps = System.Array.FindAll(allMaps, m => System.Array.IndexOf(m.compatibleModes, mode) >= 0);
        currentIndex = 0;

        if (maps.Length == 0)
        {
            Debug.LogError($"[MapCarouselUI] Ningún mapa es compatible con el modo '{mode.modeName}'.", this);
            return; // corta ACÁ, no llames BuildSlots() con array vacío
        }

        BuildSlots();
    }

    int GetShortestOffset(int index, int center, int count)
    {
        int raw = index - center;
        if (raw > count / 2) raw -= count;
        if (raw < -count / 2) raw += count;
        return raw;
    }
}