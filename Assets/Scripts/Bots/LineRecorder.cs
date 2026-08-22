using UnityEngine;
using System.Collections.Generic;

public class LineRecorder : MonoBehaviour
{
    public float sampleDistance = 2f;
    public CarController targetCar; // dejalo vacío, se autocompleta

    List<RecordedPoint> recordedPoints = new List<RecordedPoint>();
    Vector3 lastSamplePos;
    bool isRecording = false;

    public void StartRecording()
    {
        if (targetCar == null)
            targetCar = FindPlayerCar();

        if (targetCar == null)
        {
            Debug.LogError("[LineRecorder] No se encontró el auto del jugador. ¿Ya arrancó la carrera?");
            return;
        }

        recordedPoints.Clear();
        lastSamplePos = targetCar.transform.position;
        isRecording = true;
        Debug.Log($"[LineRecorder] Grabando, target = {targetCar.name}");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"[LineRecorder] Detenido. Puntos grabados hasta ahora: {recordedPoints.Count}");
    }

    CarController FindPlayerCar()
    {
        // Busca el auto con playerIndex == 1 (el jugador humano P1)
        foreach (var car in FindObjectsByType<CarController>(FindObjectsSortMode.None))
            if (car.playerIndex == 1) return car;
        return null;
    }

    void FixedUpdate()
    {
        if (!isRecording || targetCar == null) return;

        Vector3 currentPos = targetCar.transform.position;
        float distSinceLastSample = Vector3.Distance(currentPos, lastSamplePos);

        if (distSinceLastSample >= sampleDistance)
        {
            recordedPoints.Add(new RecordedPoint
            {
                position = currentPos,
                speed = targetCar.CurrentSpeed,
                wasHandbraking = targetCar.IsHandbrakeActive // NUEVO — necesita exponerse en CarController
            });
            lastSamplePos = currentPos;
        }
    }

#if UNITY_EDITOR
    public void SaveAsAsset(string assetPath, MapDataSO map, GameModeSO mode)
    {
        var line = ScriptableObject.CreateInstance<RecordedRacingLine>();
        line.map = map;
        line.gameMode = mode;
        line.points = recordedPoints.ToArray();

        UnityEditor.AssetDatabase.CreateAsset(line, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[LineRecorder] Guardado con {recordedPoints.Count} puntos en {assetPath}");
    }
#endif
}