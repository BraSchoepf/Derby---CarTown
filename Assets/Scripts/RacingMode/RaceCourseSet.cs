using UnityEngine;

public class RaceCourseSet : MonoBehaviour
{
    [System.Serializable]
    public class CourseVariant
    {
        public GameModeSO gameMode;       // referencia al SO de Sprint, Circuito, etc.
        public Transform[] checkpoints;   // orden de paso
        public int laps = 1;              // 1 para Sprint, 3+ para Circuito
    }

    public CourseVariant[] courses;

    public CourseVariant GetCourseFor(GameModeSO mode)
    {
        return System.Array.Find(courses, c => c.gameMode == mode);
    }
}