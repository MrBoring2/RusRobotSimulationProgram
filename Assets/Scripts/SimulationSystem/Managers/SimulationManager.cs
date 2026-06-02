//DI
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using UnityEngine;

public class SimulationManager : MonoBehaviour,IService
{
    private EventBus _eventBus;
    private NotificationSystemManager _notification;
    //========= СТАТУСЫ =========//
    private SIM_STAT SimulationStat = SIM_STAT.STOP; //статус симуляции в наст. время
    private MODE SimulationMode = MODE.PROGRAM_MODE; // режим симуляции робота
    private MODE oldSimulationMode = MODE.PROGRAM_MODE;
    private TimerSimulation TimeSim = new TimerSimulation(); //таймер симуляции
    //========= ПАРАМЕТРЫ =========//
    public bool AlarmJointCollision = false;
    public bool AlarmEndEffectorCollicion = true;
    public bool PauseSimInCol = true;
    public bool CheckSpeed = false;
    void Start()
    {
        _notification = ServiceManager.Current.Get<NotificationSystemManager>();//сервис уведомлений
        _eventBus = ServiceManager.Current.Get<EventBus>();//шина событий
        _eventBus.Subscribe<StartSimulationSignal>(StartSim);//подписка на событие интерфейса (старт симуляции)
        _eventBus.Subscribe<SetGyzmoManipulatorModeSignal>(OnSetManipulatorMode); //подписка на событие интерфейса (изменение режима управления роботом)
        _eventBus.Subscribe<PauseSimulationSignal>(PauseSim);//подписка на событие интерфейса (пауза симуляции)
        _eventBus.Subscribe<StopSimulationSignal>(StopSim);//подписка на событие интерфейса (стоп симуляции)
        _eventBus.Subscribe<SystemPauseSim>(SystemPauseSimulation);//подписка на события (пауза симуляции при обнаружении коллизии)

    }
    private void FixedUpdate()
    {
        //работа таймера
        if(SimulationStat == SIM_STAT.PLAY)
        { 
            TimeSim.UpdateTimerSim();
        }
    }
    public void Init() { }
    /// <summary>
    /// изменнеие режима управление роботом (ручной/программный) 
    /// </summary>
    /// <param name="signal"></param>
    private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
    {
        if (signal.Mode == SceneManipulatorMode.JOG)
        {
            ChangeMode(MODE.JOG_MODE);
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
                _notification.ShowInfo("Симуляция запущена");

            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка запуска симуляции: {ex}");
            }

        }
        else if (SimulationStat == SIM_STAT.PAUSE)
        {
            SimulationStat = SIM_STAT.PLAY;
            _notification.ShowInfo("Симуляция продолжается");
        }
    }
    private void PauseSim(PauseSimulationSignal s)
    {
        
        if(SimulationStat != SIM_STAT.PAUSE)
        {
            SimulationStat = SIM_STAT.PAUSE;
            _eventBus.Invoke(new PauseProgramm());
            _notification.ShowInfo("Симуляция приостановлена пользователем");
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
            _notification.ShowInfo("Симуляция остановлена пользователем");
        }
    }
    private void SystemPauseSimulation(SystemPauseSim s)
    {
        if (SimulationStat != SIM_STAT.PAUSE)
        {
            SimulationStat = SIM_STAT.PAUSE;
            _eventBus.Invoke(new PauseProgramm());
            _notification.ShowWarning("Симуляция приостановлена программно");
            _notification.ShowWarning(s.info);
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
    /// <summary>
    /// вкл/выкл уведомление о коллизиях с осями робота
    /// </summary>
    /// <param name="b"></param>
    public void SetAlarmJointCollision(bool b)
    {
       AlarmJointCollision = b;
    }
    /// <summary>
    /// вкл/выкл уведомление о коллизиях с захватом робота
    /// </summary>
    /// <param name="b"></param>
    public void SetAlarmEndEffectorCollicion(bool b)
    {
        AlarmEndEffectorCollicion = b;
    }
    /// <summary>
    /// вкл/выкл паузу при коллизиях с захватом робота
    /// </summary>
    /// <param name="b"></param>
    public void SetPauseSimInCol(bool b)
    {
        PauseSimInCol = b;
    }
    /// <summary>
    /// вкл/выкл проверку превышения скорости робота
    /// </summary>
    /// <param name="b"></param>
    public void SetCheckSpeed(bool b)
    {
        CheckSpeed = b;
    }
    /// <summary>
    /// получение параметров симуляции
    /// </summary>
    /// <returns></returns>
    public (bool AlarmJointColStatus, bool AlarmEndEffectorColStatus, bool PauseSimInCol, bool CheckSpeed) GetSimulationParam()
    {
        return (AlarmJointCollision, AlarmEndEffectorCollicion, PauseSimInCol, CheckSpeed);
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
    PROGRAM_MODE
}