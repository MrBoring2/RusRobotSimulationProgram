using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class WaitForCommand
    {
        //private RobotKinematics robot;
        private int time;
        public string parameter { get; set; }
        public bool value { get; set; }
        public bool isCompleted { get; private set; }
        //public P2PMoveCommand(RobotKinematics robot, Vector3 target)
        public WaitForCommand(string parameter, bool value)
        {
            //this.robot = robot;
            this.parameter = parameter;
            this.value = value; 
        }
        public void Execute()
        {
            //robot.WaitFor(parameter, value); 
            isCompleted = true;
        }

        public void Undo()
        {
            //robot.UndoLastMove();
        }
    }
}
