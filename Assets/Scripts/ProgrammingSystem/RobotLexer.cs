using System.Collections.Generic;
using System.Text;

namespace RobotLanguageCompiler.Robot
{
    public enum RobotTokenType
    {
        PtpPoint,
        LinPoint,
        Wait,
        OpenEffector,
        CloseEffector,
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        Identifier,
        Number,
        Error
    }

    public class RobotToken
    {
        public RobotTokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }
        public int Position { get; }

        public RobotToken(RobotTokenType type, string value, int line, int column, int position)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
            Position = position;
        }

        public override string ToString()
        {
            return $"{Type}({Value}) на {Line}:{Column}";
        }
    }

    public class RobotLexer
    {
        private readonly string _source;
        private int _position;
        private int _line;
        private int _column;
        private readonly List<string> _errors;

        private readonly Dictionary<string, RobotTokenType> _keywords = new()
        {
            { "ptp_point", RobotTokenType.PtpPoint },
            { "lin_point", RobotTokenType.LinPoint },
            { "wait", RobotTokenType.Wait },
            { "open_effector", RobotTokenType.OpenEffector },
            { "close_effector", RobotTokenType.CloseEffector }
        };

        public RobotLexer(string source)
        {
            _source = source;
            _position = 0;
            _line = 1;
            _column = 1;
            _errors = new List<string>();
        }

        public List<string> Errors => _errors;

        /// <summary>
        /// Выполняет лексический анализ исходного кода и возвращает список токенов.
        /// </summary>
        /// <returns>Список токенов RobotToken.</returns>
        public List<RobotToken> Tokenize()
        {
            var tokens = new List<RobotToken>();
            RobotToken token = GetNextToken();

            while (token != null)
            {
                tokens.Add(token);
                token = GetNextToken();
            }

            return tokens;
        }

        /// <summary>
        /// Извлекает следующий токен из исходного кода.
        /// </summary>
        /// <returns>Объект RobotToken или null, если достигнут конец файла.</returns>
        private RobotToken GetNextToken()
        {
            SkipWhitespace();

            if (_position >= _source.Length)
            {
                return null;
            }

            char current = _source[_position];

            if (char.IsDigit(current))
            {
                return ReadNumber();
            }

            if (char.IsLetter(current) || current == '_')
            {
                return ReadIdentifierOrKeyword();
            }

            switch (current)
            {
                case '(':
                    return CreateSingleCharToken(RobotTokenType.LeftParen, '(');
                case ')':
                    return CreateSingleCharToken(RobotTokenType.RightParen, ')');
                case '{':
                    return CreateSingleCharToken(RobotTokenType.LeftBrace, '{');
                case '}':
                    return CreateSingleCharToken(RobotTokenType.RightBrace, '}');
                default:
                    return CreateErrorToken($"Неожиданный символ '{current}'", current.ToString(), _line, _column, _position);
            }
        }

        /// <summary>
        /// Пропускает пробельные символы (пробелы, табуляции, переводы строк).
        /// </summary>
        private void SkipWhitespace()
        {
            while (_position < _source.Length)
            {
                char c = _source[_position];
                if (c == ' ' || c == '\t')
                {
                    _position++;
                    _column++;
                }
                else if (c == '\r')
                {
                    _position++;
                    _column++;
                }
                else if (c == '\n')
                {
                    _position++;
                    _line++;
                    _column = 1;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Считывает числовой токен.
        /// </summary>
        /// <returns>Токен с типом Number.</returns>
        private RobotToken ReadNumber()
        {
            int startLine = _line;
            int startColumn = _column;
            int startPosition = _position;
            StringBuilder sb = new StringBuilder();

            while (_position < _source.Length && (char.IsDigit(_source[_position]) || _source[_position] == '.'))
            {
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            return new RobotToken(RobotTokenType.Number, sb.ToString(), startLine, startColumn, startPosition);
        }

        /// <summary>
        /// Считывает идентификатор или ключевое слово.
        /// </summary>
        /// <returns>Токен соответствующего типа.</returns>
        private RobotToken ReadIdentifierOrKeyword()
        {
            int startLine = _line;
            int startColumn = _column;
            int startPosition = _position;
            StringBuilder sb = new StringBuilder();

            while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
            {
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            string value = sb.ToString();

            if (_keywords.TryGetValue(value, out RobotTokenType type))
            {
                return new RobotToken(type, value, startLine, startColumn, startPosition);
            }

            return new RobotToken(RobotTokenType.Identifier, value, startLine, startColumn, startPosition);
        }

        /// <summary>
        /// Создает токен из одного символа.
        /// </summary>
        /// <param name="type">Тип токена.</param>
        /// <param name="character">Символ.</param>
        /// <returns>Созданный токен.</returns>
        private RobotToken CreateSingleCharToken(RobotTokenType type, char character)
        {
            int startLine = _line;
            int startColumn = _column;
            int startPosition = _position;
            _position++;
            _column++;
            return new RobotToken(type, character.ToString(), startLine, startColumn, startPosition);
        }

        /// <summary>
        /// Создает токен ошибки.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        /// <param name="line">Строка ошибки.</param>
        /// <param name="column">Колонка ошибки.</param>
        /// <returns>Токен с типом Error.</returns>
        private RobotToken CreateErrorToken(string message, string value, int line, int column, int position)
        {
            _position++;
            _column++;
            _errors.Add($"{message} на {line}:{column}");
            return new RobotToken(RobotTokenType.Error, value, line, column, position);
        }
    }
}