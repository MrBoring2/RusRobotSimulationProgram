using System.Collections.Generic;
using System.Text;

namespace RobotLanguageCompiler.PLC
{
    public enum PLCTokenType
    {
        InitSection,
        RobotsBlocksSection,
        LogicSection,
        Robot,
        If,
        Elif,
        Else,
        StartProgram,
        Int,
        Bool,
        True,
        False,
        And,
        Or,
        Assign,
        Increment,
        Decrement,
        Equal,
        NotEqual,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual,
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        Identifier,
        Number,
        Error
    }

    public class PLCToken
    {
        public PLCTokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public PLCToken(PLCTokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}({Value}) на {Line}:{Column}";
        }
    }

    public class PLCLexer
    {
        private readonly string _source;
        private int _position;
        private int _line;
        private int _column;
        private readonly List<string> _errors;

        private readonly Dictionary<string, PLCTokenType> _keywords = new()
        {
            { "robot", PLCTokenType.Robot },
            { "if", PLCTokenType.If },
            { "elif", PLCTokenType.Elif },
            { "else", PLCTokenType.Else },
            { "start_program", PLCTokenType.StartProgram },
            { "int", PLCTokenType.Int },
            { "bool", PLCTokenType.Bool },
            { "true", PLCTokenType.True },
            { "false", PLCTokenType.False }
        };

        public PLCLexer(string source)
        {
            _source = source;
            _position = 0;
            _line = 1;
            _column = 1;
            _errors = new List<string>();
        }

        public List<string> Errors => _errors;

        /// <summary>
        /// Выполняет лексический анализ исходного кода PLC и возвращает список токенов.
        /// </summary>
        /// <returns>Список токенов PLCToken.</returns>
        public List<PLCToken> Tokenize()
        {
            var tokens = new List<PLCToken>();
            PLCToken token = GetNextToken();

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
        /// <returns>Объект PLCToken или null, если достигнут конец файла.</returns>
        private PLCToken GetNextToken()
        {
            SkipWhitespace();

            if (_position >= _source.Length)
            {
                return null;
            }

            char current = _source[_position];

            if (current == '#')
            {
                return ReadSection();
            }

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
                case '=':
                    if (Peek() == '=')
                    {
                        return ReadTwoCharToken(PLCTokenType.Equal, "==");
                    }
                    return CreateSingleCharToken(PLCTokenType.Assign, '=');
                case '!':
                    if (Peek() == '=')
                    {
                        return ReadTwoCharToken(PLCTokenType.NotEqual, "!=");
                    }
                    return CreateErrorToken($"Неожиданный символ '{current}'", current.ToString(), _line, _column);
                case '>':
                    if (Peek() == '=')
                    {
                        return ReadTwoCharToken(PLCTokenType.GreaterOrEqual, ">=");
                    }
                    return CreateSingleCharToken(PLCTokenType.Greater, '>');
                case '<':
                    if (Peek() == '=')
                    {
                        return ReadTwoCharToken(PLCTokenType.LessOrEqual, "<=");
                    }
                    return CreateSingleCharToken(PLCTokenType.Less, '<');
                case '+':
                    if (Peek() == '=')
                    {
                        return ReadTwoCharToken(PLCTokenType.Increment, "+=");
                    }
                    return CreateErrorToken($"Неожиданный символ '{current}'", current.ToString(), _line, _column);
                case '-':
                    if (Peek() == '=')
                    {
                        return ReadTwoCharToken(PLCTokenType.Decrement, "-=");
                    }
                    return CreateErrorToken($"Неожиданный символ '{current}'", current.ToString(), _line, _column);
                case '&':
                    if (Peek() == '&')
                    {
                        return ReadTwoCharToken(PLCTokenType.And, "&&");
                    }
                    return CreateErrorToken($"Неожиданный символ '{current}'", current.ToString(), _line, _column);

                case '|':
                    if (Peek() == '|')
                    {
                        return ReadTwoCharToken(PLCTokenType.Or, "||");
                    }
                    return CreateErrorToken($"Неожиданный символ '{current}'", current.ToString(), _line, _column);
                case '(':
                    return CreateSingleCharToken(PLCTokenType.LeftParen, '(');
                case ')':
                    return CreateSingleCharToken(PLCTokenType.RightParen, ')');
                case '{':
                    return CreateSingleCharToken(PLCTokenType.LeftBrace, '{');
                case '}':
                    return CreateSingleCharToken(PLCTokenType.RightBrace, '}');
                default:
                    return CreateErrorToken($"Неожиданный символ '{current}'", current.ToString(), _line, _column);
            }
        }

        /// <summary>
        /// Пропускает пробельные символы.
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
        /// Просматривает следующий символ без продвижения позиции.
        /// </summary>
        private char Peek()
        {
            if (_position + 1 >= _source.Length)
                return '\0';
            return _source[_position + 1];
        }

        /// <summary>
        /// Считывает секцию (начинается с #).
        /// </summary>
        private PLCToken ReadSection()
        {
            int startLine = _line;
            int startColumn = _column;
            _position++;
            _column++;

            StringBuilder sb = new StringBuilder();
            sb.Append('#');

            while (_position < _source.Length && (char.IsLetter(_source[_position]) || _source[_position] == '_'))
            {
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            string value = sb.ToString();

            PLCTokenType type = value switch
            {
                "#INIT" => PLCTokenType.InitSection,
                "#ROBOTS_BLOCKS" => PLCTokenType.RobotsBlocksSection,
                "#LOGIC" => PLCTokenType.LogicSection,
                _ => PLCTokenType.Error
            };

            if (type == PLCTokenType.Error)
            {
                return CreateErrorToken($"Неизвестная секция '{value}'", value, startLine, startColumn);
            }

            return new PLCToken(type, value, startLine, startColumn);
        }

        /// <summary>
        /// Считывает числовой токен.
        /// </summary>
        private PLCToken ReadNumber()
        {
            int startLine = _line;
            int startColumn = _column;
            StringBuilder sb = new StringBuilder();

            while (_position < _source.Length && char.IsDigit(_source[_position]))
            {
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            return new PLCToken(PLCTokenType.Number, sb.ToString(), startLine, startColumn);
        }

        /// <summary>
        /// Считывает идентификатор или ключевое слово.
        /// </summary>
        private PLCToken ReadIdentifierOrKeyword()
        {
            int startLine = _line;
            int startColumn = _column;
            StringBuilder sb = new StringBuilder();

            while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_' || _source[_position] == '-'))
            {
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            string value = sb.ToString();

            if (_keywords.TryGetValue(value, out PLCTokenType type))
            {
                return new PLCToken(type, value, startLine, startColumn);
            }

            return new PLCToken(PLCTokenType.Identifier, value, startLine, startColumn);
        }

        /// <summary>
        /// Считывает двухсимвольный токен (==, !=, >=, <=, +=, -=).
        /// </summary>
        private PLCToken ReadTwoCharToken(PLCTokenType type, string value)
        {
            int startLine = _line;
            int startColumn = _column;
            _position += 2;
            _column += 2;
            return new PLCToken(type, value, startLine, startColumn);
        }

        /// <summary>
        /// Создает односимвольный токен.
        /// </summary>
        private PLCToken CreateSingleCharToken(PLCTokenType type, char character)
        {
            int startLine = _line;
            int startColumn = _column;
            _position++;
            _column++;
            return new PLCToken(type, character.ToString(), startLine, startColumn);
        }

        /// <summary>
        /// Создает токен ошибки.
        /// </summary>
        private PLCToken CreateErrorToken(string message, string value, int line, int column)
        {
            _position++;
            _column++;
            _errors.Add($"{message} на {line}:{column}");
            return new PLCToken(PLCTokenType.Error, value, line, column);
        }
    }
}