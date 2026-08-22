using UnityEngine;
using UnityEngine.Rendering;

public class MapEnvironmentApplier : MonoBehaviour
{
    public Volume globalVolume;           // el Volume persistente en la escena Core
    public Light directionalLight;        // la luz persistente en la escena Core

    public void ApplyMap(MapDataSO map)
    {
        if (globalVolume != null && map.postProcessProfile != null)
            globalVolume.profile = map.postProcessProfile;

        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(map.directionalLightRotation);
            directionalLight.color = map.directionalLightColor;
            directionalLight.intensity = map.directionalLightIntensity;
        }

        RenderSettings.skybox = map.skyboxMaterial;
        DynamicGI.UpdateEnvironment(); // recalcula la luz ambiente que depende del skybox
    }
}