using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace Assets.Scripts.Models
{
    public abstract class RobotProgrammElement
    {
        public ENUM_COMMANDS TypeComand;
        public string ID { get; set; }
        public RobotProgrammElement(string id)
        {
            ID = id;
        }
        public abstract void Execute(RobotController rc);
    }
    public class CommandMove : RobotProgrammElement
    {
        private LinearPointPropertyProvider Point { get; set; }
        public CommandMove(LinearPointPropertyProvider p, ENUM_COMMANDS tc, string id) : base(id)
        {
            Point = p;
            TypeComand = tc;
        }
        public LinearPointPropertyProvider Get()
        {
            return Point;
        }
        public override void Execute(RobotController rc)
        {
            rc.RobotSetLinMove(Point);
        }
    }
    public class CommandWait : RobotProgrammElement
    {
        private WaitPropertyProvider Wait { get; set; }
        public CommandWait(WaitPropertyProvider p, ENUM_COMMANDS tc, string id) : base(id)
        {
            Wait = p;
            TypeComand = tc;
        }
        public WaitPropertyProvider Get()
        {
            return Wait;
        }
        public override void Execute(RobotController rc)
        {
            rc.RobotSetWait(Wait);
        }
    }
    public class ComandSetStateEndEffector : RobotProgrammElement
    {
        private StateEndEffectorPropertyProvider stateEndEffectorProperty;
        public ComandSetStateEndEffector(StateEndEffectorPropertyProvider p, ENUM_COMMANDS tc, string id)  : base(id)
        {
            stateEndEffectorProperty = p;
            TypeComand = tc;
        }
        public StateEndEffectorPropertyProvider Get()
        {
            return stateEndEffectorProperty;
        }
        public override void Execute(RobotController rc)
        {
            rc.RobotSetStateEndEffector(stateEndEffectorProperty);
        }
    }

        public class SubProgramm : RobotProgrammElement
    {
        private List<RobotProgrammElement> ProgrammElement { get; set; } = new List<RobotProgrammElement>();
        //public SubProgramm(ENUM_COMANDS tc)
        //{
        //    TypeComand = tc;
        //}
        public SubProgramm(List<RobotProgrammElement> p, ENUM_COMMANDS tc, string id) : base(id)
        {
            ProgrammElement = p;
        }

        public List<RobotProgrammElement> Get()
        {
            return ProgrammElement;
        }
        public override void Execute(RobotController rc)
        {
            //rc.RobotSetLinMove(Cmd);
        }

    }




   
    public enum ENUM_COMMANDS
    {
        MOVE_PTP,
        MOVE_LIN,
        WAIT,
        CHANGE_STATE_ENDEFFECTOR,
        SUBPROGRAMM
    }

}
