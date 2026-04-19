using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static Unity.Collections.AllocatorManager;

namespace Assets.Scripts.CustomEventBus.Signals.PLC
{
    public class PLCShowExpressionPanelSignal
    {
        public string ParentId {  get; set; }
        public string Expression { get; set;  }

        public PLCShowExpressionPanelSignal(string parentId, string expression = "")
        {
            ParentId = parentId;
            Expression = expression;
        }
        //public PLCShowExpressionPanelSignal(string parentId, string expression = "")
        //{
        //    ParentId = parentId;
        //    Expression = expression;
        //}
    }

    public class PLCShowSetProgramPanelSignal
    {
        public string BlockId { get; set; }
        public string RobotId { get; set; }
        public PLCShowSetProgramPanelSignal(string blockId, string robotId)
        {
            BlockId = blockId;
            RobotId = robotId;

        }
    }
    public class PLCSelectProgramPanelSignal
    {
        public string BlockId { get; set; }
        public RobotProgramObject Program { get; set; }
        public PLCSelectProgramPanelSignal(string blockId, RobotProgramObject program)
        {
            BlockId = blockId;
            Program = program;

        }
    }

    public class PLCChangeExpressionSignal
    {
        public string ParentId { get; set; }
        public string Expression { get; set; }

        public PLCChangeExpressionSignal(string parentId, string expression = "")
        {
            ParentId = parentId;
            Expression = expression;
        }
    }
}
