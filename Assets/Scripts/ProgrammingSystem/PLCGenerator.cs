using Assets.Scripts.Models;
using System.Text;
using Unity.VisualScripting;

namespace RobotLanguageCompiler.PLC
{
    public class PLCGenerator
    {
        private const string Indent = "\t"; // tab

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
                    if (item is PLCBlockCondition condition)
                    {
                        GenerateCondition(sb, condition, Indent);
                    }
                    else if (item is PLCCommand command)
                    {
                        GenerateCommands(sb, command, Indent);
                    }
                }

                sb.AppendLine($"}}");
                sb.AppendLine();
            }
            sb.AppendLine();

            // Секция #LOGIC
            sb.AppendLine("#LOGIC");
            foreach (var item in data.LogicBlockItems)
            {
                if (item is PLCBlockCondition blockCond)
                {
                    GenerateCondition(sb, blockCond, "");
                }
                else if (item is PLCCommand command)
                {
                    GenerateCommands(sb, command, "");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private void GenerateCondition(StringBuilder sb, PLCBlockCondition blockCond, string indent)
        {
            // if условие с пробелами
            sb.AppendLine($"{indent}if ({blockCond.IfCondition.Expression}) {{");
            foreach (PLCCommand command in blockCond.IfCondition.Content)
            {
                GenerateCommands(sb, command, indent + Indent);
            }
            sb.AppendLine($"{indent}}}");

            // elif
            foreach (var elif in blockCond.ElifConditions)
            {
                sb.AppendLine($"{indent}elif ({elif.Expression}) {{");
                foreach (PLCCommand command in elif.Content)
                {
                    GenerateCommands(sb, command, indent + Indent);
                }
                sb.AppendLine($"{indent}}}");
            }

            // else
            if (blockCond.ElseCondition != null && blockCond.ElseCondition.Content.Count > 0)
            {
                sb.AppendLine($"{indent}else {{");
                foreach (PLCCommand command in blockCond.ElseCondition.Content)
                {
                    GenerateCommands(sb, command, indent + Indent);
                }
                sb.AppendLine($"{indent}}}");
            }
        }

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