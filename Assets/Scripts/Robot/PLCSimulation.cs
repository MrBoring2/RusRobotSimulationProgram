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
using Assets.Scripts.PLC;

public class PLCSimulation : MonoBehaviour
{
    private EventBus _eventBus;
    private SimulationManager _simManager;
    private SceneObjectsManager _sceneObjectsManager;
    public Dictionary<string, List<RobotProgrammElement>> RobotsPrograms;
    public List<PLCProgrammElement> PLCProgramm = new List<PLCProgrammElement>();

    private void Start()
    {
        _simManager = ServiceManager.Current.Get<SimulationManager>();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _eventBus = ServiceManager.Current.Get<EventBus>();
        //Статусы симуляции//
        _eventBus.Subscribe<StartProgramm>(StartSim);
        _eventBus.Subscribe<PauseProgramm>(PauseSim);
        _eventBus.Subscribe<StopProgramm>(StopSim);
        //--//
        _eventBus.Subscribe<PickCommandSignal>(TeleportToPoint);//перенести
    }
    
    //--Получение программы всех роботов--
    private void GetAllRobotsProg()
    {
        RobotsPrograms = new Dictionary<string, List<RobotProgrammElement>>();
        var list = _sceneObjectsManager.GetGameObjectsList();
        if (list != null)
        {
            foreach (var so in list)
            {
                if (so == null)
                    continue;

                if (so.Type == ObjectType.Robot)
                {
                    RobotsPrograms.Add(so.Id, (so.PropertyProvider as RobotPropertyProvider)?.Programm);
                }
            }
        }
    }
    //--Получение провайдера робота по ID--
    private RobotPropertyProvider GetRobotById(string id)
    {
        var obj = _sceneObjectsManager.GetById(id);

            if (obj.Type == ObjectType.Robot && obj.Id == id)
            {
                //возврат провайдера робота
                return obj.PropertyProvider as RobotPropertyProvider;
            }
        // Ничего не найдено
        return null;
    }
    //убрать
    private void TeleportToPoint(PickCommandSignal s)
    {
        if(_simManager.GetStatusSim() == SIM_STAT.STOP)
        {
            SceneObject obj = s.Point;
                RobotPropertyProvider RP;
                if (obj.Type == ObjectType.LinearMoveCommand && ( RP = GetRobotById(_sceneObjectsManager.Commands.GetSubProgram(obj.ParentId).ParentId)) != null)
                {
                    if (s.Point.Type == ObjectType.LinearMoveCommand)
                    {
                        RP.RobotController.TeleportToPoint((LinearPointPropertyProvider)s.Point.PropertyProvider);
                    }
                }
                /*else
                {
                    if(obj.ParentId != null)
                    {
                        if(obj.Type == ObjectType.LinearMoveCommand)
                            obj = _sceneObjectsManager.Commands.GetSubProgram(obj.ParentId);
                        else obj = _sceneObjectsManager.GetById(obj.ParentId);
                    }
                    
                }*/
                
            
        }
    }

   
    //--Запуск симуляции--
    void StartSim(StartProgramm s)
    {
        GetAllRobotsProg();
        int count = RobotsPrograms.Count;

        //////////тестовое условие для блока робота//////////
        PLCCommandBlockRobotsTask block = new PLCCommandBlockRobotsTask("1",RobotsPrograms.Keys.First());
        PLCCommandInit init = new PLCCommandInit("911");
        PLCConditionBlock condition = new PLCConditionBlock("10");
        PLCConditionBranch branch = new PLCConditionBranch("21");
        PLCConditionBranch branch2 = new PLCConditionBranch("22");
        PLCConditionBranch branch3 = new PLCConditionBranch("22");
        PLCConditionBlock condition2 = new PLCConditionBlock("23");
        condition.Branches.Add(branch);
        condition2.Branches.Add(branch3);
        condition.Branches.Add(branch2);
        block.ProgrammElements.Add(condition);

        PLCProgramm.Add(init);
        PLCProgramm.Add(block);

        branch.Condition = new PLCCondition("key == true && counter == 98");
        branch3.Condition = new PLCCondition("key2");
        branch2.Condition = new PLCCondition("key == true && counter == 99");
        SubProgramm subProgramm1 = RobotsPrograms.Values.First().First() as SubProgramm;
        SubProgramm subProgramm2 = RobotsPrograms.Values.First().Skip(1).First() as SubProgramm;
        SubProgramm subProgramm3 = RobotsPrograms.Values.First().Skip(2).First() as SubProgramm;
        //branch.Commands.Add(new PLCCommandTask("31", subProgramm1.ID));
        branch2.ProgrammElements.Add(new PLCCommandTask("32", subProgramm2.ID));
        branch3.ProgrammElements.Add(new PLCCommandTask("33", subProgramm3.ID));
        branch3.ProgrammElements.Add(new PLCCommandSetBool("993", "key2", false));
        branch.ProgrammElements.Add(condition2);
        init.ProgrammElements.Add(new PLCCommandSetInt("992", "counter", 1000));
        StartPLC();
        //StartCoroutine(ExecuteProgramm());
    }
    //--"Асинхронный" старт блоков работы с роботом--
    public void StartPLC()
    {
        foreach(var programmElement in PLCProgramm)
        {

            /*if(programmElement.TypeComand == ENUM_PLC_COMMANDS.BLOCK_ROBOTS)
            {
                PLCCommandBlockRobotsTask block = (PLCCommandBlockRobotsTask)programmElement;
                StartCoroutine(ExecuteRobotBlock(block));
            }*/
            try
            {
                if(programmElement.GetType() == typeof(PLCCommandInit))
                {
                    programmElement.Execute();
                }
                if(programmElement.GetType() == typeof(PLCCommandBlockRobotsTask))
                {
                    PLCCommandBlockRobotsTask block = (PLCCommandBlockRobotsTask)programmElement;
                    StartCoroutine(ExecuteRobotBlock(block));
                }
                
            }
            
            catch (Exception ex)
            {
                Debug.LogError($"Не удалсь запустить ПЛК {ex}");
            }
        }
    }
    //--Выполнение блока работы с роботом--
    public IEnumerator ExecuteRobotBlock(PLCCommandBlockRobotsTask BlockRobotTasks)
    {
        string robotID = BlockRobotTasks.RobotID;
        var RC = GetRobotById(robotID).RobotController;

        if (RC == null)
        {
            Debug.LogError($"RobotController для {robotID} не найден!");
            yield break;
        }

        while (_simManager.GetStatusSim() == SIM_STAT.PLAY)
        {
            yield return new WaitForFixedUpdate();

            // Ждём, пока робот свободен
            yield return new WaitUntil(() => !RC.RunTask);

            foreach (var element in BlockRobotTasks.ProgrammElements)
            {
                //если робот занят выходим
                if (GetRobotById(robotID).RobotController.RunTask) break;
                bool success = element.Execute(RC, RobotsPrograms);
            }

            // Если ничего не выполнилось — можно добавить логику "по умолчанию"
        }
    }
    //public IEnumerator ExecuteRobotBlock(PLCCommandBlockRobotsTask BlockRobotTasks)
    //{
    //    string RobotID = BlockRobotTasks.RobotID;
    //    while (_simManager.GetStatusSim() == SIM_STAT.PLAY)
    //    {
    //        yield return new WaitForFixedUpdate();
    //        yield return new WaitUntil(() => !GetRobotById(RobotID).RobotController.RunTask);
    //        foreach (PLCProgrammElement comand in BlockRobotTasks.Get())
    //        {
    //            if(comand.TypeComand == ENUM_PLC_COMMANDS.BLOCK_ROBOT_TASK)
    //            {
    //                bool flag = comand.Execute(GetRobotById(RobotID).RobotController, RobotsPrograms, BlockRobotTasks.RobotID);
    //                if (flag) { break; }
    //                //для теста
    //                yield return new WaitUntil(() => _simManager.GetStatusSim() == SIM_STAT.PLAY && !GetRobotById(RobotID).RobotController.RunTask);
    //            }
    //            else
    //            {
    //                Debug.LogError("Вложенный блок управления роботом не поддерживается");
    //            }

