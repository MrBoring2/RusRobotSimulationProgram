//////using Assets.Scripts.CustomServiceManager;
//////using Assets.Scripts.Models;
//////using System;
//////using System.Collections.Generic;
//////using System.Linq;
//////using System.Text;
//////using UnityEngine;

//////namespace Assets.Scripts.PLC
//////{
//////    public abstract class PLCProgrammElement
//////    {
//////        public ENUM_PLC_COMMANDS TypeComand { get; set; }
//////        public string ID { get; set; }
//////        public PLCProgrammElement(string id)
//////        {
//////            ID = id;
//////        }
//////        public abstract bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm, string RobotID);
//////    }
//////    public class PLCCommandBlockRobotsTask : PLCProgrammElement
//////    {
//////        public string RobotID { get; set; }
//////        public List<PLCProgrammElement> ProgrammElements { get; set; } = new List<PLCProgrammElement>();
//////        public PLCCommandBlockRobotsTask(string id, string RobotID) : base(id)
//////        {
//////            this.RobotID = RobotID;
//////            TypeComand = ENUM_PLC_COMMANDS.BLOCK_ROBOTS;
//////        }
//////        public List<PLCProgrammElement> Get()
//////        {
//////            return ProgrammElements;
//////        }
//////        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm, string RobotID) { return false; }
//////    }
//////    public class PLCCommandTask : PLCProgrammElement
//////    {
//////        public PLCCommandTask(string id) : base(id)
//////        {
//////            TypeComand = ENUM_PLC_COMMANDS.BLOCK_ROBOT_TASK;
//////        }
//////        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm, string RobotID ) 
//////        {
//////            RC.RunTask = true;
//////            RC.RunSubProgramm(RobotsProgramm[RobotID].Where(x => x.ID == ID).FirstOrDefault());
//////            return true;

//////        }
//////    }
//////    // Блок условия (If - ElseIf - Else)
//////    public class PLCConditionBlock : PLCProgrammElement
//////    {
//////        public List<PLCConditionBranch> Branches { get; set; } = new List<PLCConditionBranch>();

//////        public PLCConditionBlock(string id) : base(id)
//////        {
//////            TypeComand = ENUM_PLC_COMMANDS.BLOCK_CONDITION;
//////        }

//////        public override bool Execute(RobotController RC,
//////                                     Dictionary<string, List<RobotProgrammElement>> RobotsProgramm,
//////                                     string currentRobotID)
//////        {
//////            foreach (var branch in Branches)
//////            {
//////                if (branch.CheckCondition(RC, currentRobotID)) // Проверяем условие
//////                {
//////                    // Выполняем все команды внутри этой ветки
//////                    foreach (var command in branch.Commands)
//////                    {
//////                        command.Execute(RC, RobotsProgramm, currentRobotID);
//////                    }
//////                    return true; // Условие сработало → выходим из блока условия
//////                }
//////            }

//////            // Если ни одно условие не сработало — ничего не делаем и возвращаем false
//////            // (логика продолжит проверять следующие блоки на том же уровне)
//////            return false;
//////        }
//////    }
//////    public class PLCConditionBranch
//////    {
//////        public PLCCondition Condition { get; set; }        // само условие (можно сделать абстрактным)
//////        public List<PLCProgrammElement> Commands { get; set; } = new List<PLCProgrammElement>();

//////        public ENUM_PLC_COMMANDS TYPE_COMMAND  { get; set; } // IF, ELIF или ELSE

//////        public bool CheckCondition(RobotController RC, string robotID)
//////        {
//////            if (TYPE_COMMAND == ENUM_PLC_COMMANDS.ELSE_CONDITION) return true;                    // Else всегда true
//////            if (Condition == null) return false;

//////            return Condition.Evaluate(RC, robotID);
//////        }
//////    }
//////    public class PLCCondition
//////    {
//////        public string ConditionString = "";

//////        // Словари с переменными (будут передаваться из RobotController или PLC)
//////        private Dictionary<string, bool> boolVariables;
//////        private Dictionary<string, int> intVariables;

