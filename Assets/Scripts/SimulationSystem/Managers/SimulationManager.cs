//DI
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Timers;
using UnityEngine;

public class SimulationManager : MonoBehaviour,IService
{
    private EventBus _eventBus;
    private SIM_STAT SimulationStat = SIM_STAT.STOP; //статус симуляции в наст. время
    private MODE SimulationMode = MODE.PROGRAM_MODE;
    private MODE oldSimulationMode = MODE.PROGRAM_MODE;
    private TimerSimulation TimeSim = new TimerSimulation();
    void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<StartSimulationSignal>(StartSim);
        _eventBus.Subscribe<SetGyzmoManipulatorModeSignal>(OnSetManipulatorMode);
        _eventBus.Subscribe<PauseSimulationSignal>(PauseSim);
        _eventBus.Subscribe<StopSimulationSignal>(StopSim); 

    }
    private void FixedUpdate()
    {
        if(SimulationStat == SIM_STAT.PLAY)
        { 
            TimeSim.UpdateTimerSim();
        }
    }
    public void Init() { }
    private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
    {
        if (signal.Mode == Assets.Scripts.Managers.SceneManipulatorMode.JOG)
        {
            ChangeMode(MODE.JOG_MODE);
        }
        else if(signal.Mode == Assets.Scripts.Managers.SceneManipulatorMode.Rotation)
        {
            ChangeMode(MODE.ANGLES_MODE);
        }
        else
        {
            ChangeMode(MODE.PROGRAM_MODE);
        }
    }

    private void StartSim(StartSimulationSignal s)
    {
        if (SimulationStat == SIM_STAT.STOP)
        {
            try
            {
                _eventBus.Invoke(new Init());
                ChangeMode(MODE.PROGRAM_MODE);
                SimulationStat = SIM_STAT.PLAY;
                _eventBus.Invoke(new StartProgramm());
                TimeSim.ResetTimer();

            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка запуска симуляции: {ex}");
            }

        }
        if (SimulationStat == SIM_STAT.PAUSE)
        {
            SimulationStat = SIM_STAT.PLAY;
        }
    }
    private void PauseSim(PauseSimulationSignal s)
    {
        if(SimulationStat != SIM_STAT.PAUSE)
        {
            SimulationStat = SIM_STAT.PAUSE;
            _eventBus.Invoke(new PauseProgramm());
        }
    }
    private void StopSim(StopSimulationSignal s)
    {
        if (SimulationStat != SIM_STAT.STOP)
        {
            SimulationStat = SIM_STAT.STOP;
            _eventBus.Invoke(new StopProgramm());
            ChangeOldMode();
            TimeSim.ResetTimer();
        }
    }
    private void ChangeMode(MODE mode)
    {
        oldSimulationMode = SimulationMode;
        SimulationMode = mode;
    }
    private void ChangeOldMode()
    {
        (oldSimulationMode, SimulationMode) = (SimulationMode, oldSimulationMode);
    }


    public SIM_STAT GetStatusSim()
    {
        return SimulationStat;
    }
    public (MODE SimulationMode, MODE oldSimulationMode) GetModeSim()
    {
        (MODE, MODE) modes = (SimulationMode, oldSimulationMode);
        return modes;
    }
    public (int Hours, int Minute, int Seconds) GetTimeSimulation()
    {
        return TimeSim.GetTime();
    }
    public float GetTimeSimulationFloat()
    {
        return TimeSim.GetFloat();
    }
}

public enum SIM_STAT
{
    PLAY,
    STOP,
    PAUSE,
}
public enum MODE
{
    JOG_MODE,
    ANGLES_MODE,
    PROGRAM_MODE
}