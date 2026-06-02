using System.Collections.Generic;
using System.Globalization;

namespace RobotLanguageCompiler.Robot
{
    public class RobotParser
    {
        private readonly List<RobotToken> _tokens;
        private List<string> subroutineNames = new List<string>();
        private int _position;
        private readonly List<string> _errors;

        public RobotParser(List<RobotToken> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _errors = new List<string>();
        }

        public List<string> Errors => _errors;

        public RobotProgramData Parse(string robotId)
        {
            var data = new RobotProgramData(robotId);

            while (!IsAtEnd())
            {
                var token = Current();

                // Ожидаем идентификатор (имя подпрограммы)
                if (token.Type != RobotTokenType.Identifier)
                {
                    if (token.Type == RobotTokenType.RightBrace)
                    {
                        AddError($"Unexpected '}}'", token);
                        Consume();
                        continue;
                    }

                    AddError($"Expected subroutine name, got {token.Type}", token);
                    Consume();
                    continue;
                }

                string subroutineName = token.Value;
                int nameLine = token.Line;
                int nameColumn = token.Column;

                if (subroutineNames.Contains(subroutineName))
                {
                    AddError($"Программа с именем '{subroutineName}' уже существует", Current());
                    return null;
                }
                else
                {
                    subroutineNames.Add(subroutineName);
                }

                Consume(); // имя подпрограммы

                // Ожидаем {
                if (Current().Type != RobotTokenType.LeftBrace)
                {
                    AddError($"Expected '{{' after subroutine name '{subroutineName}'", Current());
                    return null;
                }
                Consume(); // {

                var subroutine = new RobotSubroutine(subroutineName);
                subroutine.Line = nameLine;
                subroutine.Column = nameColumn;

                // Парсим команды внутри подпрограммы
                while (!IsAtEnd() && Current().Type != RobotTokenType.RightBrace)
                {
                    var command = ParseCommand();
                    if (command != null)
                    {
                        subroutine.Commands.Add(command);
                    }
                    else if (Current().Type == RobotTokenType.Identifier)
                    {
                        AddError($"Expected command, got '{Current().Value}'", Current());
                        var line = Current().Line;
                        while (Current().Line == line)
                        {
                            Consume();
                        }
                    }
                    else
                    {
                        Consume();
                    }
                }

                // Проверяем, есть ли закрывающая скобка
                if (IsAtEnd())
                {
                    AddError($"Expected '}}' to close subroutine '{subroutineName}'", Current());
                    return null;
                }

                if (Current().Type != RobotTokenType.RightBrace)
                {
                    AddError($"Expected '}}' to close subroutine '{subroutineName}'", Current());
                    if (Current().Type != RobotTokenType.Identifier)
                    {
                        return null;
                    }
                }
                if (Current().Type == RobotTokenType.RightBrace)
                {
                    Consume(); // }
                }

                data.Subroutines.Add(subroutine);
            }

            return data;
        }

        private object ParseCommand()
        {
            var token = Current();

            switch (token.Type)
            {
                case RobotTokenType.PtpPoint:
                    Consume();
                    return ParseMoveCommand(true);

                case RobotTokenType.LinPoint:
                    Consume();
                    return ParseMoveCommand(false);

                case RobotTokenType.Wait:
                    Consume();
                    return ParseWaitCommand();

                case RobotTokenType.OpenEffector:
                    Consume();
                    return new RobotEffectorCommand(false); // open = false

                case RobotTokenType.CloseEffector:
                    Consume();
                    return new RobotEffectorCommand(true); // close = true

                default:
                    // Не возвращаем ошибку здесь, так как это может быть просто конец блока
                    return null;
            }
        }

        private RobotMoveCommand ParseMoveCommand(bool isPtp)
        {
            if (Current().Type != RobotTokenType.LeftParen)
            {
                AddError($"Expected '(' after {(isPtp ? "ptp_point" : "lin_point")}", Current());
                return null;
            }
            Consume(); // (

            if (Current().Type != RobotTokenType.Identifier)
            {
                AddError($"Expected point name", Current());
                return null;
            }

            string pointName = Current().Value;
            Consume(); // имя точки

            if (Current().Type != RobotTokenType.RightParen)
            {
                AddError($"Expected ')' after point name", Current());
                return null;
            }
            Consume(); // )

            return new RobotMoveCommand(isPtp, pointName);
        }

        private RobotWaitCommand ParseWaitCommand()
        {
            if (Current().Type != RobotTokenType.LeftParen)
            {
                AddError($"Expected '(' after wait", Current());
                return null;
            }
            Consume(); // (

            if (Current().Type != RobotTokenType.Number)
            {
                AddError($"Expected number in wait()", Current());
                return null;
            }

            if (!float.TryParse(Current().Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
            {
                AddError($"Invalid number format", Current());
                return null;
            }
            Consume(); // число

            if (Current().Type != RobotTokenType.RightParen)
            {
                AddError($"Expected ')' after wait argument", Current());
                return null;
            }
            Consume(); // )

            return new RobotWaitCommand(seconds);
        }

        private RobotToken Current()
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

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
        }

        private void AddError(string message, RobotToken token)
        {
            _errors.Add($"{message} at {token.Line}:{token.Column}");
        }
    }
}