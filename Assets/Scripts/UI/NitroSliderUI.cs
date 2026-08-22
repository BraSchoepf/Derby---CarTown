using UnityEngine;
using UnityEngine.UI;

public class NitroSliderUI : MonoBehaviour
{
    public Image fillImage; // Image con Fill Method: Horizontal (o el estilo que uses)
    CarController target;

    public void SetTarget(CarController car)
    {
        target = car;
    }

    void Update()
    {
        if (target == null || fillImage == null) return;
        fillImage.fillAmount = target.NitroPercent;
    }
}