//////        public bool Evaluate(RobotController RC, string robotID)
//////        {
//////            // Получаем актуальные словари переменных для данного робота
//////            boolVariables = ServiceManager.Current.Get<LogicSignalBus>().GetSignals();
//////            intVariables = ServiceManager.Current.Get<LogicSignalBus>().GetIntData();

//////            if (string.IsNullOrWhiteSpace(ConditionString))
//////                return false;

//////            try
//////            {
//////                return BooleanExpressionParser.Evaluate(ConditionString, boolVariables, intVariables);
//////            }
//////            catch (Exception ex)
//////            {
//////                Debug.LogError($"Ошибка разбора условия: '{ConditionString}'\n{ex.Message}");
//////                return false;
//////            }
//////        }
//////    }


////    public enum ENUM_PLC_COMMANDS
////    {
////        BLOCK_ROBOTS,
////        BLOCK_ROBOT_TASK,
////        BLOCK_CONDITION,
////        IF_CONDITION,
////        ELIF_CONDITION,
////        ELSE_CONDITION

////    }
////}
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.SimulationSystem.RobotSimulation;
namespace Assets.Scripts.SimulationSystem.PLC
{
    public abstract class PLCProgrammElement
    {
        public string ID { get; set; }

        protected PLCProgrammElement(string id)
        {
            ID = id;
        }

