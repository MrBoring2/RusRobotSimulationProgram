using System;
using System.Collections.Generic;

namespace RobotLanguageCompiler.Robot
{
    public enum InternalCommandType
    {
        MovePtp,
        MoveLin,
        Wait,
        OpenEffector,
        CloseEffector
    }

    /// <summary>
    /// Представляет команду движения робота.
    /// </summary>
    public class RobotMoveCommand
    {
        /// <summary>Тип движения: true - PTP (позиционирование по точкам), false - линейное.</summary>
        public bool IsPtp { get; set; }
        /// <summary>Имя целевой точки.</summary>
        public string PointName { get; set; }

        public RobotMoveCommand(bool isPtp, string pointName)
        {
            IsPtp = isPtp;
            PointName = pointName;
        }
    }

    /// <summary>
    /// Представляет команду ожидания.
    /// </summary>
    public class RobotWaitCommand
    {
        /// <summary>Время ожидания в секундах.</summary>
        public float Seconds { get; set; }

        public RobotWaitCommand(float seconds)
        {
            Seconds = seconds;
        }
    }

    /// <summary>
    /// Представляет команду управления захватом (эффектором).
    /// </summary>
    public class RobotEffectorCommand
    {
        /// <summary>true - закрыть захват, false - открыть.</summary>
        public bool IsClosed { get; set; }

        public RobotEffectorCommand(bool isClosed)
        {
            IsClosed = isClosed;
        }
    }

    /// <summary>
    /// Представляет подпрограмму робота (последовательность команд).
    /// </summary>
    public class RobotSubroutine
    {
        /// <summary>Имя подпрограммы.</summary>
        public string Name { get; set; }
        /// <summary>Номер строки объявления.</summary>
        public int Line { get; set; }
        /// <summary>Номер колонки объявления.</summary>
        public int Column { get; set; }
        /// <summary>Список команд подпрограммы.</summary>
        public List<object> Commands { get; set; } = new List<object>();

        public RobotSubroutine(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Представляет полные данные программы робота.
    /// </summary>
    public class RobotProgramData
    {
        /// <summary>Идентификатор робота.</summary>
        public string RobotId { get; set; }
        /// <summary>Список подпрограмм робота.</summary>
        public List<RobotSubroutine> Subroutines { get; set; } = new List<RobotSubroutine>();

        public RobotProgramData(string robotId)
        {
            RobotId = robotId;
        }
    }
}