using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.SystemManager
{
    public class RobotManager
    {
        private static RobotManager _instance;
        private static object syncRoot = new object();
        public static RobotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                            _instance = new RobotManager();
                    }
                }
                return _instance;
            }
        }

        private Dictionary<string, RobotPropertyProvider> robots = new Dictionary<string, RobotPropertyProvider>();
        //private Dictionary<string, RobotCommand> allCommands = new Dictionary<string, RobotCommand>();
        private Dictionary<string, RobotProgramPropertyProvider> programs = new Dictionary<string, RobotProgramPropertyProvider>();
        private Dictionary<string, PointPropertyProvider> points = new Dictionary<string, PointPropertyProvider>();

        //public string RegisterCommand(RobotCommand command)
        //{
        //    allCommands[command.Id] = command;
        //    return command.Id;
        //}

        //public string RegisterProgram(RobotProgramPropertyProvider program, string robotId)
        //{
        //    programs[program.Id] = program;
        //    //robots[robotId].AddProgram(program);
        //    return program.Id;
        //}
        //public void RegisterRobot(RobotPropertyProvider robot)
        //{
        //    robots[robot.Id] = robot;
        //}

        ////public IEnumerable<WaypointPropertyProvider> GetRobotCommands(string robotId)
        //public IEnumerable<WaypointPropertyProvider> GetRobotPoints(string robotId)
        //{
        //    //return points.Values.Where(c => robots[robotId].programs.Contains(programs[c.Parent.Id]));
        //}
    }
}
