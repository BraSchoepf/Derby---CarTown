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

        public Transform carTransform;
    }
    public void RegisterRacer(RacerProgress progress) => racers.Add(progress);
    public IReadOnlyList<RacerProgress> Racers => racers;

    public System.Action<RacerProgress> OnRacerFinishedIndividual;
    public System.Action<List<RacerProgress>> OnRaceEnded; // resultado final, para el panel de resumen

    bool IsHuman(RacerProgress racer) => racer.humanSlotIndex >= 0;

    public RaceCourseSet.CourseVariant activeCourse;
    int totalCheckpointsInCourse => activeCourse != null ? activeCourse.checkpoints.Length : 0;

    float raceStartTime;
    List<RacerProgress> racers = new();
    int totalLaps;
    bool raceEndTriggered = false;
    bool gracePeriodStarted = false; // evita lanzar la corrutina más de una vez
    float gracePeriodAfterFirstFinish = 15f;
    bool rankingActive = true;

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
            var checkpointGO = activeCourse.checkpoints[i].gameObject;

            var detector = checkpointGO.GetComponent<RaceCheckpointDetector>();
            if (detector == null)
                detector = checkpointGO.AddComponent<RaceCheckpointDetector>();

            detector.checkpointIndex = i;

            // Si el checkpoint no tiene VFX asignado manualmente, lo buscamos/agregamos
            if (detector.vfx == null)
            {
                detector.vfx = checkpointGO.GetComponent<CheckpointVFX>();
                if (detector.vfx == null)
                    Debug.LogWarning($"[RaceManager] Checkpoint {i} no tiene CheckpointVFX asignado — sin feedback visual.");
            }
        }
    }

    public bool OnCheckpointReached(RacerProgress racer, int checkpointIndex)
    {
        if (raceEndTriggered || racer.finished) return false;
        if (checkpointIndex != racer.currentCheckpointIndex) return false;

        racer.currentCheckpointIndex++;

        bool completedLap = racer.currentCheckpointIndex >= totalCheckpointsInCourse;
        if (completedLap)
        {
            racer.currentCheckpointIndex = 0;
            racer.currentLap++;

            if (racer.currentLap >= totalLaps)
                HandleRacerFinished(racer);
        }

        return true; // el paso fue válido
    }

    void HandleRacerFinished(RacerProgress racer)
    {
        if (raceEndTriggered) return;

        racer.finished = true;
        racer.finishTime = Time.time - raceStartTime;
        racer.finishPlacement = racers.Count(r => r.finished);

        OnRacerFinishedIndividual?.Invoke(racer);

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

        ReportMissionResults(); // ← nuevo, acá todos los placements ya están definitivos

        OnRaceEnded?.Invoke(racers.OrderBy(r => r.finishPlacement).ToList());
    }

    void ReportMissionResults()
    {
        if (MissionManager.Instance == null) return;
        if (GameSession.Instance == null || GameSession.Instance.chosenGameMode == null) return;

        GameModeSO mode = GameSession.Instance.chosenGameMode;
        MapDataSO map = GameSession.Instance.chosenMap;

        foreach (var racer in racers)
        {
            if (!IsHuman(racer)) continue;

            int playerIndex = racer.humanSlotIndex + 1; // 0-based → 1-based (0→1, 1→2)

            MissionManager.Instance.ReportResult(mode, map, MissionObjectiveType.FinishPlacement, racer.finishPlacement, playerIndex);
            MissionManager.Instance.ReportResult(mode, map, MissionObjectiveType.RaceTimeUnder, racer.finishTime, playerIndex);
        }
    }

    public int GetLivePosition(RacerProgress racer)
    {
        if (!rankingActive)
            return racers.IndexOf(racer) + 1; // orden de registro como fallback momentáneo, sin cálculo geométrico

        var ranked = racers.OrderByDescending(r => GetRankingScore(r)).ToList();
        return ranked.IndexOf(racer) + 1;
    }

    float GetRankingScore(RacerProgress r)
    {
        if (r.finished)
            return float.MaxValue - r.finishPlacement;

        if (activeCourse == null || activeCourse.aiPath == null || r.carTransform == null)
            return 0f;

        float pathLength = activeCourse.aiPath.TotalLength;
        float distAlongPath = activeCourse.aiPath.GetClosestPointInfo(r.carTransform.position).progress;

        return r.currentLap * pathLength + distAlongPath;
    }
    public void BeginRace()
    {
        raceEndTriggered = false;
        gracePeriodStarted = false;
        raceStartTime = Time.time;
        rankingActive = false;
        StartCoroutine(EnableRankingNextFrame());
    }
    public float GetRaceTime(RacerProgress racer)
    {
        float endTime = racer.finished ? racer.finishTime : Time.time;
        return endTime - raceStartTime;
    }
    IEnumerator EnableRankingNextFrame()
    {
        yield return null; // esperamos 1 frame a que todos los autos ya estén posicionados en su spawn real
        rankingActive = true;
    }
}