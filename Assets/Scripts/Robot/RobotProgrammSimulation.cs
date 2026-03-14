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
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.Providers;
using Assets.Scripts.Managers;

public class RobotProgrammSimulation : MonoBehaviour
{
    private EventBus _eventBus;
    private SimulationManager _simManager;
    private SceneObjectsManager _sceneObjectsManager;
    private SIM_STAT LocalSimStat;
    private RobotController RC;
    private RobotPropertyProvider _propertyProvider => gameObject.GetComponent<RobotPropertyProvider>();
    public List<RobotProgrammElement> Programm => _propertyProvider.Programm.ToList();
    
    //---//
    private int currentCommandIndex = 0;//òåêóùàÿ âûïîëíÿåìàÿ â ïðîãðàììå, èìåííî íà âåõðíåì óðîâíå íå â ïîäïðîãðàììàõ
    public bool allowNextCommand = true;//ðàçðåø. íà ñëåä. êîìàíäó


    private void Start()
    {
        _simManager = ServiceManager.Current.Get<SimulationManager>();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _eventBus = ServiceManager.Current.Get<EventBus>();
        RC = gameObject.GetComponent<RobotController>();
        //Ñèãíàëû ñèìóëÿöèè//
        _eventBus.Subscribe<StartProgramm>(StartSim);
        _eventBus.Subscribe<PauseProgramm>(PauseSim);
        _eventBus.Subscribe<StopProgramm>(StopSim);
        _eventBus.Subscribe<RobotEndMove>(EndCurrentMove);
        //--//
        _eventBus.Subscribe<PickCommandSignal>(TeleportToPoint);

    }
    private void TeleportToPoint(PickCommandSignal s)
    {
        if(_simManager.GetStatusSim() == SIM_STAT.STOP)
        {
            SceneObject obj = s.Point;
            for(int i = 0; i < 20; i++)
            {
                if( obj.Type == ObjectType.Robot && obj.Id == _propertyProvider.Id)
                {
                    if (s.Point.Type == ObjectType.LinearMoveCommand)
                    {
                        RC.TeleportToPoint((LinearPointPropertyProvider)s.Point.PropertyProvider);
                        break;
                    }
                }
                else
                {
                    if(obj.ParentId != null)
                    {
                        obj = _sceneObjectsManager.GetById(obj.ParentId);
                    }
                    
                }
                
            }
            
        }
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

    // Êîðóòèíà äëÿ ïîñëåäîâàòåëüíîãî âûïîëíåíèÿ ïðîãðàììû
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
            else if(_simManager.GetStatusSim() == SIM_STAT.PLAY)
            {
                currentCommandIndex = 0;
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
        else if (EP.TypeComand == ENUM_COMMANDS.SUBPROGRAMM)
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
        if(LocalSimStat == SIM_STAT.PAUSE)
        {
            ContinueSim();
        }
        else
        {
            LocalSimStat = SIM_STAT.PLAY;
            allowNextCommand = true;
            RC.SetAllowNextMove(true);
            StartProgramm();
        }
        

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
    private void ContinueSim()
    {
        LocalSimStat = SIM_STAT.PLAY;
        RC.SetAllowNextMove(true);
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
        InProgress();
        c.Execute(RC);  
    }
    private void OnDestroy()
    {
        _eventBus?.Unsubcribe<StartProgramm>(StartSim);
        _eventBus?.Unsubcribe<PauseProgramm>(PauseSim);
        _eventBus?.Unsubcribe<StopProgramm>(StopSim);
        _eventBus?.Unsubcribe<RobotEndMove>(EndCurrentMove);

        _eventBus?.Unsubcribe<PickCommandSignal>(TeleportToPoint);
    }
}

