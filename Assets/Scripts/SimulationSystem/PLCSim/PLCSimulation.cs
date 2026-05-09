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
using Assets.Scripts.SimulationSystem.PLC;
using System.Threading;

public class PLCSimulation : MonoBehaviour
{
    private EventBus _eventBus;
    private SimulationManager _simManager;
    private SceneObjectsManager _sceneObjectsManager;
    public Dictionary<string, List<SubProgramm>> RobotsPrograms;
    public List<PLCProgrammElement> PLCProgramm = new List<PLCProgrammElement>();

    IEnumerator cor;
    private void Start()
    {
        _simManager = ServiceManager.Current.Get<SimulationManager>();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _eventBus = ServiceManager.Current.Get<EventBus>();
        //Статусы симуляции//
        _eventBus.Subscribe<StartProgramm>(StartSim);
        //--//
        
    }

    //--Получение программы всех роботов-- ///////////////////////////////////////// потом уберется
    private void GetAllRobotsProg()
    {
        RobotsPrograms = new Dictionary<string, List<SubProgramm>>();
        var list = _sceneObjectsManager.GetGameObjectsList();
        if (list != null)
        {
            foreach (var so in list)
            {
                if (so == null)
                    continue;

                if (so.Type == ObjectType.Robot)
                {
                    RobotsPrograms.Add(so.Id, (so.PropertyProvider as RobotPropertyProvider).RobotController.Programm);
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
    

   
    //--Запуск симуляции--
    void StartSim(StartProgramm s)
    {
        _eventBus.Invoke(new RobotsControllerResetState());
        GetAllRobotsProg();
        //int count = RobotsPrograms.Count;
        //PLCProgramm.Clear();
        ////////////тестовое условие для блока робота//////////
        PLCCommandBlockRobotsTask block = new PLCCommandBlockRobotsTask("1",RobotsPrograms.Keys.First());
        //PLCCommandInit init = new PLCCommandInit("911");
        //PLCConditionBlock condition = new PLCConditionBlock("10");
        //PLCConditionBranch branch = new PLCConditionBranch("21");
        //PLCConditionBranch branch2 = new PLCConditionBranch("22");
        //condition.Branches.Add(branch);
        //condition.Branches.Add(branch2);
        //block.ProgrammElements.Add(condition);
        
        //PLCProgramm.Add(init);
        PLCProgramm.Add(block);

        //branch.Condition = new PLCCondition("key == true");
        //branch2.Condition = new PLCCondition("key2");
        SubProgramm subProgramm1 = RobotsPrograms.Values.First().First();
        //SubProgramm subProgramm2 = RobotsPrograms.Values.First().Skip(1).First() as SubProgramm;
        ////branch.Commands.Add(new PLCCommandTask("31", subProgramm1.ID));
        //branch2.ProgrammElements.Add(new PLCCommandTask("32", subProgramm1.ID));
        //branch.ProgrammElements.Add(new PLCCommandTask("33", subProgramm2.ID));
        //init.ProgrammElements.Add(new PLCCommandSetInt("992", "counter", 99));
        block.ProgrammElements.Add(new PLCCommandTask("31", subProgramm1.ID));
        //PLCCommandCycleBlock cycle = new("1");
        //PLCConditionBlock condition = new("2");
        //PLCConditionBranch conditionBranch = new("3", ENUM_PLC_COMMANDS.IF_CONDITION);
        //PLCConditionBranch conditionBranch2 = new("4", ENUM_PLC_COMMANDS.ELSE_CONDITION);
        //PLCCondition con1 = new("ke1");
        //PLCCommandSetBool set1 = new("4", "key2", true);
        //PLCCommandSetBool set2 = new("5", "key2", false);

        //PLCProgramm.Add(cycle);
        //cycle.ProgrammElements.Add(condition);
        //condition.Branches.Add(conditionBranch);
        //conditionBranch.ProgrammElements.Add(set1);
        //conditionBranch.Condition = con1;
        //condition.Branches.Add(conditionBranch2);
        //conditionBranch2.ProgrammElements.Add(set2);
        //
        //StartCoroutine(ExecuteProgramm());
        StartPLC();
    }
    // старт ПЛК--
    private async void StartPLC()
    {
        ServiceManager.Current.Get<LogicSignalBus>().CreateSignalCadr();
        while (_simManager.GetStatusSim() == SIM_STAT.PLAY)
        {
            if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
            await Awaitable.FixedUpdateAsync();
            // Логика ПЛК
            await PLC();
            ServiceManager.Current.Get<LogicSignalBus>().CadrToActiveSignal();
        }
    }
    public async Awaitable PLC()
    {

        foreach (var programmElement in PLCProgramm)
        {
            try
            {
                if (programmElement.GetType() == typeof(PLCCommandInit))
                {
                    programmElement.Execute();
                }
                if (programmElement.GetType() == typeof(PLCCommandBlockRobotsTask))
                {
                    PLCCommandBlockRobotsTask block = (PLCCommandBlockRobotsTask)programmElement;
                    await ExecuteRobotBlock(block);
                }
                if (programmElement.GetType() == typeof(PLCCommandCycleBlock))
                {
                    PLCCommandCycleBlock block = (PLCCommandCycleBlock)programmElement;
                    await ExecuteCycleBlock(block);
                }

            }

            catch (Exception ex)
            {
                Debug.LogError($"Ошибка исполнителя ПЛК: {ex}");
            }
        }
    }
    
    //--Выполнение блока работы с роботом--
    public async Awaitable ExecuteRobotBlock(PLCCommandBlockRobotsTask BlockRobotTasks)
    {
        string robotID = BlockRobotTasks.RobotID;
        var RC = GetRobotById(robotID).RobotController;

        if (RC == null)
        {
            Debug.LogError($"RobotController для {robotID} не найден!");
            return;
        }
        if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
        while (_simManager.GetStatusSim() == SIM_STAT.PLAY)
        {
            if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
            await Awaitable.FixedUpdateAsync();
          
            // Ждём, пока робот свободен
            while (RC.RunTask)
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                await Awaitable.FixedUpdateAsync();
            }
            foreach (var command in BlockRobotTasks.ProgrammElements)
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                if (command.Execute(RC)) ;// break;
            }
        }
    }
    public async Awaitable ExecuteCycleBlock(PLCCommandCycleBlock block)
    {
        while (_simManager.GetStatusSim() == SIM_STAT.PLAY)
        {
            await Awaitable.FixedUpdateAsync();
            foreach (var element in block.ProgrammElements)
            {
                element.Execute();
            }
        }
    }


    private void OnDestroy()
    {

    }
}

