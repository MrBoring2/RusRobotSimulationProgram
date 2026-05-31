using System;
using System.Collections.Generic;
using System.Text;

namespace RobotLanguageCompiler.PLC
{
    public class PLCParser
    {
        private readonly List<PLCToken> _tokens;
        private int _position;
        private readonly List<string> _errors;
        private bool logicSection = false;

        public PLCParser(List<PLCToken> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _errors = new List<string>();
        }

        public List<string> Errors => _errors;

        public PLCData Parse()
        {
            var data = new PLCData();

            while (!IsAtEnd())
            {
                var token = Current();

                switch (token.Type)
                {
                    case PLCTokenType.InitSection:
                        Consume();
                        ParseInitSection(data);
                        break;
                    case PLCTokenType.RobotsBlocksSection:
                        Consume();
                        ParseRobotsBlocksSection(data);
                        break;
                    case PLCTokenType.LogicSection:
                        Consume();
                        ParseLogicSection(data);
                        break;
                    default:
                        if (token.Type != PLCTokenType.Error)
                        {
                            AddError($"Unexpected token {token.Type}", token);
                        }
                        Consume();
                        break;
                }
            }

            return data;
        }

        private void ParseInitSection(PLCData data)
        {
            while (!IsAtEnd() && Current().Type != PLCTokenType.RobotsBlocksSection &&
                   Current().Type != PLCTokenType.LogicSection)
            {
                // Объявление переменной: int x = 5 или bool flag = true
                var varType = ParseVarType();
                if (varType == null)
                {
                    SkipToNextLine();
                    continue;
                }

                var varName = ExpectIdentifier();
                if (varName == null)
                {
                    SkipToNextLine();
                    continue;
                }

                if (Current().Type != PLCTokenType.Assign)
                {
                    AddError($"Expected '='", Current());
                    SkipToNextLine();
                    continue;
                }
                Consume(); // =

                var value = ParseValue(varType.Value);
                if (value == null)
                {
                    SkipToNextLine();
                    continue;
                }

                // Добавляем в InitBlockItems и Variables
                var initVar = new PLCInitVariable
                {
                    VarType = varType.Value,
                    VariableName = varName,
                    StartValue = value
                };
                data.InitBlockItems.Add(initVar);
                data.Variables.Add(new Variable(Guid.NewGuid().ToString(), varType.Value, varName));
            }
        }

        private void ParseRobotsBlocksSection(PLCData data)
        {
            while (!IsAtEnd() && Current().Type != PLCTokenType.LogicSection)
            {
                bool Error = false;
                var ErrStart = Current();
                string robotId = "";

                if (Current().Type == PLCTokenType.Robot)
                {
                    Consume(); // robot
                    robotId = ExpectIdentifier();
                    if (robotId == null) Error = true;

                    if (Current().Type == PLCTokenType.LeftBrace && !Error)
                    {
                        Consume(); // {
                    }
                    else if (!Error)
                    {
                        AddError($"Expected '{{'", Current());
                        Error = true;
                    }
                }
                else
                {
                    AddError($"Expected 'robot' keyword", Current());
                    Error = true;
                }

                if (Error)
                {
                    while (!IsAtEnd() && Current().Type != PLCTokenType.Robot && Current().Type != PLCTokenType.LogicSection)
                    {
                        Consume();
                    }
                    var ErrEnd = Current();

                    _errors.Add($"This block of code requires keyword 'robot' and robot name, starts at {ErrStart.Line}:{ErrStart.Column}, ends at {ErrEnd.Line}:{ErrEnd.Column}");
                    continue;
                }

                var robotBlock = new PLCRobotBlock(robotId);

                // Парсим содержимое блока робота
                while (!IsAtEnd() && Current().Type != PLCTokenType.RightBrace)
                {
                    if (Current().Type == PLCTokenType.If)
                    {
                        var condition = ParseCondition();
                        if (condition != null)
                        {
                            robotBlock.ConditionsList.Add(condition);
                        }
                    }
                    else
                    {
                        // Это команда вне условия
                        var command = ParseCommand();
                        if (command != null)
                        {
                            robotBlock.ConditionsList.Add(command);
                        }
                        else
                        {
                            AddError($"Unexpected token in ROBOTS_BLOCKS section {Current().Type}", Current());
                            Consume();
                        }
                    }
                }

                if (Current().Type != PLCTokenType.RightBrace)
                {
                    AddError($"Expected '}}'", Current());
                    return;
                }
                Consume(); // }

                data.RobotCommandsBlockItems.Add(robotBlock);
            }
        }

