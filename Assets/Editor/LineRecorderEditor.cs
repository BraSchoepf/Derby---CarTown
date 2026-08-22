using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LineRecorder))]
public class LineRecorderEditor : Editor
{
    string mapName = "NuevoMapa";
    string modeName = "Circuito";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // muestra los campos normales (sampleDistance, targetCar, etc.)

        LineRecorder recorder = (LineRecorder)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Grabación de línea de carrera", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Entrá en Play Mode para poder grabar.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("● Empezar a grabar", GUILayout.Height(30)))
        {
            recorder.StartRecording();
        }
        if (GUILayout.Button("■ Detener", GUILayout.Height(30)))
        {
            recorder.StopRecording();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Guardar como asset", EditorStyles.boldLabel);

        mapName = EditorGUILayout.TextField("Nombre archivo", mapName);

        if (GUILayout.Button("💾 Guardar vuelta grabada", GUILayout.Height(30)))
        {
            string path = $"Assets/Data/RecordedLines/{mapName}.asset";
            recorder.SaveAsAsset(path, null, null); // los MapDataSO/GameModeSO los asignás después a mano en el asset
            EditorUtility.RevealInFinder(path);
        }
    }
}