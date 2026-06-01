using System;
using System.Collections.Generic;

namespace RobotLanguageCompiler.Robot
{
    // Типы команд для внутреннего представления
    public enum InternalCommandType
    {
        MovePtp,
        MoveLin,
        Wait,
        OpenEffector,
        CloseEffector
    }

    // Команда движения
    public class RobotMoveCommand
    {
        public bool IsPtp { get; set; }
        public string PointName { get; set; }

        public RobotMoveCommand(bool isPtp, string pointName)
        {
            IsPtp = isPtp;
            PointName = pointName;
        }
    }

    // Команда ожидания
    public class RobotWaitCommand
    {
        public float Seconds { get; set; }

        public RobotWaitCommand(float seconds)
        {
            Seconds = seconds;
        }
    }

    // Команда управления схватом
    public class RobotEffectorCommand
    {
        public bool IsClosed { get; set; }

        public RobotEffectorCommand(bool isClosed)
        {
            IsClosed = isClosed;
        }
    }

    // Подпрограмма робота
    public class RobotSubroutine
    {
        public string Name { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public List<object> Commands { get; set; } = new List<object>();

        public RobotSubroutine(string name)
        {
            Name = name;
        }
    }

    // Данные программы робота
    public class RobotProgramData
    {
        public string RobotId { get; set; }
        public List<RobotSubroutine> Subroutines { get; set; } = new List<RobotSubroutine>();

        public RobotProgramData(string robotId)
        {
            RobotId = robotId;
        }
    }
}