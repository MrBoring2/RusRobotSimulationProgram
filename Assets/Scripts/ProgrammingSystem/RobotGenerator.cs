using System.Globalization;
using System.Text;

namespace RobotLanguageCompiler.Robot
{
    public class RobotGenerator
    {
        private const string Indent = "\t";

        /// <summary>
        /// Генерирует исходный код на языке программирования роботов из внутренней структуры данных.
        /// </summary>
        /// <param name="data">Объект RobotProgramData, содержащий подпрограммы и команды.</param>
        /// <returns>Строка с сгенерированным кодом программы робота.</returns>
        public string Generate(RobotProgramData data)
        {
            var sb = new StringBuilder();

            foreach (var subroutine in data.Subroutines)
            {
                sb.AppendLine($"{subroutine.Name} {{");
                
                foreach (var command in subroutine.Commands)
                {
                    string commandLine = FormatCommand(command);
                    sb.AppendLine($"{Indent}{commandLine}");
                }
                
                sb.AppendLine($"}}");
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Форматирует отдельную команду в строковое представление языка.
        /// </summary>
        /// <param name="command">Объект команды (RobotMoveCommand, RobotWaitCommand, RobotEffectorCommand).</param>
        /// <returns>Строковое представление команды в синтаксисе языка роботов.</returns>
        private string FormatCommand(object command)
        {
            switch (command)
            {
                case RobotMoveCommand move:
                    string moveType = move.IsPtp ? "ptp_point" : "lin_point";
                    return $"{moveType}({move.PointName})";
                    
                case RobotWaitCommand wait:
                    string seconds = wait.Seconds.ToString(CultureInfo.InvariantCulture);
                    return $"wait({seconds})";
                    
                case RobotEffectorCommand effector:
                    return effector.IsClosed ? "close_effector" : "open_effector";
                    
                default:
                    return "";
            }
        }
    }
}