        private void ParseLogicSection(PLCData data)
        {
            logicSection = true;
            while (!IsAtEnd())
            {
                if (Current().Type == PLCTokenType.If)
                {
                    var condition = ParseCondition();
                    if (condition != null)
                    {
                        data.LogicBlockItems.Add(condition);
                    }
                }
                else if (Current().Type == PLCTokenType.Identifier)
                {
                    // Это команда вне условия
                    var command = ParseCommand();
                    if (command != null)
                    {
                        data.LogicBlockItems.Add(command);
                    }
                }
                else
                {
                    AddError($"Unexpected token in LOGIC section: {Current().Type}", Current());
                    Consume();
                }
            }
        }

        private PLCBlockCondition ParseCondition()
        {
            Consume(); // if

            var expression = ParseExpressionInParens();
            if (expression == null) return null;

            var ifCondition = new PLCCondition(ConditionType.If, expression);
            var blockCondition = new PLCBlockCondition(ifCondition);

            if (Current().Type != PLCTokenType.LeftBrace)
            {
                AddError($"Expected '{{'", Current());
                return null;
            }
            Consume(); // {

            ParseConditionContent(ifCondition.Content);

            if (Current().Type != PLCTokenType.RightBrace)
            {
                AddError($"Expected '}}'", Current());
                return null;
            }
            Consume(); // }

            // Парсим elif
            while (!IsAtEnd() && Current().Type == PLCTokenType.Elif)
            {
                Consume(); // elif

                var elifExpression = ParseExpressionInParens();
                if (elifExpression == null) return blockCondition;

                var elifCondition = new PLCCondition(ConditionType.ElseIf, elifExpression);
                blockCondition.ElifConditions.Add(elifCondition);

                if (Current().Type != PLCTokenType.LeftBrace)
                {
                    AddError($"Expected '{{'", Current());
                    return blockCondition;
                }
                Consume(); // {

                ParseConditionContent(elifCondition.Content);

                if (Current().Type != PLCTokenType.RightBrace)
                {
                    AddError($"Expected '}}'", Current());
                    return blockCondition;
                }
                Consume(); // }
            }

            // Парсим else
            if (!IsAtEnd() && Current().Type == PLCTokenType.Else)
            {
                Consume(); // else

                if (Current().Type != PLCTokenType.LeftBrace)
                {
                    AddError($"Expected '{{'", Current());
                    return blockCondition;
                }
                Consume(); // {

                ParseConditionContent(blockCondition.ElseCondition.Content);

                if (Current().Type != PLCTokenType.RightBrace)
                {
                    AddError($"Expected '}}'", Current());
                    return blockCondition;
                }
                Consume(); // }
            }

            return blockCondition;
        }

        private void ParseConditionContent(List<PLCBase> content)
        {
            while (!IsAtEnd() && Current().Type != PLCTokenType.RightBrace)
            {
                var command = ParseCommand();
                if (command != null)
                {
                    content.Add(command);
                }
                else
                {
                    Consume();
                }
            }
        }

        private PLCCommand ParseCommand()
        {
            var token = Current();

            switch (token.Type)
            {
                case PLCTokenType.StartProgram:
                    if (logicSection)
                    {
                        AddError($"Unexpected command 'start_program'", Current());
                        break;
                    }
                    Consume();
                    if (Current().Type != PLCTokenType.LeftParen)
                    {
                        AddError($"Expected '('", Current());
                        break;
                    }
                    Consume(); // (

                    var programName = ExpectIdentifier();
                    if (programName == null) break;

                    if (Current().Type != PLCTokenType.RightParen)
                    {
                        AddError($"Expected ')'", Current());
                        break;
                    }
                    Consume(); // )

                    return new PLCStartProgram { ProgramName = programName };

                case PLCTokenType.Identifier:
                    var varName = token.Value;
                    Consume();

                    if (Current().Type == PLCTokenType.Increment)
                    {
                        Consume();
                        return new PLCIncrement { VariableName = varName };
                    }
                    else if (Current().Type == PLCTokenType.Decrement)
                    {
                        Consume();
                        return new PLCDecrement { VariableName = varName };
                    }
                    else if (Current().Type == PLCTokenType.Assign)
                    {
                        Consume();
                        var value = ParseSimpleValue();
                        if (value == null) return null;
                        return new PLCSetVariable { VariableName = varName, Value = value };
                    }
                    break;
            }

            return null;
        }

