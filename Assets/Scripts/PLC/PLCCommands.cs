//using Assets.Scripts.CustomServiceManager;
//using Assets.Scripts.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;

//namespace Assets.Scripts.PLC
//{
//    public abstract class PLCProgrammElement
//    {
//        public ENUM_PLC_COMMANDS TypeComand { get; set; }
//        public string ID { get; set; }
//        public PLCProgrammElement(string id)
//        {
//            ID = id;
//        }
//        public abstract bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm, string RobotID);
//    }
//    public class PLCCommandBlockRobotsTask : PLCProgrammElement
//    {
//        public string RobotID { get; set; }
//        public List<PLCProgrammElement> ProgrammElements { get; set; } = new List<PLCProgrammElement>();
//        public PLCCommandBlockRobotsTask(string id, string RobotID) : base(id)
//        {
//            this.RobotID = RobotID;
//            TypeComand = ENUM_PLC_COMMANDS.BLOCK_ROBOTS;
//        }
//        public List<PLCProgrammElement> Get()
//        {
//            return ProgrammElements;
//        }
//        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm, string RobotID) { return false; }
//    }
//    public class PLCCommandTask : PLCProgrammElement
//    {
//        public PLCCommandTask(string id) : base(id)
//        {
//            TypeComand = ENUM_PLC_COMMANDS.BLOCK_ROBOT_TASK;
//        }
//        public override bool Execute(RobotController RC, Dictionary<string, List<RobotProgrammElement>> RobotsProgramm, string RobotID ) 
//        {
//            RC.RunTask = true;
//            RC.RunSubProgramm(RobotsProgramm[RobotID].Where(x => x.ID == ID).FirstOrDefault());
//            return true;

//        }
//    }
//    // Блок условия (If - ElseIf - Else)
//    public class PLCConditionBlock : PLCProgrammElement
//    {
//        public List<PLCConditionBranch> Branches { get; set; } = new List<PLCConditionBranch>();

//        public PLCConditionBlock(string id) : base(id)
//        {
//            TypeComand = ENUM_PLC_COMMANDS.BLOCK_CONDITION;
//        }

//        public override bool Execute(RobotController RC,
//                                     Dictionary<string, List<RobotProgrammElement>> RobotsProgramm,
//                                     string currentRobotID)
//        {
//            foreach (var branch in Branches)
//            {
//                if (branch.CheckCondition(RC, currentRobotID)) // Проверяем условие
//                {
//                    // Выполняем все команды внутри этой ветки
//                    foreach (var command in branch.Commands)
//                    {
//                        command.Execute(RC, RobotsProgramm, currentRobotID);
//                    }
//                    return true; // Условие сработало → выходим из блока условия
//                }
//            }

//            // Если ни одно условие не сработало — ничего не делаем и возвращаем false
//            // (логика продолжит проверять следующие блоки на том же уровне)
//            return false;
//        }
//    }
//    public class PLCConditionBranch
//    {
//        public PLCCondition Condition { get; set; }        // само условие (можно сделать абстрактным)
//        public List<PLCProgrammElement> Commands { get; set; } = new List<PLCProgrammElement>();

//        public ENUM_PLC_COMMANDS TYPE_COMMAND  { get; set; } // IF, ELIF или ELSE

//        public bool CheckCondition(RobotController RC, string robotID)
//        {
//            if (TYPE_COMMAND == ENUM_PLC_COMMANDS.ELSE_CONDITION) return true;                    // Else всегда true
//            if (Condition == null) return false;

//            return Condition.Evaluate(RC, robotID);
//        }
//    }
//    public class PLCCondition
//    {
//        public string ConditionString = "";

//        // Словари с переменными (будут передаваться из RobotController или PLC)
//        private Dictionary<string, bool> boolVariables;
//        private Dictionary<string, int> intVariables;

//        public bool Evaluate(RobotController RC, string robotID)
//        {
//            // Получаем актуальные словари переменных для данного робота
//            boolVariables = ServiceManager.Current.Get<LogicSignalBus>().GetSignals();
//            intVariables = ServiceManager.Current.Get<LogicSignalBus>().GetIntData();

//            if (string.IsNullOrWhiteSpace(ConditionString))
//                return false;

//            try
//            {
//                return BooleanExpressionParser.Evaluate(ConditionString, boolVariables, intVariables);
//            }
//            catch (Exception ex)
//            {
//                Debug.LogError($"Ошибка разбора условия: '{ConditionString}'\n{ex.Message}");
//                return false;
//            }
//        }
//    }


//    public enum ENUM_PLC_COMMANDS
//    {
//        BLOCK_ROBOTS,
//        BLOCK_ROBOT_TASK,
//        BLOCK_CONDITION,
//        IF_CONDITION,
//        ELIF_CONDITION,
//        ELSE_CONDITION

