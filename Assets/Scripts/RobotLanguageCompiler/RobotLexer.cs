using System.Collections.Generic;
using System.Text;

namespace RobotLanguageCompiler.Robot
{
    public enum RobotTokenType
    {
        // Ключевые слова
        PtpPoint,       // ptp_point
        LinPoint,       // lin_point
        Wait,           // wait
        OpenEffector,   // open_effector
        CloseEffector,  // close_effector

        // Разделители
        LeftParen,      // (
        RightParen,     // )
        LeftBrace,      // {
        RightBrace,     // }

        // Прочее
        Identifier,     // имя подпрограммы или имя точки
        Number,         // число (для wait)
        Error
    }

    public class RobotToken
    {
        public RobotTokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public RobotToken(RobotTokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}({Value}) at {Line}:{Column}";
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

        public List<RobotToken> Tokenize()
        {
            var tokens = new List<RobotToken>();
            RobotToken token = GetNextToken();

            while (token != null && token.Type != RobotTokenType.Error)
            {
                tokens.Add(token);
                token = GetNextToken();
            }

            return tokens;
        }

        private RobotToken GetNextToken()
        {
            SkipWhitespace();

            if (_position >= _source.Length)
            {
                return null;
            }

            char current = _source[_position];

            // Числа
            if (char.IsDigit(current))
            {
                return ReadNumber();
            }

            // Идентификаторы и ключевые слова
            if (char.IsLetter(current) || current == '_')
            {
                return ReadIdentifierOrKeyword();
            }

            // Разделители
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
                    return CreateErrorToken($"Unexpected character '{current}'", _line, _column);
            }
        }

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

        private RobotToken ReadNumber()
        {
            int startLine = _line;
            int startColumn = _column;
            StringBuilder sb = new StringBuilder();

            while (_position < _source.Length && (char.IsDigit(_source[_position]) || _source[_position] == '.'))
            {
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            return new RobotToken(RobotTokenType.Number, sb.ToString(), startLine, startColumn);
        }

        private RobotToken ReadIdentifierOrKeyword()
        {
            int startLine = _line;
            int startColumn = _column;
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
                return new RobotToken(type, value, startLine, startColumn);
            }

            return new RobotToken(RobotTokenType.Identifier, value, startLine, startColumn);
        }

        private RobotToken CreateSingleCharToken(RobotTokenType type, char character)
        {
            int startLine = _line;
            int startColumn = _column;
            _position++;
            _column++;
            return new RobotToken(type, character.ToString(), startLine, startColumn);
        }

        private RobotToken CreateErrorToken(string message, int line, int column)
        {
            _errors.Add($"{message} at {line}:{column}");
            return new RobotToken(RobotTokenType.Error, message, line, column);
        }
    }
}