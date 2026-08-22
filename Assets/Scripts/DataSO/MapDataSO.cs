using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "NewMap", menuName = "Maps/Map Data")]
public class MapDataSO : ScriptableObject
{
    public string mapName;
    public Sprite previewImage;
    [Tooltip("Nombre EXACTO de la escena del mapa, tal como figura en Build Settings")]
    public string sceneName;
    public GameModeSO[] compatibleModes;

    [Header("Ambiente")]
    public VolumeProfile postProcessProfile;
    public Material skyboxMaterial;
    public Vector3 directionalLightRotation; // ángulos de Euler
    public Color directionalLightColor = Color.white;
    public float directionalLightIntensity = 1f;
}