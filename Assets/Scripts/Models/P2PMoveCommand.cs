using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class P2PMoveCommand : ISimulationCommand
    {
        //private RobotKinematics robot;
        private Vector3 target;
        public bool isCompleted { get; private set; }
        //public P2PMoveCommand(RobotKinematics robot, Vector3 target)
        public P2PMoveCommand(Vector3 target)
        {
            //this.robot = robot;
            this.target = target;
        }
        public void Execute()
        {
            //robot.P2P(target); 
            isCompleted = true;
        }

        public void Undo()
        {
            //robot.UndoLastMove();
        }
    }
}
