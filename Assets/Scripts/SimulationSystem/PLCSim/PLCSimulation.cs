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

    private void Start()
    {
        _simManager = ServiceManager.Current.Get<SimulationManager>();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _eventBus = ServiceManager.Current.Get<EventBus>();

        _eventBus.Subscribe<StartProgramm>(StartSim);
        
    }

    
    PLCBlockInit Init;
    List<PLCCommandBlockRobotsTask> RobotsBlocks;
    PLCCommandLogicBlock LogicBlock;
    //--Запуск симуляции--
    void StartSim(StartProgramm s)
    {
        (PLCBlockInit Init, List<PLCCommandBlockRobotsTask> RobotsBlocks, PLCCommandLogicBlock LogicBlocks) PLC = PLCDataConverter.Convert(_sceneObjectsManager.PLCData);
        Init = PLC.Init;
        RobotsBlocks = PLC.RobotsBlocks;
        LogicBlock = PLC.LogicBlocks;
        StartPLC();
    }
    // старт ПЛК--
    private async void StartPLC()
    {
        foreach(var cmd in Init.ProgrammElements)
        {
            cmd.Execute();
        }
        //ServiceManager.Current.Get<LogicSignalBus>().CadrToActiveSignal();
        while (_simManager.GetStatusSim() == SIM_STAT.PLAY)
        {
           // ServiceManager.Current.Get<LogicSignalBus>().CreateSignalCadr();
            if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
            
            // Логика ПЛК
            await PLC();
            //ServiceManager.Current.Get<LogicSignalBus>().CadrToActiveSignal();
            await Awaitable.WaitForSecondsAsync(0.1f);
        }
    }
    /// <summary>
    /// Цикл ПЛК
    /// </summary>
    /// <returns></returns>
    public async Awaitable PLC()
    {
        foreach(var block in RobotsBlocks)
        {
            await ExecuteRobotBlock(block);
        }
        await ExecuteCycleBlock(LogicBlock);
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
        // Ждём, пока робот свободен
        if (!RC.RunTask)
        {
            foreach (var command in BlockRobotTasks.ProgrammElements)
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                if (command.Execute(RC));// break;
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
    public async Awaitable ExecuteCycleBlock(PLCCommandLogicBlock block)
    {
        if (block.ProgrammElements == null) return;
            foreach (var element in block.ProgrammElements)
            {
                element.Execute();
            }
    }


    private void OnDestroy()
    {

    }
}

public static class PLCDataConverter
{
    public static (PLCBlockInit Init, List<PLCCommandBlockRobotsTask> RobotsBlocks, PLCCommandLogicBlock LogicBlocks) Convert(PLCData data)
    {
        (PLCBlockInit Init, List<PLCCommandBlockRobotsTask> RobotsBlocks, PLCCommandLogicBlock LogicBlocks) result = new();

        result.RobotsBlocks = new List<PLCCommandBlockRobotsTask>();

        if (data == null) return result;

        // Init block
        var init = new PLCBlockInit("1");
        foreach (var initVar in data.InitBlockItems ?? new List<PLCInitVariable>())
        {
            var el = ParseInitVariable(initVar);
            if (el != null) init.ProgrammElements.AddRange(el);
        }
        result.Init = init;

        // Robot blocks
        foreach (var rb in data.RobotCommandsBlockItems ?? new List<PLCRobotBlock>())
        {
            var block = new PLCCommandBlockRobotsTask(rb.RobotId, rb.RobotId)
            {
                ProgrammElements = ParsePLCBaseList(rb?.ConditionsList)
            };
            result.RobotsBlocks.Add(block);
        }

        // Logic blocks (верхний уровень)\
        PLCCommandLogicBlock LogicBlock = new("2");
        foreach (var b in data.LogicBlockItems ?? new List<PLCBase>())
        {
            var parsed = ParsePLCBase(b);
            if (parsed != null) LogicBlock.ProgrammElements.AddRange(parsed);
        }
        result.LogicBlocks = LogicBlock;

        return result;
    }

    private static List<PLCProgrammElement> ParsePLCBaseList(List<PLCBase> list)
    {
        var outList = new List<PLCProgrammElement>();
        if (list == null) return outList;
        foreach (var item in list)
        {
            var parsed = ParsePLCBase(item);
            if (parsed != null) outList.AddRange(parsed);
        }
        return outList;
    }

    private static List<PLCProgrammElement> ParsePLCBase(PLCBase item)
    {
        var outList = new List<PLCProgrammElement>();
        if (item == null) return outList;

        // PLCBlockCondition -> PLCConditionBlock
        if (item is PLCBlockCondition blockCond)
        {
            var condBlock = new PLCConditionBlock(blockCond.Id);
            // If branch
            if (blockCond.IfCondition != null)
            {
                var branch = new PLCConditionBranch(blockCond.IfCondition.Id, ENUM_PLC_COMMANDS.IF_CONDITION)
                {
                    Condition = new PLCConditionStr(blockCond.IfCondition.Expression),
                    ProgrammElements = ParsePLCBaseList(blockCond.IfCondition.Content)
                };
                condBlock.Branches.Add(branch);
            }

            // Elif branches
            foreach (var elif in blockCond.ElifConditions)
            {
                var branch = new PLCConditionBranch(elif.Id, ENUM_PLC_COMMANDS.ELIF_CONDITION)
                {
                    Condition = new Assets.Scripts.SimulationSystem.PLC.PLCConditionStr(elif.Expression),
                    ProgrammElements = ParsePLCBaseList(elif.Content)
                };
                condBlock.Branches.Add(branch);
            }

            // Else
            if (blockCond.ElseConndition != null)
            {
                var elseBranch = new PLCConditionBranch(blockCond.ElseConndition.Id, ENUM_PLC_COMMANDS.ELSE_CONDITION)
                {
                    Condition = null,
                    ProgrammElements = ParsePLCBaseList(blockCond.ElseConndition.Content)
                };
                condBlock.Branches.Add(elseBranch);
            }

            outList.Add(condBlock);
            return outList;
        }

        // PLCCondition (single condition block)
        /*if (item is PLCCondition singleCond)
        {
            var condBlock = new PLCConditionBlock(singleCond.Id ?? Guid.NewGuid().ToString());
            var branch = new PLCConditionBranch(singleCond.Id ?? Guid.NewGuid().ToString(), ENUM_PLC_COMMANDS.IF_CONDITION)
            {
                Condition = new PLCCCondition(singleCond.Expression),
                ProgrammElements = ParsePLCBaseList(singleCond.Content)
            };
            condBlock.Branches.Add(branch);
            outList.Add(condBlock);
            return outList;
        }*/

        // PLCCommand and derived
        if (item is PLCCommand cmd)
        {
            // Start program => PLCCommandTask
            if (cmd is PLCStartProgram start)
            {
                var com = new PLCCommandTask(start.Id, start.ProgramId);
                outList.Add(com);
                return outList;
            }

            // Increment => PLCCommandSetIncrement (int)
            if (cmd is PLCIncrement inc)
            {
                var val = ConvertFloatToIntSafe(inc.Step);
                var setInc = new PLCCommandSetIncrement(cmd.Id, inc.VariableName, val);
                outList.Add(setInc);
                return outList;
            }

            // Decrement => PLCCommandSetIncrement with negative value
            if (cmd is PLCDecrement dec)
            {
                var val = -ConvertFloatToIntSafe(dec.Step);
                var setInc = new PLCCommandSetIncrement(cmd.Id, dec.VariableName, val);
                outList.Add(setInc);
                return outList;
            }

            // Init variable => create set/assign depending on type
            if (cmd is PLCInitVariable initVar)
            {
                var created = ParseInitVariableToSet(initVar);
                if (created != null) outList.AddRange(created);
                return outList;
            }

            // Set variable => OperationType
            if (cmd is PLCSetVariable setVar)
            {
                var parsed = ParseSetVariable(setVar);
                if (parsed != null) outList.AddRange(parsed);
                return outList;
            }

            Debug.LogWarning($"PLC converter: неизвестная команда PLCCommand (id={cmd.Id}) типа {cmd.GetType().Name}");
        }

        Debug.LogWarning($"PLC converter: неожиданный тип PLCBase {item.GetType().Name}");
        return outList;
    }

    private static List<PLCProgrammElement> ParseInitVariableToSet(PLCInitVariable initVar)
    {
        var list = new List<PLCProgrammElement>();
        if (initVar == null) return list;

        switch (initVar.VarType)
        {
            case VarType.Int:
                if (int.TryParse(initVar.StartValue, out var iv))
                {
                    list.Add(new PLCCommandSetInt(initVar.Id, initVar.VariableName, iv));
                }
                else
                {
                    Debug.LogWarning($"PLC converter: не удалось распарсить int StartValue для {initVar.VariableName}");
                }
                break;
            case VarType.Bool:
                if (bool.TryParse(initVar.StartValue, out var bv))
                {
                    list.Add(new PLCCommandSetBool(initVar.Id, initVar.VariableName, bv));
                }
                else if (int.TryParse(initVar.StartValue, out var ib))
                {
                    list.Add(new PLCCommandSetBool(initVar.Id, initVar.VariableName, ib != 0));
                }
                else
                {
                    Debug.LogWarning($"PLC converter: не удалось распарсить bool StartValue для {initVar.VariableName}");
                }
                break;
            /*case VarType.Float:
                // нет прямого float-set в целевых PLCProgrammElement — логируем
                Debug.LogWarning($"PLC converter: Float init for '{initVar.VariableName}' не поддерживается целевой моделью. Значение='{initVar.StartValue}'");
                break;
            case VarType.String:
                Debug.LogWarning($"PLC converter: String init for '{initVar.VariableName}' не поддерживается целевой моделью.");
                break;*/
        }

        return list;
    }

    private static List<PLCProgrammElement> ParseInitVariable(PLCInitVariable initVar)
    {
        return ParseInitVariableToSet(initVar);
    }

    private static List<PLCProgrammElement> ParseSetVariable(PLCSetVariable setVar)
    {
        var list = new List<PLCProgrammElement>();
        if (setVar == null) return list;

        switch (setVar.VarType)
        {
            case VarType.Int:
                if (setVar.Operation == OperationType.Assign)
                {
                    if (int.TryParse(setVar.Value, out var iv))
                    {
                        list.Add(new PLCCommandSetInt(setVar.Id, setVar.VariableName, iv));
                    }
                    else
                    {
                        Debug.LogWarning($"PLC converter: не удалось распарсить int Value для {setVar.VariableName}");
                    }
                }
                else if (setVar.Operation == OperationType.Increment)
                {
                    if (int.TryParse(setVar.Value, out var incVal))
                    {
                        list.Add(new PLCCommandSetIncrement(setVar.Id, setVar.VariableName, incVal));
                    }
                    else if (float.TryParse(setVar.Value, out var fInc))
                    {
                        list.Add(new PLCCommandSetIncrement(setVar.Id, setVar.VariableName, ConvertFloatToIntSafe(fInc)));
                    }
                    else
                    {
                        Debug.LogWarning($"PLC converter: не удалось распарсить increment Value для {setVar.VariableName}");
                    }
                }
                else if (setVar.Operation == OperationType.Decrement)
                {
                    if (int.TryParse(setVar.Value, out var decVal))
                    {
                        list.Add(new PLCCommandSetIncrement(setVar.Id, setVar.VariableName, -decVal));
                    }
                    else if (float.TryParse(setVar.Value, out var fDec))
                    {
                        list.Add(new PLCCommandSetIncrement(setVar.Id, setVar.VariableName, -ConvertFloatToIntSafe(fDec)));
                    }
                    else
                    {
                        Debug.LogWarning($"PLC converter: не удалось распарсить decrement Value для {setVar.VariableName}");
                    }
                }
                break;

            case VarType.Bool:
                if (setVar.Operation == OperationType.Assign)
                {
                    if (bool.TryParse(setVar.Value, out var bv))
                    {
                        list.Add(new PLCCommandSetBool(setVar.Id, setVar.VariableName, bv));
                    }
                    else if (int.TryParse(setVar.Value, out var ib))
                    {
                        list.Add(new PLCCommandSetBool(setVar.Id, setVar.VariableName, ib != 0));
                    }
                    else
                    {
                        Debug.LogWarning($"PLC converter: не удалось распарсить bool Value для {setVar.VariableName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"PLC converter: операция {setVar.Operation} для bool не поддерживается");
                }
                break;

            case VarType.Float:
                Debug.LogWarning($"PLC converter: операции с Float не поддержаны целевой моделью");
                break;

            case VarType.String:
                Debug.LogWarning($"PLC converter: операции со String не поддержаны целевой моделью");
                break;
        }

        return list;
    }

    private static int ConvertFloatToIntSafe(float f)
    {
        return Mathf.RoundToInt(f);
    }
}
