//DI
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RobotProgrammSimulation : MonoBehaviour
{
    private EventBus _eventBus;
    private SimulationManager _simManager;
    private SIM_STAT LocalSimStat;
    private RobotController RC;
    private RobotPropertyProvider _propertyProvider => gameObject.GetComponent<RobotPropertyProvider>();
    public List<RobotProgrammElement> Programm => _propertyProvider.Programm.ToList();
    
    //---//
    private int currentCommandIndex = 0;//текущая выполняемая в программе, именно на вехрнем уровне не в подпрограммах
    public bool allowNextCommand = true;//разреш. на след. команду


    private void Start()
    {
        _simManager = ServiceManager.Current.Get<SimulationManager>();
        _eventBus = ServiceManager.Current.Get<EventBus>();
        RC = gameObject.GetComponent<RobotController>();
        //Сигналы//
        _eventBus.Subscribe<StartProgramm>(StartSim);
        _eventBus.Subscribe<PauseProgramm>(PauseSim);
        _eventBus.Subscribe<StopProgramm>(StopSim);
        _eventBus.Subscribe<RobotEndMove>(EndCurrentMove);

        //_propertyProvider.oldXYZ = Vector3.zero;

    }
    private void FixedUpdate()
    {
        if(_simManager.GetModeSim() == MODE.JOG_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
        {
            RC.SetJogMove(_propertyProvider.JOGpoint);
        }
    }

    void StartProgramm()
    {
        StartCoroutine(ExecuteProgramm());
    }

    // Корутина для последовательного выполнения программы
    private IEnumerator ExecuteProgramm()
    {
        currentCommandIndex = 0;

        while (LocalSimStat != SIM_STAT.STOP)
        {
            if (currentCommandIndex < Programm.Count)
            {
                RobotProgrammElement element = Programm[currentCommandIndex];

                yield return new WaitUntil(() => allowNextCommand && LocalSimStat == SIM_STAT.PLAY );

                yield return ExecuteElementProgramm(element);

                currentCommandIndex++;
            }
            else
            {
                yield break;
            }
        }
    }
    public IEnumerator ExecuteElementProgramm(RobotProgrammElement EP)
    {
        if (CheckComand(EP))
        {
            HandlerCommand(EP);
            
            yield return new WaitUntil(() => allowNextCommand);
        }
        else if (EP is SubProgramm)
        {
            SubProgramm subProgramm = (SubProgramm)EP;

            yield return ExecuteSubProgramm(subProgramm);
        }
    }


    private IEnumerator ExecuteSubProgramm(SubProgramm subProgramm)
    {
        foreach (var element in subProgramm.Get())
        {
            yield return new WaitUntil(() => allowNextCommand && LocalSimStat == SIM_STAT.PLAY );

            yield return ExecuteElementProgramm(element);
        }
    }

    public void InProgress()
    {
        allowNextCommand = false;
    }
    private void StartSim(StartProgramm s)
    {
        LocalSimStat = SIM_STAT.PLAY;
        allowNextCommand = true;
        RC.SetAllowNextMove(true);
        StartProgramm();

    }
    private void PauseSim(PauseProgramm s)
    {
        LocalSimStat = SIM_STAT.PAUSE;
        RC.SetAllowNextMove(false);
    }
    private void StopSim(StopProgramm s)
    {
        LocalSimStat = SIM_STAT.STOP;
        RC.StopSim();
        StopAllCoroutines();
    }
    private void EndCurrentMove(RobotEndMove s)
    {
        if(s.RoboID == _propertyProvider.Id)
        {
            allowNextCommand = true;
        }
    }

    public bool CheckComand(RobotProgrammElement c)
    {
        switch (c.TypeComand)
        {
            case ENUM_COMMANDS.MOVE_PTP: return true;
            case ENUM_COMMANDS.MOVE_LIN: return true;
            case ENUM_COMMANDS.WAIT: return true;
            case ENUM_COMMANDS.CHANGE_STATE_ENDEFFECTOR: return true;
            default: return false;
        }
    }
    public void HandlerCommand(RobotProgrammElement c)
    {
        switch (c.TypeComand)
        {
            case ENUM_COMMANDS.MOVE_PTP:
                
                break;
            case ENUM_COMMANDS.MOVE_LIN:
                CommandMove comand0 = (CommandMove)c;
                InProgress();
                comand0.Execute(RC);
                break;
            case ENUM_COMMANDS.WAIT: 

                break;
            case ENUM_COMMANDS.CHANGE_STATE_ENDEFFECTOR:
                ComandSetStateEndEffector comand1 = (ComandSetStateEndEffector)c;
                InProgress();
                comand1.Execute(RC);
                break;
            default: break;
        }
        
    }
    private void OnDestroy()
    {
        _eventBus?.Unsubcribe<StartProgramm>(StartSim);
        _eventBus?.Unsubcribe<PauseProgramm>(PauseSim);
        _eventBus?.Unsubcribe<StopProgramm>(StopSim);
        _eventBus?.Unsubcribe<RobotEndMove>(EndCurrentMove);

    }
}

