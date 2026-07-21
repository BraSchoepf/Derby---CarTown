using UnityEngine;

[CreateAssetMenu(fileName = "NewMap", menuName = "Maps/Map Data")]
public class MapDataSO : ScriptableObject
{
    public string mapName;
    public Sprite previewImage;
    [Tooltip("Nombre EXACTO de la escena del mapa, tal como figura en Build Settings")]
    public string sceneName;
}