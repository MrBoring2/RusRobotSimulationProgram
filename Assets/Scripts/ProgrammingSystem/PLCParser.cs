using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RobotLanguageCompiler.PLC
{
    public class PLCParser
    {
        private readonly List<PLCToken> _tokens;
        private List<string> initVariables = new List<string>();
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

        /// <summary>
        /// Выполняет синтаксический анализ всех токенов и строит структуру данных PLC.
        /// </summary>
        /// <returns>Объект PLCData, содержащий все переменные, блоки роботов и логику.</returns>
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
                            AddError($"Неожиданный токен {token.Type}", token);
                        }
                        Consume();
                        break;
                }
            }

            return data;
        }

        /// <summary>
        /// Разбирает секцию #INIT, объявляет переменные и их начальные значения.
        /// </summary>
        /// <param name="data">Объект PLCData для заполнения.</param>
        private void ParseInitSection(PLCData data)
        {
            while (!IsAtEnd() && Current().Type != PLCTokenType.RobotsBlocksSection &&
                   Current().Type != PLCTokenType.LogicSection)
            {
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

                if (initVariables.Contains(varName))
                {
                    AddError($"Переменная с именем '{varName}' уже существует", Previous());
                    SkipToNextLine();
                    continue;
                }

                if (Current().Type != PLCTokenType.Assign)
                {
                    AddError($"Ожидался '='", Current());
                    SkipToNextLine();
                    continue;
                }
                Consume();

                var value = ParseValue(varType.Value);
                if (value == null)
                {
                    SkipToNextLine();
                    continue;
                }

                initVariables.Add(varName);

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

        /// <summary>
        /// Разбирает секцию #ROBOTS_BLOCKS, создавая блоки для каждого робота с командами и условиями.
        /// </summary>
        /// <param name="data">Объект PLCData для заполнения.</param>
        private void ParseRobotsBlocksSection(PLCData data)
        {
            List<String> robotIds = new List<String>();

            while (!IsAtEnd() && Current().Type != PLCTokenType.LogicSection)
            {
                bool Error = false;
                var ErrStart = Current();
                string robotId = "";

                if (Current().Type == PLCTokenType.Robot)
                {
                    Consume();
                    robotId = ExpectIdentifier();
                    if (robotId == null) Error = true;

                    if (!Error)
                    {
                        if (robotIds.Contains(robotId))
                        {
                            while (!IsAtEnd() && Current().Type != PLCTokenType.Robot && Current().Type != PLCTokenType.LogicSection)
                            {
                                Consume();
                            }
                            _errors.Add($"Имя робота должно быть уникальным, блок для робота '{robotId}' уже существует");
                            continue;
                        }
                        else robotIds.Add(robotId);
                    }

                    if (Current().Type == PLCTokenType.LeftBrace && !Error)
                    {
                        Consume();
                    }
                    else if (!Error)
                    {
                        AddError($"Ожидался '{{'", Current());
                        Error = true;
                    }
                }
                else
                {
                    AddError($"Ожидалось ключевое слово 'robot'", Current());
                    Error = true;
                }

                if (Error)
                {
                    while (!IsAtEnd() && Current().Type != PLCTokenType.Robot && Current().Type != PLCTokenType.LogicSection)
                    {
                        Consume();
                    }
                    var ErrEnd = Current();

                    _errors.Add($"Этот блок кода требует ключевое слово 'robot' и имя робота, начинается на {ErrStart.Line}:{ErrStart.Column}, заканчивается на {ErrEnd.Line}:{ErrEnd.Column}");
                    continue;
                }

                var robotBlock = new PLCRobotBlock(robotId);

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
                        var command = ParseCommand();
                        if (command != null)
                        {
                            robotBlock.ConditionsList.Add(command);
                        }
                        else
                        {
                            AddError($"Неожиданный токен в секции ROBOTS_BLOCKS {Current().Type}", Current());
                            Consume();
                        }
                    }
                }

                if (Current().Type != PLCTokenType.RightBrace)
                {
                    AddError($"Ожидался '}}'", Current());
                    return;
                }
                Consume();

                data.RobotCommandsBlockItems.Add(robotBlock);
            }
        }

        /// <summary>
        /// Разбирает секцию #LOGIC, содержащую условия и команды верхнего уровня.
        /// </summary>
        /// <param name="data">Объект PLCData для заполнения.</param>
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
                    var command = ParseCommand();
                    if (command != null)
                    {
                        data.LogicBlockItems.Add(command);
                    }
                }
                else
                {
                    AddError($"Неожиданный токен в секции LOGIC: {Current().Type}", Current());
                    Consume();
                }
            }
        }

        /// <summary>
        /// Разбирает условную конструкцию if-elif-else и возвращает блок условия.
        /// </summary>
        /// <returns>Объект PLCBlockCondition или null при ошибке.</returns>
        private PLCBlockCondition ParseCondition()
        {
            int ifLine = Current().Line;
            int ifColumn = Current().Column;
            Consume(); // if

            var expression = ParseExpressionInParens();
            if (expression == null) return null;

            var ifCondition = new PLCCondition(ConditionType.If, expression);
            var blockCondition = new PLCBlockCondition(ifCondition);

            if (Current().Type != PLCTokenType.LeftBrace)
            {
                AddError($"Ожидался '{{' после if", Current());
                return null;
            }
            Consume(); // {

            ParseConditionContent(ifCondition.Content);

            if (Current().Type != PLCTokenType.RightBrace)
            {
                AddError($"Ожидался '}}' для закрытия блока if, начатого на {ifLine}:{ifColumn}", Current());
                return null;
            }
            Consume(); // }

            // Парсим elif
            while (!IsAtEnd() && Current().Type == PLCTokenType.Elif)
            {
                int elifLine = Current().Line;
                int elifColumn = Current().Column;
                Consume(); // elif

                var elifExpression = ParseExpressionInParens();
                if (elifExpression == null) return blockCondition;

                var elifCondition = new PLCCondition(ConditionType.ElseIf, elifExpression);
                blockCondition.ElifConditions.Add(elifCondition);

                if (Current().Type != PLCTokenType.LeftBrace)
                {
                    AddError($"Ожидался '{{' после elif", Current());
                    return blockCondition;
                }
                Consume(); // {

                ParseConditionContent(elifCondition.Content);

                if (Current().Type != PLCTokenType.RightBrace)
                {
                    AddError($"Ожидался '}}' для закрытия блока elif, начатого на {elifLine}:{elifColumn}", Current());
                    return blockCondition;
                }
                Consume(); // }
            }

            // Парсим else
            if (!IsAtEnd() && Current().Type == PLCTokenType.Else)
            {
                int elseLine = Current().Line;
                int elseColumn = Current().Column;
                Consume(); // else

                if (Current().Type != PLCTokenType.LeftBrace)
                {
                    AddError($"Ожидался '{{' после else", Current());
                    return blockCondition;
                }
                Consume(); // {

                ParseConditionContent(blockCondition.ElseCondition.Content);

                if (Current().Type != PLCTokenType.RightBrace)
                {
                    AddError($"Ожидался '}}' для закрытия блока else, начатого на {elseLine}:{elseColumn}", Current());
                    return blockCondition;
                }
                Consume(); // }
            }

            return blockCondition;
        }

        /// <summary>
        /// Разбирает содержимое блока условия.
        /// </summary>
        /// <param name="content">Список для добавления команд.</param>
        private void ParseConditionContent(List<PLCBase> content)
        {
            while (!IsAtEnd() && Current().Type != PLCTokenType.RightBrace)
            {
                // Проверяем, не является ли токен началом нового условия
                if (Current().Type == PLCTokenType.If)
                {
                    var nestedCondition = ParseCondition();
                    if (nestedCondition != null)
                    {
                        content.Add(nestedCondition);
                    }
                }
                else
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
        }

        /// <summary>
        /// Разбирает отдельную команду (start_program, присваивание, инкремент, декремент).
        /// </summary>
        /// <returns>Объект PLCCommand или null при ошибке.</returns>
        private PLCCommand ParseCommand()
        {
            var token = Current();

            switch (token.Type)
            {
                case PLCTokenType.StartProgram:
                    if (logicSection)
                    {
                        AddError($"Неожиданная команда 'start_program' в секции LOGIC", Current());
                        break;
                    }
                    Consume();
                    if (Current().Type != PLCTokenType.LeftParen)
                    {
                        AddError($"Ожидался '(' после start_program", Current());
                        break;
                    }
                    Consume();

                    var programName = ExpectIdentifier();
                    if (programName == null) break;

                    if (Current().Type != PLCTokenType.RightParen)
                    {
                        AddError($"Ожидался ')' после имени программы", Current());
                        break;
                    }
                    Consume();

                    return new PLCStartProgram { ProgramName = programName };

                case PLCTokenType.Identifier:
                    var varName = token.Value;

                    if (!initVariables.Contains(varName))
                    {
                        AddError($"Переменная '{varName}' должна быть объявлена в #INIT перед использованием", token);
                        SkipToNextLine();
                        return null;
                    }

                    Consume();

                    if (Current().Type == PLCTokenType.Increment)
                    {
                        Consume();
                        return new PLCSetVariable { Operation = OperationType.Increment, VariableName = varName, Value = "1" };
                    }
                    else if (Current().Type == PLCTokenType.Decrement)
                    {
                        Consume();
                        return new PLCSetVariable { Operation = OperationType.Decrement, VariableName = varName, Value = "1" };
                    }
                    else if (Current().Type == PLCTokenType.Assign)
                    {
                        Consume();
                        var value = ParseSimpleValue();
                        if (value == null) return null;
                        return new PLCSetVariable { Operation = OperationType.Assign, VariableName = varName, Value = value };
                    }
                    break;
            }

            return null;
        }

        /// <summary>
        /// Разбирает выражение внутри круглых скобок, например условие (x > 5).
        /// </summary>
        /// <returns>Строковое представление выражения или null при ошибке.</returns>
        private string ParseExpressionInParens()
        {
            if (Current().Type != PLCTokenType.LeftParen)
            {
                AddError($"Ожидался '('", Current());
            }
            else
            {
                Consume();
            }

            var expression = ParseExpressionContent();

            if (Current().Type != PLCTokenType.RightParen)
            {
                AddError($"Ожидался ')'", Current());
            }
            else
            {
                Consume();
            }

            return expression;
        }

        /// <summary>
        /// Разбирает содержимое выражения, собирая токены до закрывающей скобки.
        /// </summary>
        /// <returns>Строковое представление выражения.</returns>
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
                else if (token.Type == PLCTokenType.Identifier && !initVariables.Contains(token.Value))
                {
                    AddError($"Переменная '{token.Value}' должна быть объявлена в #INIT перед использованием", token);
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

        /// <summary>
        /// Разбирает простое значение (число, идентификатор, true/false).
        /// </summary>
        /// <returns>Строковое представление значения или null при ошибке.</returns>
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

            AddError($"Ожидалось значение", token);
            return null;
        }

        /// <summary>
        /// Разбирает значение с проверкой ожидаемого типа.
        /// </summary>
        /// <param name="expectedType">Ожидаемый тип переменной.</param>
        /// <returns>Строковое представление значения или null при ошибке.</returns>
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

            AddError($"Ожидалось значение типа {expectedType}", token);
            return null;
        }

        /// <summary>
        /// Разбирает тип переменной (int или bool).
        /// </summary>
        /// <returns>Тип VarType или null при ошибке.</returns>
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

            AddError($"Ожидался тип 'int' или 'bool'", token);
            return null;
        }

        /// <summary>
        /// Ожидает идентификатор и возвращает его значение.
        /// </summary>
        /// <returns>Имя идентификатора или null при ошибке.</returns>
        private string ExpectIdentifier()
        {
            var token = Current();
            if (token.Type == PLCTokenType.Identifier)
            {
                var value = token.Value;
                Consume();
                return value;
            }

            AddError($"Ожидался идентификатор", token);
            return null;
        }

        /// <summary>
        /// Возвращает текущий токен без продвижения позиции.
        /// </summary>
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

        /// <summary>
        /// Возвращает предыдущий токен.
        /// </summary>
        private PLCToken Previous()
        {
            return _tokens[_position - 1];
        }

        /// <summary>
        /// Продвигает позицию парсера на следующий токен.
        /// </summary>
        private void Consume()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        /// <summary>
        /// Пропускает все токены до конца текущей строки.
        /// </summary>
        private void SkipToNextLine()
        {
            var line = Current().Line;
            while (Current().Line == line && !IsAtEnd())
            {
                Consume();
            }
        }

        /// <summary>
        /// Проверяет, достигнут ли конец списка токенов.
        /// </summary>
        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
        }

        /// <summary>
        /// Добавляет сообщение об ошибке с указанием позиции токена.
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        /// <param name="token">Токен, на котором произошла ошибка.</param>
        private void AddError(string message, PLCToken token)
        {
            _errors.Add($"{message} на {token.Line}:{token.Column}");
        }
    }
}