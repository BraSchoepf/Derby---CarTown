using UnityEngine;

[CreateAssetMenu(fileName = "NewWheelVisual", menuName = "Cars/Wheel Visual")]
public class WheelVisualSO : ScriptableObject
{
    public string wheelName;
    public Sprite previewIcon; // para la UI de selección
    public Mesh wheelMesh;
    public Material[] wheelMaterials;

    [Tooltip("Radio REAL del mesh tal como fue modelado, en unidades de Unity. Medilo en el modelo 3D, no lo inventes a ojo.")]
    public float nativeRadius = 0.35f;

    [Header("Desbloqueo")]
    public string unlockRewardId = "";
}