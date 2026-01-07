//DI
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using System;
using UnityEngine;

public class SimulationManager : MonoBehaviour,IService
{
    private EventBus _eventBus;
    private SIM_STAT SimulationStat = SIM_STAT.STOP; //статус симул€ции в наст. врем€
    private MODE SimulationMode = MODE.NONE; // режим симул€ции, пока так.
    void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<StartSimulationSignal>(StartSim);
        _eventBus.Subscribe<SetGyzmoManipulatorModeSignal>(OnSetManipulatorMode);
        //_eventBus.Subscribe<PauseSimulationSignal>(PauseSim);

        _eventBus.Subscribe<PauseSimulationSignal>(JOGHandler); //нужен сигнал JOG  один и тот же сигнал на вкл и выкл
        //_eventBus.Subscribe<---->(StopSim);  //Ќужен сигнал —“ќѕ_—»ћ”Ћя÷»я

    }

    private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
    {
        if (SimulationStat == SIM_STAT.STOP && signal.Mode == Assets.Scripts.Managers.SceneManipulatorMode.JOG)
        {
            SimulationMode = MODE.JOG_MODE;
        }
        else
        {
            SimulationMode = MODE.NO_STEP;
        }
    }

    private void StartSim(StartSimulationSignal s)
    {
        if(1==1/*SimulationStat != SIM_STAT.RESUME*/)
        {
            SimulationStat = SIM_STAT.RESUME;
            SimulationMode = MODE.NONE;
            _eventBus.Invoke(new StartProgramm());
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
    private void StopSim(/*--*/)
    {
        if (SimulationStat != SIM_STAT.STOP)
        {
            SimulationStat = SIM_STAT.STOP;
            _eventBus.Invoke(new StopProgramm());
        }
    }
    private void JOGHandler(PauseSimulationSignal s)
    {
        if(SimulationStat == SIM_STAT.STOP && SimulationMode != MODE.JOG_MODE)
        {
            SimulationMode = MODE.JOG_MODE;
        }
        else if(SimulationStat == SIM_STAT.STOP)
        {
            SimulationMode = MODE.NONE;
        }
    }



    public SIM_STAT GetStatusSim()
    {
        return SimulationStat;
    }
    public MODE GetModeSim()
    {
        return SimulationMode;
    }

}

public enum SIM_STAT
{
    START,
    STOP,
    PAUSE,
    RESUME,
    NONE
}
public enum MODE
{
    STEP,
    NO_STEP,
    JOG_MODE,
    NONE
}