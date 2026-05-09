using System.Collections.Generic;
using System.Text;

namespace RobotLanguageCompiler
{
    public class Lexer
    {
        private readonly string _source;
        private int _position;
        private int _line;
        private int _column;
        private readonly List<CompilationError> _errors;

        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            { "if", TokenType.If },
            { "elif", TokenType.Elif },
            { "else", TokenType.Else },
            { "while", TokenType.While },
            { "int", TokenType.Int },
            { "bool", TokenType.Bool },
            { "ptp_point", TokenType.PtpPoint },
            { "lin_point", TokenType.LinPoint },
            { "wait", TokenType.Wait },
            { "wait_for", TokenType.WaitFor },
            { "subprogram", TokenType.Subprogram },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "in", TokenType.In },
            { "out", TokenType.Out },
            { "and", TokenType.And },
            { "or", TokenType.Or },
            { "not", TokenType.Not }
        };

        public Lexer(string source)
        {
            _source = source;
            _position = 0;
            _line = 1;
            _column = 1;
            _errors = new List<CompilationError>();
        }

        public List<CompilationError> Errors => _errors;

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            Token token;

            do
            {
                token = GetNextToken();
                if (token.Type != TokenType.Comment && token.Type != TokenType.Newline)
                {
                    tokens.Add(token);
                }
                else if (token.Type == TokenType.Newline)
                {
                    tokens.Add(token);
                }
            } while (token.Type != TokenType.EndOfFile); //  && token.Type != TokenType.Error

            //if (token.Type == TokenType.Error)
            //{
            //    tokens.Add(token);
            //}

            return tokens;
        }

        private Token GetNextToken()
        {
            SkipWhitespace();

            if (_position >= _source.Length)
            {
                return CreateToken(TokenType.EndOfFile, "", _line, _column);
            }

            char current = _source[_position];

            // Комментарии
            if (current == '/' && _position + 1 < _source.Length && _source[_position + 1] == '/')
            {
                return ReadComment();
            }

            // Строковые литералы
            if (current == '"')
            {
                return ReadString();
            }

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

            // Операторы и разделители
            switch (current)
            {
                case '=':
                    if (_position + 1 < _source.Length && _source[_position + 1] == '=')
                    {
                        return ReadTwoCharToken(TokenType.Equal, "==");
                    }
                    return CreateSingleCharToken(TokenType.Assign, '=');

                case '!':
                    if (_position + 1 < _source.Length && _source[_position + 1] == '=')
                    {
                        return ReadTwoCharToken(TokenType.NotEqual, "!=");
                    }
                    return CreateErrorToken($"Unexpected character '!'", current.ToString());

                case '>':
                    if (_position + 1 < _source.Length && _source[_position + 1] == '=')
                    {
                        return ReadTwoCharToken(TokenType.GreaterOrEqual, ">=");
                    }
                    return CreateSingleCharToken(TokenType.Greater, '>');

                case '<':
                    if (_position + 1 < _source.Length && _source[_position + 1] == '=')
                    {
                        return ReadTwoCharToken(TokenType.LessOrEqual, "<=");
                    }
                    return CreateSingleCharToken(TokenType.Less, '<');

                case '+':
                    return CreateSingleCharToken(TokenType.Plus, '+');

                case '-':
                    return CreateSingleCharToken(TokenType.Minus, '-');

                case '*':
                    return CreateSingleCharToken(TokenType.Multiply, '*');

                case '/':
                    return CreateSingleCharToken(TokenType.Divide, '/');

                case '(':
                    return CreateSingleCharToken(TokenType.LeftParen, '(');

                case ')':
                    return CreateSingleCharToken(TokenType.RightParen, ')');

                case '{':
                    return CreateSingleCharToken(TokenType.LeftBrace, '{');

                case '}':
                    return CreateSingleCharToken(TokenType.RightBrace, '}');

                case '\n':
                    return ReadNewline();

                case '\r':
                    // Пропускаем \r, обработаем \n отдельно
                    _position++;
                    _column++;
                    return GetNextToken();

                default:
                    return CreateErrorToken($"Unexpected character '{current}'", current.ToString());
            }
        }

        private void SkipWhitespace()
        {
            while (_position < _source.Length && char.IsWhiteSpace(_source[_position]) && _source[_position] != '\n')
            {
                char c = _source[_position];
                if (c == '\r')
                {
                    _position++;
                    _column++;
                }
                else if (c == ' ' || c == '\t')
                {
                    _position++;
                    _column++;
                }
                else
                {
                    break;
                }
            }
        }

        private Token ReadComment()
        {
            int startLine = _line;
            int startColumn = _column;
            _position += 2; // Пропускаем //
            _column += 2;

            StringBuilder sb = new StringBuilder();

            while (_position < _source.Length && _source[_position] != '\n')
            {
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            return CreateToken(TokenType.Comment, sb.ToString(), startLine, startColumn);
        }

        private Token ReadString()
        {
            int startLine = _line;
            int startColumn = _column;
            _position++; // Пропускаем открывающую кавычку
            _column++;

            StringBuilder sb = new StringBuilder();

            while (_position < _source.Length && _source[_position] != '"')
            {
                if (_source[_position] == '\n')
                {
                    return CreateErrorToken("Unterminated string literal", _source);
                }
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            if (_position >= _source.Length)
            {
                return CreateErrorToken("Unterminated string literal", _source);
            }

            _position++; // Пропускаем закрывающую кавычку
            _column++;

            return CreateToken(TokenType.StringLiteral, sb.ToString(), startLine, startColumn);
        }

        private Token ReadNumber()
        {
            int startLine = _line;
            int startColumn = _column;
            StringBuilder sb = new StringBuilder();
            bool hasDot = false;

            while (_position < _source.Length && (char.IsDigit(_source[_position]) || _source[_position] == '.'))
            {
                if (_source[_position] == '.')
                {
                    if (hasDot)
                    {
                        break;
                    }
                    hasDot = true;
                }
                sb.Append(_source[_position]);
                _position++;
                _column++;
            }

            string value = sb.ToString();
            TokenType type = hasDot ? TokenType.FloatLiteral : TokenType.IntegerLiteral;

            return CreateToken(type, value, startLine, startColumn);
        }

        private Token ReadIdentifierOrKeyword()
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

            if (Keywords.TryGetValue(value, out TokenType type))
            {
                return CreateToken(type, value, startLine, startColumn);
            }

            return CreateToken(TokenType.Identifier, value, startLine, startColumn);
        }

        private Token ReadTwoCharToken(TokenType type, string value)
        {
            int startLine = _line;
            int startColumn = _column;
            _position += 2;
            _column += 2;
            return CreateToken(type, value, startLine, startColumn);
        }

        private Token CreateSingleCharToken(TokenType type, char character)
        {
            int startLine = _line;
            int startColumn = _column;
            _position++;
            _column++;
            return CreateToken(type, character.ToString(), startLine, startColumn);
        }

        private Token ReadNewline()
        {
            int startLine = _line;
            int startColumn = _column;
            _position++;
            _line++;
            _column = 1;
            return CreateToken(TokenType.Newline, "\n", startLine, startColumn);
        }

        private Token CreateErrorToken(string message, string value)
        {
            int startLine = _line;
            int startColumn = _column;
            _position++;
            _column++;
            _errors.Add(new CompilationError(message, startLine, startColumn));
            return CreateToken(TokenType.Error, value, startLine, startColumn);
        }

        private Token CreateToken(TokenType type, string value, int line, int column)
        {
            return new Token(type, value, line, column);
        }
    }
}