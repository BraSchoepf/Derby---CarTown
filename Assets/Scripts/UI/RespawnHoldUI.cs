using UnityEngine;
using UnityEngine.UI;

public class RespawnHoldUI : MonoBehaviour
{
    public Slider slider;
    public bool hideWhenNotHolding = true;

    CarController target;
    Image[] visualImages;

    void Awake()
    {
        visualImages = GetComponentsInChildren<Image>(true);
    }

    public void SetTarget(CarController car)
    {
        target = car;
    }

    void Update()
    {
        if (target == null || slider == null) return;

        float progress = target.RespawnHoldProgress;
        slider.value = progress;

        if (hideWhenNotHolding)
            SetVisualsEnabled(progress > 0f);
    }

    void SetVisualsEnabled(bool state)
    {
        foreach (var img in visualImages)
            img.enabled = state;
    }
}