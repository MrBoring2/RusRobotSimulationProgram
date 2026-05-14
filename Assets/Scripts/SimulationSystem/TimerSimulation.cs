using UnityEngine;

public class TimerSimulation
{
    public float TimerSim { get; private set; }

    public void ResetTimer()
    {
        TimerSim = 0f;
    }

    public void UpdateTimerSim()
    {
        TimerSim += Time.fixedDeltaTime;
    }

    public (int Hours, int Minute, int Seconds) GetTime()
    {
        int totalSeconds = Mathf.FloorToInt(TimerSim);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return (hours, minutes, seconds);
    }
    public float  GetFloat()
    {
        return TimerSim;
    }
}