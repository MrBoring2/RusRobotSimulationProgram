using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using UnityEngine;

public abstract class CellDeviceBase : MonoBehaviour
{
    protected SimulationManager _SimManager;
    protected SIM_STAT SIM_STATUS { get => ServiceManager.Current.Get<SimulationManager>().GetStatusSim(); }
    protected LogicSignalBus _signalBus;
    protected void Start()
    {
        _SimManager = ServiceManager.Current.Get<SimulationManager>();
        _signalBus = ServiceManager.Current.Get<LogicSignalBus>();
        ServiceManager.Current.Get<EventBus>().Subscribe<StartProgramm>(StartSim);
        ServiceManager.Current.Get<EventBus>().Subscribe<StopProgramm>(StopSim);
    }
    protected abstract void StartSim(StartProgramm s);
    protected abstract void StopSim(StopProgramm s);
}