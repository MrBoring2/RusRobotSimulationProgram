using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.SimulationSystem.RobotSimulation;

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
        public abstract Awaitable Execute(RobotController rc);
    }
    public class CommandMove : RobotProgrammElement
    {
        private LinearPointPropertyProvider Point { get; set; }
        public CommandMove(LinearPointPropertyProvider p, ENUM_COMMANDS tc, string id) : base(id)
        {
            Point = p;
            TypeComand = tc;
        }
        public async override Awaitable Execute(RobotController rc)
        {
            if (Point.PointType == POINTTYPE.PointToPoint)
            {
                await rc.RobotSetPTPMove(Point);
            }
            else if (Point.PointType == POINTTYPE.LinearPoint)
                await rc.RobotSetLinMove(Point);
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
        public async override Awaitable Execute(RobotController rc)
        {
            await rc.RobotSetWait(Wait);
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
        public async override Awaitable Execute(RobotController rc)
        {
            await rc.RobotSetStateEndEffector(stateEndEffectorProperty);
        }
    }

        public class SubProgramm : RobotProgrammElement
    {
        public List<RobotProgrammElement> ProgrammElement { get; set; } = new List<RobotProgrammElement>();
        public SubProgramm(List<RobotProgrammElement> p, ENUM_COMMANDS tc, string id) : base(id)
        {
            ProgrammElement = p;
            TypeComand = tc;
        }

        public List<RobotProgrammElement> Get()
        {
            return ProgrammElement;
        }
        public async override Awaitable Execute(RobotController rc)
        {
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