//    }
//}
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.PLC
{
    public abstract class PLCProgrammElement
    {
        public ENUM_PLC_COMMANDS TypeComand { get; set; }
        public string ID { get; set; }

        protected PLCProgrammElement(string id)
        {
            ID = id;
        }

        public abstract bool Execute(RobotController RC,
                                     Dictionary<string, List<RobotProgrammElement>> RobotsProgramm,
                                     string RobotID);
    }

    // ==================== БЛОК РОБОТА (верхний уровень) ====================
    public class PLCCommandBlockRobotsTask : PLCProgrammElement
    {
        public string RobotID { get; set; }
        public List<PLCProgrammElement> ProgrammElements { get; set; } = new List<PLCProgrammElement>();

        public PLCCommandBlockRobotsTask(string id, string robotID) : base(id)
        {
            RobotID = robotID;
            TypeComand = ENUM_PLC_COMMANDS.BLOCK_ROBOTS;
        }

        public override bool Execute(RobotController RC,
                                     Dictionary<string, List<RobotProgrammElement>> RobotsProgramm,
                                     string RobotID)
        {
            return false; // Этот класс не должен вызываться напрямую
        }
    }

    // ==================== КОМАНДА ЗАПУСКА ЗАДАЧИ ====================
    public class PLCCommandTask : PLCProgrammElement
    {
        public PLCCommandTask(string id) : base(id)
        {
            TypeComand = ENUM_PLC_COMMANDS.BLOCK_ROBOT_TASK;
        }

        public override bool Execute(RobotController RC,
                                     Dictionary<string, List<RobotProgrammElement>> RobotsProgramm,
                                     string currentRobotID)
        {
            RC.RunTask = true;

            var subProgram = RobotsProgramm[currentRobotID]
                .FirstOrDefault(x => x.ID == ID);

            if (subProgram != null)
                RC.RunSubProgramm(subProgram);
            else
                Debug.LogWarning($"Подпрограмма с ID '{ID}' не найдена для робота {currentRobotID}");

            return true;
        }
    }

    // ==================== БЛОК УСЛОВИЯ (If - ElseIf - Else) ====================
    public class PLCConditionBlock : PLCProgrammElement
    {
        public List<PLCConditionBranch> Branches { get; set; } = new List<PLCConditionBranch>();

        public PLCConditionBlock(string id) : base(id)
        {
            TypeComand = ENUM_PLC_COMMANDS.BLOCK_CONDITION;
        }

        public override bool Execute(RobotController RC,
                                     Dictionary<string, List<RobotProgrammElement>> RobotsProgramm,
                                     string currentRobotID)
        {
            foreach (var branch in Branches)
            {
                if (branch.CheckCondition(RC, currentRobotID))
                {
                    // Выполняем все команды внутри выбранной ветки
                    foreach (var command in branch.Commands)
                    {
                        command.Execute(RC, RobotsProgramm, currentRobotID);
                    }
                    return true; // Условие сработало → больше не проверяем следующие ветки и блоки
                }
            }

            return false; // Ни одно условие не подошло → продолжаем проверять следующие элементы на этом уровне
        }
    }

    // ==================== ВЕТКА УСЛОВИЯ ====================
    public class PLCConditionBranch
    {
        public string ID { get; set; }
        public PLCCondition Condition { get; set; }
        public List<PLCProgrammElement> Commands { get; set; } = new List<PLCProgrammElement>();
        public ENUM_PLC_COMMANDS BranchType { get; set; }   // IF_CONDITION, ELIF_CONDITION, ELSE_CONDITION
        public PLCConditionBranch(string id)
        {
            ID = id;
        }
        public bool CheckCondition(RobotController RC, string robotID)
        {
            if (BranchType == ENUM_PLC_COMMANDS.ELSE_CONDITION)
                return true;

            if (Condition == null)
                return false;

            return Condition.Evaluate(RC, robotID);
        }
    }

    // ==================== УСЛОВИЕ (строка) ====================
    public class PLCCondition
    {
        public string ConditionString = "";

        public PLCCondition(string conditionString)
        {
            ConditionString = conditionString;
        }
        public bool Evaluate(RobotController RC, string robotID)
        {
            if (string.IsNullOrWhiteSpace(ConditionString))
                return false;

            try
            {
                var boolVars = ServiceManager.Current.Get<LogicSignalBus>().GetSignals();
                var intVars = ServiceManager.Current.Get<LogicSignalBus>().GetIntData();

                return BooleanExpressionParser.Evaluate(ConditionString, boolVars, intVars);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка разбора условия '{ConditionString}': {ex.Message}");
                return false;
            }
        }
    }

    // ==================== ENUM ====================
    public enum ENUM_PLC_COMMANDS
    {
        BLOCK_ROBOTS,
        BLOCK_ROBOT_TASK,
        BLOCK_CONDITION,
        IF_CONDITION,
        ELIF_CONDITION,
        ELSE_CONDITION
    }
}