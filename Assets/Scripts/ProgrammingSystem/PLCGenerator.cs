using Assets.Scripts.Models;
using System.Text;

namespace RobotLanguageCompiler.PLC
{
    public class PLCGenerator
    {
        private const string Indent = "\t";

        /// <summary>
        /// Генерирует исходный код на языке PLC из внутренней структуры данных.
        /// </summary>
        /// <param name="data">Объект PLCData, содержащий переменные, блоки роботов и логику.</param>
        /// <returns>Строка с сгенерированным кодом PLC.</returns>
        public string Generate(PLCData data)
        {
            var sb = new StringBuilder();

            // Секция #INIT
            sb.AppendLine("#INIT");
            foreach (var initVar in data.InitBlockItems)
            {
                string typeStr = initVar.VarType == VarType.Int ? "int" : "bool";
                sb.AppendLine($"{typeStr} {initVar.VariableName} = {initVar.StartValue}");
            }
            sb.AppendLine();

            // Секция #ROBOTS_BLOCKS
            sb.AppendLine("#ROBOTS_BLOCKS");
            foreach (var robotBlock in data.RobotCommandsBlockItems)
            {
                sb.AppendLine($"robot {robotBlock.RobotId} {{");

                foreach (var item in robotBlock.ConditionsList)
                {
                    GenerateConditionOrCommand(sb, item, Indent);
                }

                sb.AppendLine($"}}");
                sb.AppendLine();
            }
            if (data.RobotCommandsBlockItems.Count == 0)
            {
                sb.AppendLine();
            }

            // Секция #LOGIC
            sb.AppendLine("#LOGIC");
            foreach (var item in data.LogicBlockItems)
            {
                GenerateConditionOrCommand(sb, item, "");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Рекурсивно генерирует код для условия или команды.
        /// </summary>
        /// <param name="sb">StringBuilder для накопления кода.</param>
        /// <param name="item">Элемент (условие или команда) для генерации.</param>
        /// <param name="indent">Отступ для форматирования.</param>
        private void GenerateConditionOrCommand(StringBuilder sb, PLCBase item, string indent)
        {
            if (item is PLCBlockCondition condition)
            {
                GenerateCondition(sb, condition, indent);
            }
            else if (item is PLCCommand command)
            {
                GenerateCommands(sb, command, indent);
            }
        }

        /// <summary>
        /// Рекурсивно генерирует код условной конструкции if-elif-else с поддержкой вложенности.
        /// </summary>
        /// <param name="sb">StringBuilder для накопления кода.</param>
        /// <param name="blockCond">Блок условия для генерации.</param>
        /// <param name="indent">Отступ для форматирования.</param>
        private void GenerateCondition(StringBuilder sb, PLCBlockCondition blockCond, string indent)
        {
            // Генерация if
            sb.AppendLine($"{indent}if ({blockCond.IfCondition.Expression}) {{");

            // Рекурсивная генерация содержимого блока if
            foreach (var item in blockCond.IfCondition.Content)
            {
                GenerateConditionOrCommand(sb, item, indent + Indent);
            }
            sb.AppendLine($"{indent}}}");

            // Генерация elif
            foreach (var elif in blockCond.ElifConditions)
            {
                sb.AppendLine($"{indent}else if ({elif.Expression}) {{");

                // Рекурсивная генерация содержимого блока elif
                foreach (var item in elif.Content)
                {
                    GenerateConditionOrCommand(sb, item, indent + Indent);
                }
                sb.AppendLine($"{indent}}}");
            }

            // Генерация else
            if (blockCond.ElseCondition != null && blockCond.ElseCondition.Content.Count > 0)
            {
                sb.AppendLine($"{indent}else {{");

                // Рекурсивная генерация содержимого блока else
                foreach (var item in blockCond.ElseCondition.Content)
                {
                    GenerateConditionOrCommand(sb, item, indent + Indent);
                }
                sb.AppendLine($"{indent}}}");
            }
        }

        /// <summary>
        /// Генерирует код отдельной команды (start_program, присваивание, инкремент, декремент).
        /// </summary>
        /// <param name="sb">StringBuilder для накопления кода.</param>
        /// <param name="command">Команда для генерации.</param>
        /// <param name="indent">Отступ для форматирования.</param>
        private void GenerateCommands(StringBuilder sb, PLCCommand command, string indent)
        {
            switch (command)
            {
                case PLCStartProgram start:
                    sb.AppendLine($"{indent}start_program({start.ProgramName})");
                    break;
                case PLCSetVariable set:
                    switch (set.Operation)
                    {
                        case OperationType.Assign:
                            sb.AppendLine($"{indent}{set.VariableName} = {set.Value}");
                            break;
                        case OperationType.Increment:
                            sb.AppendLine($"{indent}{set.VariableName} += {set.Value}");
                            break;
                        case OperationType.Decrement:
                            sb.AppendLine($"{indent}{set.VariableName} -= {set.Value}");
                            break;
                    }
                    break;
            }
        }
    }
}