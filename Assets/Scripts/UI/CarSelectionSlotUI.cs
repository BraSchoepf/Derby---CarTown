using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CarSelectionSlotUI : MonoBehaviour
{
    public CarRegistry registry;
    public Image carPreviewImage;
    public TextMeshProUGUI carNameText;

    int currentIndex = 0;

    public CarStatsSO SelectedCar => registry.cars[currentIndex].stats;

    void Start() => UpdateDisplay();

    public void OnNextCar()
    {
        currentIndex = (currentIndex + 1) % registry.cars.Length;
        UpdateDisplay();
    }

    public void OnPreviousCar()
    {
        currentIndex = (currentIndex - 1 + registry.cars.Length) % registry.cars.Length;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        CarStatsSO car = registry.cars[currentIndex].stats;
        carNameText.text = car.carName;
        if (car.previewImage != null)
            carPreviewImage.sprite = car.previewImage;
    }
}