using UnityEngine;
using System;

public class GameModeTypeListUI : MonoBehaviour
{
    public GameObject slotPrefab; // ahora tiene GameModeSlotUI, no un Button simple
    public Transform container;

    public void PopulateModes(GameModeSO[] modes, Action<GameModeSO, GameModeSlotUI> onSelected)
    {
        foreach (Transform child in container) Destroy(child.gameObject);

        foreach (var mode in modes)
        {
            GameObject go = Instantiate(slotPrefab, container);
            GameModeSlotUI slot = go.GetComponent<GameModeSlotUI>();
            slot.Setup(mode, onSelected);
        }
    }
}