    //        }
    //    }

    //}

    // Êîðóòèíà äëÿ ïîñëåäîâàòåëüíîãî âûïîëíåíèÿ ïðîãðàììû
    //private IEnumerator ExecuteProgramm()
    //{
    //    currentCommandIndex = 0;

    //    while (LocalSimStat != SIM_STAT.STOP)
    //    {
    //        if (currentCommandIndex < Programm.Count)
    //        {
    //            RobotProgrammElement element = Programm[currentCommandIndex];

    //            yield return new WaitUntil(() => allowNextCommand && LocalSimStat == SIM_STAT.PLAY );

    //            yield return ExecuteElementProgramm(element);

    //            currentCommandIndex++;
    //        }
    //        else if(_simManager.GetStatusSim() == SIM_STAT.PLAY)
    //        {
    //            currentCommandIndex = 0;
    //        }
    //        else
    //        {
    //            yield break;
    //        }
    //    }
    //}
    //public IEnumerator ExecuteElementProgramm(RobotProgrammElement EP)
    //{
    //    if (CheckComand(EP))
    //    {
    //        HandlerCommand(EP);

    //        yield return new WaitUntil(() => allowNextCommand);
    //    }
    //    else if (EP.TypeComand == ENUM_COMMANDS.SUBPROGRAMM)
    //    {
    //        SubProgramm subProgramm = (SubProgramm)EP;

