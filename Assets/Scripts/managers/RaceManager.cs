using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public class RacerProgress
    {
        public string racerName;
        public int currentCheckpointIndex;
        public int currentLap;
        public bool finished;
        public float finishTime;
        public int finishPlacement;
        public int humanSlotIndex = -1;
    }
    public void RegisterRacer(RacerProgress progress) => racers.Add(progress);

    public System.Action<List<RacerProgress>> OnRaceEnded; // resultado final, para el panel de resumen

    bool IsHuman(RacerProgress racer) => racer.humanSlotIndex >= 0;

    public RaceCourseSet.CourseVariant activeCourse;
    int totalCheckpointsInCourse => activeCourse != null ? activeCourse.checkpoints.Length : 0;

    List<RacerProgress> racers = new();
    int totalLaps;
    bool raceEndTriggered = false;
    bool gracePeriodStarted = false; // evita lanzar la corrutina más de una vez
    float gracePeriodAfterFirstFinish = 15f;

    public static RaceManager Instance;

    void Awake() => Instance = this;

    void Start()
    {
        if (MapLoader.Instance.IsMapReady)
            OnMapReady();
        else
            MapLoader.Instance.OnMapReady += OnMapReady;
    }

    public void InitializeCheckpoints()
    {
        if (activeCourse == null) return;

        for (int i = 0; i < activeCourse.checkpoints.Length; i++)
        {
            var detector = activeCourse.checkpoints[i].GetComponent<RaceCheckpointDetector>();
            if (detector == null)
                detector = activeCourse.checkpoints[i].gameObject.AddComponent<RaceCheckpointDetector>();

            detector.checkpointIndex = i;
        }
    }

    public void OnCheckpointReached(RacerProgress racer, int checkpointIndex)
    {
        if (raceEndTriggered || racer.finished) return;
        if (checkpointIndex != racer.currentCheckpointIndex) return;

        racer.currentCheckpointIndex++;

        bool completedLap = racer.currentCheckpointIndex >= totalCheckpointsInCourse;
        if (completedLap)
        {
            racer.currentCheckpointIndex = 0;
            racer.currentLap++;

            if (racer.currentLap >= totalLaps)
                HandleRacerFinished(racer);
        }
    }

    void HandleRacerFinished(RacerProgress racer)
    {
        if (raceEndTriggered) return;

        racer.finished = true;
        racer.finishTime = Time.time;
        racer.finishPlacement = racers.Count(r => r.finished);

        bool anyHumanStillRacing = racers.Any(r => !r.finished && IsHuman(r));

        if (!anyHumanStillRacing)
        {
            EndRace(); // todos los humanos ya terminaron (o no había más humanos activos)
        }
        else if (!IsHuman(racer))
        {
            // Un bot cruzó la meta: no corta la carrera, los humanos siguen
        }
        else if (!gracePeriodStarted)
        {
            // Un humano cruzó y otro humano sigue activo: arranca la cuenta regresiva UNA sola vez,
            // sin importar cuántos humanos terminen después durante esa ventana
            gracePeriodStarted = true;
            StartCoroutine(GracePeriodThenEnd());
        }
    }
    void OnMapReady()
    {
        MapLoader.Instance.OnMapReady -= OnMapReady;

        RaceCourseSet courseSet = MapLoader.Instance.GetRaceCourseSet(); // ver nota abajo
        if (courseSet == null)
        {
            Debug.LogError("[RaceManager] El mapa actual no tiene RaceCourseSet.", this);
            return;
        }

        GameModeSO mode = GameSession.Instance.chosenGameMode;
        activeCourse = courseSet.GetCourseFor(mode);

        if (activeCourse == null)
        {
            Debug.LogError($"[RaceManager] El mapa actual no tiene un curso configurado para el modo '{mode.modeName}'.", this);
            return;
        }

        totalLaps = activeCourse.laps;
    }

    IEnumerator GracePeriodThenEnd()
    {
        yield return new WaitForSeconds(gracePeriodAfterFirstFinish);
        EndRace();
    }

    void EndRace()
    {
        if (raceEndTriggered) return;
        raceEndTriggered = true;

        // A los que no cruzaron meta, se les asigna placement según cuánto avanzaron:
        // más vueltas primero, y a igualdad de vuelta, más checkpoints primero
        var unfinished = racers.Where(r => !r.finished)
            .OrderByDescending(r => r.currentLap)
            .ThenByDescending(r => r.currentCheckpointIndex)
            .ToList();

        int nextPlacement = racers.Count(r => r.finished) + 1;
        foreach (var racer in unfinished)
        {
            racer.finishPlacement = nextPlacement;
            nextPlacement++;
        }

        OnRaceEnded?.Invoke(racers.OrderBy(r => r.finishPlacement).ToList());
    }

    public void BeginRace()
    {
        raceEndTriggered = false;
        gracePeriodStarted = false;
    }
}