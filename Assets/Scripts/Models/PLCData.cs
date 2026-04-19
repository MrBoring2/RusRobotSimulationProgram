using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Assets.Scripts.Models
{
    public class PLCData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        //Блок инициалзиации
        public List<PLCSetVariable> InitBlockItems = new List<PLCSetVariable>();
        //Блок команд роботов
        public List<PLCRobotBlock> RobotCommandsBlockItems = new List<PLCRobotBlock>();
        //Блок логики
        public List<PLCBase> LogicBlockItems = new List<PLCBase>();
    }
    [Serializable]
    public class PLCBase
    {

    }
    [Serializable]
    public class PLCRobotBlock
    {
        public string RobotId { get; private set; }
        public List<PLCBase> ConditionsList = new List<PLCBase>();
        public PLCRobotBlock(string robotId)
        {
            RobotId = robotId;
        }

    }
    [Serializable]
    public class PLCBlockCondition : PLCBase
    {

        public PLCBlockCondition(PLCCondition IfCondition)
        {
            this.IfCondition = IfCondition;
            Id = Guid.NewGuid().ToString();
        }
        public string Id { get; private set; }
        public PLCCondition IfCondition { get; set; }
        public List<PLCCondition> ElifConditions { get; set; } = new List<PLCCondition>();
        public PLCCondition ElseConndition { get; set; } = new PLCCondition();


    }

    [Serializable]
    public class PLCCondition : PLCBase
    {
        public PLCCondition(string expression = "")
        {
            Expression = expression;
            Id = Guid.NewGuid().ToString();
        }
        public string Id { get; private set; }
        public string Expression = "";
        public List<PLCBase> Content = new List<PLCBase>();
    }
    [Serializable]
    public class PLCCommand : PLCBase
    {
        public string Id { get; private set; }
        public CommandType Type;
        public PLCCommand()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
    [Serializable]
    public enum ConditionType
    {
        If,
        ElseIf,
        Else
    }
    [Serializable]
    public enum CommandType
    {
        StartProgram,
        Increment,
        Decrement,
        SetVariable,
    }
    [Serializable]
    public class PLCStartProgram : PLCCommand
    {
        public string ProgramId;
        public string ProgramName;
        public PLCStartProgram() { Type = CommandType.StartProgram; }
    }

    [Serializable]
    public class PLCIncrement : PLCCommand
    {
        public string VariableName;
        public float Step;
        public PLCIncrement() { Type = CommandType.Increment; }
    }

    [Serializable]
    public class PLCDecrement : PLCCommand
    {
        public string VariableName;
        public float Step;
        public PLCDecrement() { Type = CommandType.Decrement; }
    }


    [Serializable]
    public class PLCSetVariable : PLCCommand
    {
        public string Id;
        public string VariableName;
        public string Value;
        public PLCSetVariable() { Id = Guid.NewGuid().ToString(); Type = CommandType.SetVariable; }
    }
}