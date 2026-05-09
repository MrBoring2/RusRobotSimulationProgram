using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    internal class AddWaypointCommand : ICommand, IDestructiveCommand
    {
        private RobotProgramPropertyProvider path;
        private LinearPointPropertyProvider point;
        public AddWaypointCommand(RobotProgramPropertyProvider path)
        {
            this.path = path;
        }

        public void Execute()
        {
            var go = new GameObject("Waypoint");
            point = go.AddComponent<LinearPointPropertyProvider>();
            //point.Owner = path;
            //path.AddPoint(point);
        }

        public void FinalizeDestroy()
        {
            
        }

        public void Undo()
        {
            //path.RemovePoint(point);
        }
    }
}
