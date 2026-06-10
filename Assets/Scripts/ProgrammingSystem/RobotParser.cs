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

        /// <summary>
        /// Выполняет синтаксический анализ токенов и строит структуру данных программы робота.
        /// </summary>
        /// <param name="robotId">Идентификатор робота.</param>
        /// <returns>Объект RobotProgramData с разобранными подпрограммами.</returns>
        public RobotProgramData Parse(string robotId)
        {
            var data = new RobotProgramData(robotId);

            while (!IsAtEnd())
            {
                var token = Current();

                if (token.Type != RobotTokenType.Identifier && token.Type != RobotTokenType.String)
                {
                    if (token.Type == RobotTokenType.RightBrace)
                    {
                        AddError($"Неожиданная '}}'", token);
                        Consume();
                        continue;
                    }

                    AddError($"Ожидалось имя подпрограммы, получено {token.Type}", token);
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

                Consume();

                if (Current().Type != RobotTokenType.LeftBrace)
                {
                    AddError($"Ожидался '{{' после имени подпрограммы '{subroutineName}'", Current());
                    return null;
                }
                Consume();

                var subroutine = new RobotSubroutine(subroutineName);
                subroutine.Line = nameLine;
                subroutine.Column = nameColumn;

                while (!IsAtEnd() && Current().Type != RobotTokenType.RightBrace)
                {
                    var command = ParseCommand();
                    if (command != null)
                    {
                        subroutine.Commands.Add(command);
                    }
                    else if (Current().Type == RobotTokenType.Identifier)
                    {
                        AddError($"Ожидалась команда, получено '{Current().Value}'", Current());
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

                if (IsAtEnd())
                {
                    AddError($"Ожидалась '}}' для закрытия подпрограммы '{subroutineName}'", Current());
                    return null;
                }

                if (Current().Type != RobotTokenType.RightBrace)
                {
                    AddError($"Ожидалась '}}' для закрытия подпрограммы '{subroutineName}'", Current());
                    if (Current().Type != RobotTokenType.Identifier)
                    {
                        return null;
                    }
                }
                if (Current().Type == RobotTokenType.RightBrace)
                {
                    Consume();
                }

                data.Subroutines.Add(subroutine);
            }

            return data;
        }

        /// <summary>
        /// Разбирает отдельную команду в подпрограмме.
        /// </summary>
        /// <returns>Объект команды (RobotMoveCommand, RobotWaitCommand, RobotEffectorCommand) или null.</returns>
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
                    return new RobotEffectorCommand(false);

                case RobotTokenType.CloseEffector:
                    Consume();
                    return new RobotEffectorCommand(true);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Разбирает команду движения (ptp_point или lin_point).
        /// </summary>
        /// <param name="isPtp">true для PTP движения, false для линейного.</param>
        /// <returns>Объект RobotMoveCommand или null при ошибке.</returns>
        private RobotMoveCommand ParseMoveCommand(bool isPtp)
        {
            if (Current().Type != RobotTokenType.LeftParen)
            {
                AddError($"Ожидался '(' после {(isPtp ? "ptp_point" : "lin_point")}", Current());
                return null;
            }
            Consume();

            // Имя точки может быть идентификатором или строкой в кавычках
            if (Current().Type != RobotTokenType.Identifier && Current().Type != RobotTokenType.String)
            {
                AddError($"Ожидалось имя точки", Current());
                return null;
            }

            string pointName = Current().Value;
            Consume();

            if (Current().Type != RobotTokenType.RightParen)
            {
                AddError($"Ожидался ')' после имени точки", Current());
                return null;
            }
            Consume();

            return new RobotMoveCommand(isPtp, pointName);
        }

        /// <summary>
        /// Разбирает команду ожидания wait(секунды).
        /// </summary>
        /// <returns>Объект RobotWaitCommand или null при ошибке.</returns>
        private RobotWaitCommand ParseWaitCommand()
        {
            if (Current().Type != RobotTokenType.LeftParen)
            {
                AddError($"Ожидался '(' после wait", Current());
                return null;
            }
            Consume();

            if (Current().Type != RobotTokenType.Number)
            {
                AddError($"Ожидалось число в wait()", Current());
                return null;
            }

            if (!float.TryParse(Current().Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
            {
                AddError($"Неверный формат числа", Current());
                return null;
            }
            Consume();

            if (Current().Type != RobotTokenType.RightParen)
            {
                AddError($"Ожидался ')' после аргумента wait", Current());
                return null;
            }
            Consume();

            return new RobotWaitCommand(seconds);
        }

        /// <summary>
        /// Возвращает текущий токен.
        /// </summary>
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

        /// <summary>
        /// Продвигает позицию парсера.
        /// </summary>
        private void Consume()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        /// <summary>
        /// Проверяет, достигнут ли конец токенов.
        /// </summary>
        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
        }

        /// <summary>
        /// Добавляет сообщение об ошибке.
        /// </summary>
        private void AddError(string message, RobotToken token)
        {
            _errors.Add($"{message} на {token.Line}:{token.Column}");
        }
    }
}