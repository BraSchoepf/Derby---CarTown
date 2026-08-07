using UnityEngine;

[System.Serializable]
public struct RecordedPoint
{
    public Vector3 position;
    public float speed; // velocidad real del jugador en ese punto (m/s)
}

[CreateAssetMenu(fileName = "NewRecordedLine", menuName = "AI/Recorded Racing Line")]
public class RecordedRacingLine : ScriptableObject
{
    public MapDataSO map;
    public GameModeSO gameMode;
    public RecordedPoint[] points;

    public int GetClosestPointIndex(Vector3 worldPos)
    {
        int closest = 0;
        float closestDist = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float d = (points[i].position - worldPos).sqrMagnitude;
            if (d < closestDist) { closestDist = d; closest = i; }
        }
        return closest;
    }

    public RecordedPoint GetPoint(int index)
    {
        int wrapped = ((index % points.Length) + points.Length) % points.Length;
        return points[wrapped];
    }
}