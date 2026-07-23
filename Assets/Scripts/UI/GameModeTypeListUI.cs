using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameModeTypeListUI : MonoBehaviour
{
    public GameObject buttonPrefab; // Button + Image (icon) + TextMeshProUGUI (nombre)
    public Transform container;

    public void PopulateModes(GameModeSO[] modes, Action<GameModeSO> onSelected)
    {
        foreach (Transform child in container) Destroy(child.gameObject);

        foreach (var mode in modes)
        {
            GameObject go = Instantiate(buttonPrefab, container);
            go.GetComponentInChildren<TextMeshProUGUI>().text = mode.modeName;

            Image icon = go.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = mode.icon;

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onSelected(mode));
        }
    }
}