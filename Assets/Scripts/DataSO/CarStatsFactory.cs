using UnityEngine;

public static class CarStatsFactory
{
    public static CarStatsSO BuildEffectiveStats(CarStatsSO baseStats, DrivingProfileSO profile)
    {
        CarStatsSO runtimeStats = Object.Instantiate(baseStats);
        if (profile != null) profile.ApplyTo(runtimeStats);
        return runtimeStats;
    }
}