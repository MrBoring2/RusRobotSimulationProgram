using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using UnityEngine;

public abstract class CellDeviceBase : MonoBehaviour
{
    protected SceneObjectsManager _sceneObjectsManager;
    protected SimulationManager _SimManager;
    protected SIM_STAT SIM_STATUS { get => ServiceManager.Current.Get<SimulationManager>().GetStatusSim(); }
    protected LogicSignalBus _signalBus;
    protected EventBus _eventBus => ServiceManager.Current.Get<EventBus>();
    protected void Start()
    {
        _SimManager = ServiceManager.Current.Get<SimulationManager>();
        _signalBus = ServiceManager.Current.Get<LogicSignalBus>();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _eventBus.Subscribe<StartProgramm>(StartSim);
        _eventBus.Subscribe<StopProgramm>(StopSim);
    }
    protected abstract void StartSim(StartProgramm s);
    protected abstract void StopSim(StopProgramm s);
}