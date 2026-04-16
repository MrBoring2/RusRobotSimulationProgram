using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Assets.Scripts.Models
{
    public class PLCData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<PLCCondition> ConditionBlocks = new List<PLCCondition>();
    }

    [Serializable]
    public class PLCCondition
    {
        public string Id;
        public ConditionType Type;
        public string Expression;
        public List<PLCCommand> Commands = new List<PLCCommand>();
    }
    public class PLCCommand
    {
        public string Id;
        public CommandType Type;
    }
    public enum ConditionType
    {
        If,
        ElseIf,
        Else
    }

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
        public string VariableName;
        public string Value;
        public PLCSetVariable() { Type = CommandType.SetVariable; }
    }
}