    //        yield return ExecuteSubProgramm(subProgramm);
    //    }
    //}


    //private IEnumerator ExecuteSubProgramm(SubProgramm subProgramm)
    //{
    //    foreach (var element in subProgramm.Get())
    //    {
    //        yield return new WaitUntil(() => allowNextCommand && LocalSimStat == SIM_STAT.PLAY );

    //        yield return ExecuteElementProgramm(element);
    //    }
    //}


    /*private void StartSim(StartProgramm s)
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
        

    }*/
    private void PauseSim(PauseProgramm s)
    {
        //LocalSimStat = SIM_STAT.PAUSE;
        
    }
    private void StopSim(StopProgramm s)
    {
        //LocalSimStat = SIM_STAT.STOP;
        //RC.StopSim();
        StopAllCoroutines();
    }
    private void ContinueSim()
    {
        //LocalSimStat = SIM_STAT.PLAY;
        
    }
    //private void EndCurrentMove(RobotEndMove s)
    //{
    //    if(s.RoboID == _propertyProvider.Id)
    //    {
    //        allowNextCommand = true;
    //    }
    //}

    //public bool CheckComand(RobotProgrammElement c)
    //{
    //    switch (c.TypeComand)
    //    {
    //        case ENUM_COMMANDS.MOVE_PTP: return true;
    //        case ENUM_COMMANDS.MOVE_LIN: return true;
    //        case ENUM_COMMANDS.WAIT: return true;
    //        case ENUM_COMMANDS.CHANGE_STATE_ENDEFFECTOR: return true;
    //        default: return false;
    //    }
    //}
    //public void HandlerCommand(RobotProgrammElement c)
    //{
    //    //InProgress();
    //    //c.Execute(RC);  
    //}

    private void OnDestroy()
    {
        _eventBus?.Unsubcribe<StartProgramm>(StartSim);
        _eventBus?.Unsubcribe<PauseProgramm>(PauseSim);

        _eventBus?.Unsubcribe<PickCommandSignal>(TeleportToPoint);
    }
}

