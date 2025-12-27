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

public class RobotSimulation : MonoBehaviour
{
    private RobotController RC;
    private RobotPropertyProvider _propertyProvider => gameObject.GetComponent<RobotPropertyProvider>();

    //public EVENTS EVENTS;
    public bool allowNextCommand = true;
    public SIM Status;
    public List<RobotProgrammElement> Programm => _propertyProvider.Programm.ToList();
    private EventBus _eventBus;

    private int currentCommandIndex = 0;//текущая выполняемая в программе, именно на вехрнем уровне не в подпрограммах

    private MODE mode = MODE.NONE;

    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        RC = gameObject.GetComponent<RobotController>();

        _eventBus.Subscribe<StartSimulationSignal>(StartSim);
        _eventBus.Subscribe<PauseSimulationSignal>(PauseSim);
        //EVENTS.StatusSim += UpdateStatusSim;
        //EVENTS.Mode += UpdateMode;
        _propertyProvider.EndMoveEvent += AllowNext;
       // Programm = _
        _propertyProvider.oldXYZ = Vector3.zero;
        //GameObject points = GameObject.Find("Points");
        //List<GameObject> list = new();
        //foreach (Transform point in points.transform)
        //{
        //    list.Add(point.gameObject);
        //}

        ///////создание программы/////
        
        //_propertyProvider.AddComand(new ComandMove(list[0].GetComponent<CmdLinMovePorpertyProvider>(),ENUM_COMANDS.MOVE_LIN,"1T"));
        //SubProgramm SubP = new(ENUM_COMANDS.SUBPROGRAMM);
        //SubP.Addcomand(new ComandMove(list[1].GetComponent<CmdLinMovePorpertyProvider>(), ENUM_COMANDS.MOVE_LIN,"2T"));
        //SubP.Addcomand(new ComandSetStateEndEffector(list[2].GetComponent<CmdSetStateEndEffectorPropertyProvider>(), ENUM_COMANDS.CHANGE_STATE_ENDEFFECTOR,"3T"));
        //SubP.Addcomand(new ComandMove(list[3].GetComponent<CmdLinMovePorpertyProvider>(), ENUM_COMANDS.MOVE_LIN,"4T"));
        //_propertyProvider.AddSubProgramm(SubP);
        //_propertyProvider.AddComand(new ComandMove(list[4].GetComponent<CmdLinMovePorpertyProvider>(), ENUM_COMANDS.MOVE_LIN, "5T"));
        //SubProgramm SubP2 = new(ENUM_COMANDS.SUBPROGRAMM);
        //SubP2.Addcomand(new ComandSetStateEndEffector(list[5].GetComponent<CmdSetStateEndEffectorPropertyProvider>(), ENUM_COMANDS.CHANGE_STATE_ENDEFFECTOR, "6T"));
        //_propertyProvider.AddSubprogrammInSubProgramm(SubP, SubP2);

    }
    private void FixedUpdate()
    {
        if(mode == MODE.JOG_MODE)
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

        while (Status != SIM.STOP)
        {
            if (currentCommandIndex < Programm.Count)
            {
                RobotProgrammElement element = Programm[currentCommandIndex];

                yield return new WaitUntil(() => allowNextCommand && Status == SIM.RESUME );

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
            HandlerComand(EP);
            
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
            yield return new WaitUntil(() => allowNextCommand && Status == SIM.RESUME );

            yield return ExecuteElementProgramm(element);
        }
    }
    //public void UpdateStatusSim(SIM sim)
    //{
    //    RC.StatusSim(sim);
    //    switch (sim)
    //    {
    //        case SIM.START: StartSim(); break;
    //        case SIM.STOP: StopSim(); break;
    //        case SIM.PAUSE: PauseSim(); break;
    //        case SIM.RESUME: ResumeSim(); break;
    //        default: break;
    //    }
    //}
    public void StartSim(StartSimulationSignal s)
    {
        RC.StatusSim(SIM.START);
        Status = SIM.RESUME;
        allowNextCommand = true;
        StartProgramm();

    }

    public void StopSim()
    {
        RC.StatusSim(SIM.STOP);
        Status = SIM.STOP;
        currentCommandIndex = 0;
        _propertyProvider.SyncJOGPosition();
        StopAllCoroutines();
        
    }

    public void PauseSim(PauseSimulationSignal s)
    {
        Status = SIM.PAUSE;

    }

    public void ResumeSim()
    {
        Status = SIM.RESUME;

    }

    public void InProgress()
    {
        allowNextCommand = false;
    }

    public void AllowNext()
    {
        if (mode == MODE.STEP)
        {
            Status = SIM.PAUSE;
        }
        allowNextCommand = true;

        
    }

    public void UpdateMode(MODE m)
    {
        mode = m;
    }
    public bool CheckComand(RobotProgrammElement c)
    {
        switch (c.TypeComand)
        {
            case ENUM_COMANDS.MOVE_PTP: return true;
            case ENUM_COMANDS.MOVE_LIN: return true;
            case ENUM_COMANDS.WAIT: return true;
            case ENUM_COMANDS.CHANGE_STATE_ENDEFFECTOR: return true;
            default: return false;
        }
    }
    public void HandlerComand(RobotProgrammElement c)
    {
        switch (c.TypeComand)
        {
            case ENUM_COMANDS.MOVE_PTP:
                
                break;
            case ENUM_COMANDS.MOVE_LIN:
                CommandMove comand0 = (CommandMove)c;
                InProgress();
                comand0.Execute(RC);
                break;
            case ENUM_COMANDS.WAIT: break;
            case ENUM_COMANDS.CHANGE_STATE_ENDEFFECTOR:
                ComandSetStateEndEffector comand1 = (ComandSetStateEndEffector)c;
                InProgress();
                comand1.Execute(RC);
                break;
            default: break;
        }
        
    }
}