        public abstract bool Execute(RobotController RC = null, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm = null);
    }
    // ==================== БЛОК ИНИЦИАЛИЗАЦИИ (верхний уровень) ====================
    /// <summary>
    /// блок инициализации
    /// </summary>
    public class PLCCommandInit : PLCProgrammElement
    {
        public List<PLCProgrammElement> ProgrammElements { get; set; } = new List<PLCProgrammElement>();
        public PLCCommandInit(string id) : base(id)
        {

        }
        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm)
        {
            foreach (var pe in ProgrammElements)
            {
                pe.Execute(RC, RobotsProgramm);
            }
            return false;
        }
    }

    // ==================== БЛОК РОБОТА (верхний уровень) ====================
    /// <summary>
    /// блок работы с роботом
    /// </summary>
    public class PLCCommandBlockRobotsTask : PLCProgrammElement
    {
        public string RobotID { get; set; }
        public List<PLCProgrammElement> ProgrammElements { get; set; } = new List<PLCProgrammElement>();

        public PLCCommandBlockRobotsTask(string id, string robotID) : base(id)
        {
            RobotID = robotID;
        }

        public override bool Execute(RobotController RC, Dictionary<string,
            List<RobotProgrammElement>> RobotsProgramm)
        { return false; }
    }

    // ==================== КОМАНДА ЗАПУСКА ЗАДАЧИ ====================
    /// <summary>
    /// Команда запуска задачи робота
    /// </summary>
    public class PLCCommandTask : PLCProgrammElement
    {
        public string IDTaskToRun { get; set; } // ID задачи которую нужно запустить
        public PLCCommandTask(string id, string IDTaskToRun) : base(id)
        {
            this.IDTaskToRun = IDTaskToRun;
        }

        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm)
        {

            RC.RunSubProgramm(IDTaskToRun);
            return true;
        }
    }

    // ==================== БЛОК УСЛОВИЯ (If - ElseIf - Else) ====================
    /// <summary>
    /// блок условий
    /// </summary>
    public class PLCConditionBlock : PLCProgrammElement
    {
        public List<PLCConditionBranch> Branches { get; set; } = new List<PLCConditionBranch>();

        public PLCConditionBlock(string id) : base(id)
        {
        }

        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm)
        {
            foreach (var branch in Branches)
            {
                if (branch.CheckCondition(RC))
                {
                    // Выполняем все команды внутри выбранной ветки
                    foreach (var command in branch.ProgrammElements)
                    {
                        if (command.Execute(RC, RobotsProgramm)) { } /*return true;*/
                    }
                    return true; // Условие сработало - больше не проверяем следующие ветки и блоки
                }
            }

            return false;
        }
    }

    // ==================== ВЕТКА УСЛОВИЯ ====================
    /// <summary>
    /// ветка условия
    /// </summary>
    public class PLCConditionBranch
    {
        public string ID { get; set; }
        public PLCCondition Condition { get; set; }
        public List<PLCProgrammElement> ProgrammElements { get; set; } = new List<PLCProgrammElement>();
        public ENUM_PLC_COMMANDS BranchType { get; set; }   // IF_CONDITION, ELIF_CONDITION, ELSE_CONDITION
        public PLCConditionBranch(string id, ENUM_PLC_COMMANDS type)
        {
            ID = id;
            BranchType = type;
        }
        public bool CheckCondition(RobotController RC)
        {
            if (BranchType == ENUM_PLC_COMMANDS.ELSE_CONDITION)
                return true;

            if (Condition == null)
                return false;

            return Condition.Evaluate();
        }
    }

    // ==================== УСЛОВИЕ (строка) ====================
    /// <summary>
    /// ветка условия
    /// </summary>
    public class PLCCondition
    {
        public string ConditionString = "";

        public PLCCondition(string conditionString)
        {
            ConditionString = conditionString;
        }
        public bool Evaluate()
        {
            if (string.IsNullOrWhiteSpace(ConditionString))
                return false;

            try
            {
                var boolVars = ServiceManager.Current.Get<LogicSignalBus>().GetCopySignals();
                var intVars = ServiceManager.Current.Get<LogicSignalBus>().GetCopyIntData();

                return BooleanExpressionParser.Evaluate(ConditionString, boolVars, intVars);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка разбора условия '{ConditionString}': {ex.Message}");
                return false;
            }
        }
    }

    // ==================== КОМАНДА УСТАНОВИТЬ ЗНАЧЕНИЕ (bool) ====================
    /// <summary>
    /// команда задать булево значение
    /// </summary>
    public class PLCCommandSetBool : PLCProgrammElement
    {
        public string SignalName { get; set; }
        public bool ValueToSet { get; set; }
        public PLCCommandSetBool(string id, string Name, bool Value) : base(id)
        {
            SignalName = Name;
            ValueToSet = Value;
        }
        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm)
        {
            ServiceManager.Current.Get<LogicSignalBus>().SetCopySignal(SignalName, ValueToSet);
            return false;
        }
    }

    // ==================== КОМАНДА УСТАНОВИТЬ ЗНАЧЕНИЕ (int) ====================
    /// <summary>
    /// команда задать численное значение
    /// </summary>
    public class PLCCommandSetInt : PLCProgrammElement
    {
        public string DataName { get; set; }
        public int ValueToSet { get; set; }
        public PLCCommandSetInt(string id, string Name, int Value) : base(id)
        {
            DataName = Name;
            ValueToSet = Value;
        }
        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm)
        {
            ServiceManager.Current.Get<LogicSignalBus>().SetCopyIntData(DataName, ValueToSet);
            return false;
        }
    }
    public class PLCCommandSetIncrement : PLCProgrammElement
    {
        public string DataName { get; set; }
        public int ValueToSet { get; set; }
        public PLCCommandSetIncrement(string id, string Name, int Value) : base(id)
        {
            DataName = Name;
            ValueToSet = Value;
        }
        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm)
        {
            var LSB = ServiceManager.Current.Get<LogicSignalBus>();
            LSB.SetCopyIntData(DataName, LSB.GetCopyIntData(DataName) + ValueToSet);
            return false;
        }
    }
    public class PLCCommandCycleBlock : PLCProgrammElement
    {
        public List<PLCProgrammElement> ProgrammElements { get; set; } = new List<PLCProgrammElement>();
        public PLCCommandCycleBlock(string id) : base(id)
        {
        }
        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm)
        {
            foreach (var el in ProgrammElements)
            {
                el.Execute(RC, RobotsProgramm);
            }
            return false;
        }
    }

    // ==================== ПЕРЕЧИСЛЕНИЕ ТИПОВ КОМАНД ====================
    public enum ENUM_PLC_COMMANDS
    {
        BLOCK_ROBOTS,
        BLOCK_ROBOT_TASK,
        BLOCK_CONDITION,
        IF_CONDITION,
        ELIF_CONDITION,
        ELSE_CONDITION,
        SET_BOOL_SIGNAL,
        SET_INT_SIGNAL,
        BLOCK_CYCLE
    }
}