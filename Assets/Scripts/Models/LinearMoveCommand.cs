using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class LinearMoveCommand : ISimulationCommand
    {
        //private RobotKinematics robot;
        private Vector3 target;
        public bool isCompleted { get; private set; }
        //public P2PMoveCommand(RobotKinematics robot, Vector3 target)
        public LinearMoveCommand(Vector3 target)
        {
            //this.robot = robot;
            this.target = target;
        }
        public void Execute()
        {
            //robot.Linear(target); 
            isCompleted = true;
        }

        public void Undo()
        {
            //robot.UndoLastMove();
        }
    }
}
