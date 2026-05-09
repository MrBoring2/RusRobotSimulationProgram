namespace RobotLanguageCompiler
{
    /// <summary>
    /// Типы токенов
    /// </summary>
    public enum TokenType
    {
        // Ключевые слова
        If,
        Elif,
        Else,
        While,
        Int,
        Bool,
        PtpPoint,
        LinPoint,
        Wait,
        WaitFor,
        Subprogram,
        True,
        False,
        In,
        Out,

        // Операторы
        Assign,          // =
        Plus,            // +
        Minus,           // -
        Multiply,        // *
        Divide,          // /
        Equal,           // ==
        NotEqual,        // !=
        Greater,         // >
        Less,            // <
        GreaterOrEqual,  // >=
        LessOrEqual,     // <=
        And,             // and
        Or,              // or
        Not,             // not

        // Разделители
        LeftParen,       // (
        RightParen,      // )
        LeftBrace,       // {
        RightBrace,      // }

        // Литералы и идентификаторы
        Identifier,
        IntegerLiteral,
        FloatLiteral,
        StringLiteral,

        // Специальные
        Newline,
        Comment,
        EndOfFile,
        Error
    }

    /// <summary>
    /// Токен
    /// </summary>
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int line, int column)
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

    /// <summary>
    /// Ошибка компиляции
    /// </summary>
    public class CompilationError
    {
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }
        public int? ContextLine { get; }
        public int? ContextColumn { get; }
        public string ContextMessage { get; }

        public CompilationError(string message, int line, int column,
            int? contextLine = null, int? contextColumn = null, string contextMessage = null)
        {
            Message = message;
            Line = line;
            Column = column;
            ContextLine = contextLine;
            ContextColumn = contextColumn;
            ContextMessage = contextMessage;
        }

        public override string ToString()
        {
            if (ContextLine.HasValue && ContextColumn.HasValue)
            {
                return $"Error at {Line}:{Column}: {Message}\n  --> {ContextMessage} at {ContextLine}:{ContextColumn}";
            }
            return $"Error at {Line}:{Column}: {Message}";
        }
    }
}