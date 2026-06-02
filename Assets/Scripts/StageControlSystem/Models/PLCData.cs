using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class PLCData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        //Блок инициалзиации
        public List<PLCInitVariable> InitBlockItems = new List<PLCInitVariable>();
        //Блок команд роботов
        public List<PLCRobotBlock> RobotCommandsBlockItems = new List<PLCRobotBlock>();
        //Блок логики
        public List<PLCBase> LogicBlockItems = new List<PLCBase>();
        public List<Variable> Variables = new List<Variable>();
    }
    [Serializable]
    public class Variable
    {
        public Variable(string id, VarType type, string name)
        {
            Id = id;
            VarType = type;
            Name = name;
        }
        public string Id { get; set; }

        public VarType VarType { get; set; }
        public string Name { get; set; }

    }


    [Serializable]
    public class PLCBase
    {
        public string Id { get; private set; }

        public PLCBase()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
    [Serializable]
    public class PLCRobotBlock : PLCBase
    {
        public string RobotId { get; private set; }
        public List<PLCBase> ConditionsList = new List<PLCBase>();
        public PLCRobotBlock(string robotId) : base()
        {
            RobotId = robotId;
        }

    }
    [Serializable]
    public class PLCBlockCondition : PLCBase
    {

        public PLCBlockCondition(PLCCondition IfCondition) : base()
        {
            this.IfCondition = IfCondition;
        }
        
        public PLCCondition IfCondition { get; set; }
        public List<PLCCondition> ElifConditions { get; set; } = new List<PLCCondition>();
        public PLCCondition ElseCondition { get; set; } = new PLCCondition(ConditionType.Else);


    }

    [Serializable]
    public class PLCCondition : PLCBase
    {
        public PLCCondition(ConditionType conditionType, string expression = "") : base()
        {
            ConditionType = conditionType;
            Expression = expression;
        }
        public ConditionType ConditionType { get; set; }
        public string Expression = "";
        public List<PLCBase> Content = new List<PLCBase>();
    }
    [Serializable]
    public class PLCCommand : PLCBase
    {
        public CommandType Type;
        public PLCCommand() : base()
        {

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
        InitVariable
    }
    [Serializable]
    public class PLCStartProgram : PLCCommand
    {
        public string ProgramId;
        public string ProgramName;
        public PLCStartProgram() : base() { Type = CommandType.StartProgram; }
    }

    [Serializable]
    public class PLCIncrement : PLCCommand
    {
        public string VariableName;
        public float Step;
        public PLCIncrement() : base() { Type = CommandType.Increment; }
    }

    [Serializable]
    public class PLCDecrement : PLCCommand
    {
        public string VariableName;
        public float Step;
        public PLCDecrement() : base() { Type = CommandType.Decrement; }
    }
    [Serializable]
    public class PLCInitVariable : PLCCommand
    {
        public VarType VarType;
        public string VariableName;
        public string StartValue;
        public PLCInitVariable() : base() { Type = CommandType.InitVariable; }
    }

    [Serializable]
    public class PLCSetVariable : PLCCommand
    {
        public VarType VarType;
        public OperationType Operation;
        public string VariableName;
        public string Value;
        public PLCSetVariable() : base() { Type = CommandType.SetVariable; }
    }

    public enum VarType
    {
        Int,
        String,
        Float,
        Bool
    }
    public enum OperationType
    {
        Increment,
        Decrement,
        Assign
    }
}