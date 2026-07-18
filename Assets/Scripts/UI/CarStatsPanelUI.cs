using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CarStatsPanelUI : MonoBehaviour
{
    public TextMeshProUGUI carNameText;
    public Slider velocidadBar; // configurar min=0, max=1 en el Inspector
    public Slider pesoBar;
    public Slider resistenciaBar;

    public void ShowStats(CarStatsSO car)
    {
        gameObject.SetActive(car != null);
        if (car == null) return;

        carNameText.text = car.carName;
        velocidadBar.value = car.displaySpeedStat;
        pesoBar.value = car.displayWeightStat;
        resistenciaBar.value = car.displayResistanceStat;
    }
}