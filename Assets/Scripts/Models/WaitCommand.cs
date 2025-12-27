using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class WaitCommand : ISimulationCommand
    {
        //private RobotKinematics robot;
        private int time;
        public bool isCompleted { get; private set; }
        //public P2PMoveCommand(RobotKinematics robot, Vector3 target)
        public WaitCommand(int time)
        {
            //this.robot = robot;
            this.time = time;
        }
        public void Execute()
        {
            //robot.Wait(time); 
            isCompleted = true;
        }

        public void Undo()
        {
            //robot.UndoLastMove();
        }
    }
}
