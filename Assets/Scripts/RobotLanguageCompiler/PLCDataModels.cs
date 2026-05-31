using System;
using System.Collections.Generic;

namespace RobotLanguageCompiler.PLC
{
    // Копия необходимых структур из PLCData.cs (без зависимостей от Unity)
    
    [Serializable]
    public class PLCData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<PLCInitVariable> InitBlockItems = new List<PLCInitVariable>();
        public List<PLCRobotBlock> RobotCommandsBlockItems = new List<PLCRobotBlock>();
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
    public class PLCBase { }

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
        public PLCCondition ElseCondition { get; set; } = new PLCCondition(ConditionType.Else);
    }

    [Serializable]
    public class PLCCondition : PLCBase
    {
        public PLCCondition(ConditionType conditionType, string expression = "")
        {
            ConditionType = conditionType;
            Expression = expression;
            Id = Guid.NewGuid().ToString();
        }
        public ConditionType ConditionType { get; set; }
        public string Id { get; private set; }
        public string Expression = "";
        public List<PLCBase> Content = new List<PLCBase>();
    }

    [Serializable]
    public class PLCCommand : PLCBase
    {
        public string Id { get; set; }
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
        InitVariable
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
        public PLCIncrement() { Type = CommandType.Increment; }
    }

    [Serializable]
    public class PLCDecrement : PLCCommand
    {
        public string VariableName;
        public PLCDecrement() { Type = CommandType.Decrement; }
    }

    [Serializable]
    public class PLCInitVariable : PLCCommand
    {
        public VarType VarType;
        public string VariableName;
        public string StartValue;
        public PLCInitVariable() { Id = Guid.NewGuid().ToString(); Type = CommandType.InitVariable; }
    }

    [Serializable]
    public class PLCSetVariable : PLCCommand
    {
        public string VariableName;
        public string Value;
        public PLCSetVariable() { Id = Guid.NewGuid().ToString(); Type = CommandType.SetVariable; }
    }

    public enum VarType
    {
        Int,
        Bool
    }
}