        private string ParseExpressionInParens()
        {
            if (Current().Type != PLCTokenType.LeftParen)
            {
                AddError($"Expected '('", Current());
            }
            else
            {
                Consume(); // (
            }

            var expression = ParseExpressionContent();

            if (Current().Type != PLCTokenType.RightParen)
            {
                AddError($"Expected ')'", Current());
            }
            else
            {
                Consume(); // )
            }

            return expression;
        }

        private string ParseExpressionContent()
        {
            List<PLCTokenType> types = new List<PLCTokenType> { PLCTokenType.StartProgram, PLCTokenType.If, PLCTokenType.Elif, PLCTokenType.Else, 
                PLCTokenType.LeftBrace, PLCTokenType.RightBrace, PLCTokenType.LogicSection };

            var expressionTokens = new List<PLCToken>();
            int parenCount = 1;

            while (!IsAtEnd() && parenCount > 0)
            {
                var token = Current();

                if (token.Type == PLCTokenType.LeftParen)
                {
                    parenCount++;
                }
                else if (token.Type == PLCTokenType.RightParen)
                {
                    parenCount--;
                    if (parenCount == 0) break;
                }
                else if (types.Contains(token.Type))
                {
                    return null;
                }

                expressionTokens.Add(token);
                Consume();
            }

            if (expressionTokens.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < expressionTokens.Count; i++)
            {
                sb.Append(expressionTokens[i].Value);
                if (i < expressionTokens.Count - 1)
                {
                    sb.Append(' ');
                }
            }

            return sb.ToString().Trim();
        }

        private string ParseSimpleValue()
        {
            var token = Current();
            if (token.Type == PLCTokenType.Number || token.Type == PLCTokenType.Identifier ||
                token.Type == PLCTokenType.True || token.Type == PLCTokenType.False)
            {
                var value = token.Value;
                Consume();
                return value;
            }

            AddError($"Expected value", token);
            return null;
        }

        private string ParseValue(VarType expectedType)
        {
            var token = Current();
            if (token.Type == PLCTokenType.Number)
            {
                var value = token.Value;
                Consume();
                return value;
            }
            else if (token.Type == PLCTokenType.True || token.Type == PLCTokenType.False)
            {
                var value = token.Value;
                Consume();
                return value;
            }

            AddError($"Expected value of type {expectedType}", token);
            return null;
        }

        private VarType? ParseVarType()
        {
            var token = Current();
            if (token.Type == PLCTokenType.Int)
            {
                Consume();
                return VarType.Int;
            }
            else if (token.Type == PLCTokenType.Bool)
            {
                Consume();
                return VarType.Bool;
            }

            AddError($"Expected type 'int' or 'bool'", token);
            return null;
        }

        private string ExpectIdentifier()
        {
            var token = Current();
            if (token.Type == PLCTokenType.Identifier)
            {
                var value = token.Value;
                Consume();
                return value;
            }

            AddError($"Expected identifier", token);
            return null;
        }

        private PLCToken Current()
        {
            if (!IsAtEnd())
            {
                return _tokens[_position];
            }
            else
            {
                return _tokens[_position - 1];
            }
        }

        private void Consume()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        private void SkipToNextLine()
        {
            var line = Current().Line;
            while (Current().Line == line)
            {
                Consume();
            }
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
        }

        private void AddError(string message, PLCToken token)
        {
            _errors.Add($"{message} at {token.Line}:{token.Column}");
        }
    }
}