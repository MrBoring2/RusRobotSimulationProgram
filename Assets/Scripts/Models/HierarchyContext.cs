using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class HierarchyContext
    {
        public string RobotId;
        public string ProgramId;

        public HierarchyContext(string robotId, string programId)
        {
            RobotId = robotId;
            ProgramId = programId;
        }
    }
}
