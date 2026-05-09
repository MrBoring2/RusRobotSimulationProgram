using RobotLanguageCompiler.AST;
using System.Collections.Generic;

namespace RobotLanguageCompiler
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _position;
        private readonly List<CompilationError> _errors;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _errors = new List<CompilationError>();
        }

        public List<CompilationError> Errors => _errors;

        public ProgramNode Parse()
        {
            var program = new ProgramNode();

            while (!IsAtEnd())
            {
                // Пропускаем пустые строки
                if (Current().Type == TokenType.Newline)
                {
                    Consume();
                    continue;
                }

                if (Current().Type == TokenType.EndOfFile)
                    break;

                var statement = ParseStatement();
                if (statement != null)
                {
                    program.Statements.Add(statement);
                }
            }
            return program;
        }

        private StatementNode ParseStatement()
        {
            // Пропускаем newline перед оператором
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            if (IsAtEnd() || Current().Type == TokenType.EndOfFile)
            {
                return null;
            }

            Token token = Current();

            //Console.WriteLine($"ParseStatement: token = {token.Type}, value = '{token.Value}' at {token.Line}:{token.Column}");

            switch (token.Type)
            {
                case TokenType.Int:
                case TokenType.Bool:
                    return ParseDeclaration();

                case TokenType.Identifier:
                    return ParseAssignment();

                case TokenType.Out:
                    return ParseAssignment();

                case TokenType.If:
                    return ParseIfStatement();

                case TokenType.While:
                    return ParseWhileStatement();

                case TokenType.PtpPoint:
                case TokenType.LinPoint:
                    return ParseMotionCommand();

                case TokenType.Wait:
                case TokenType.WaitFor:
                    return ParseWaitCommand();

                case TokenType.Subprogram:
                    return ParseCallCommand();

                case TokenType.LeftBrace:
                    return ParseBlock();

                default:
                    AddError(token, $"Unexpected token {token.Type}");
                    Consume();
                    SkipToNextNewlineOrBrace();
                    return null;
            }
        }

        private DeclarationNode ParseDeclaration()
        {
            var declaration = new DeclarationNode
            {
                Line = Current().Line,
                Column = Current().Column,
                Type = Current().Type
            };

            Consume(); // int или bool

            if (Current().Type != TokenType.Identifier)
            {
                AddError(Current(), "Expected identifier after type");
                return null;
            }

            declaration.Identifier = Current().Value;
            Consume();

            // Обязательная инициализация
            if (Current().Type != TokenType.Assign)
            {
                AddError(Current(), $"Expected '=' after variable declaration. Variable '{declaration.Identifier}' must be initialized");
                return null;
            }
            Consume(); // =

            declaration.Initializer = ParseExpression();
            if (declaration.Initializer == null)
            {
                AddError(Current(), "Expected initialization expression");
                return null;
            }

            return declaration;
        }

        private AssignmentNode ParseAssignment()
        {
            var assignment = new AssignmentNode
            {
                Line = Current().Line,
                Column = Current().Column
            };

            // Проверяем, это out() или обычная переменная
            if (Current().Type == TokenType.Out)
            {
                assignment.IsOutAssignment = true;
                Consume(); // out

                if (Current().Type != TokenType.LeftParen)
                {
                    AddError(Current(), "Expected '(' after out");
                    SkipToNextNewlineOrBrace();
                    return null;
                }
                Consume(); // (

                if (Current().Type != TokenType.IntegerLiteral)
                {
                    AddError(Current(), "Expected integer in out()");
                    SkipToNextNewlineOrBrace();
                    return null;
                }

                if (!int.TryParse(Current().Value, out int outNumber))
                {
                    AddError(Current(), "Invalid output number");
                    SkipToNextNewlineOrBrace();
                    return null;
                }
                assignment.OutNumber = outNumber;
                Consume();

                if (Current().Type != TokenType.RightParen)
                {
                    AddError(Current(), "Expected ')' after out number");
                    SkipToNextNewlineOrBrace();
                    return null;
                }
                Consume(); // )
            }
            else
            {
                assignment.Identifier = Current().Value;
                Consume(); // identifier
            }

            if (Current().Type != TokenType.Assign)
            {
                AddError(Current(), "Expected '=' in assignment");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // =

            assignment.Value = ParseExpression();
            if (assignment.Value == null)
            {
                return null;
            }

            return assignment;
        }

        private IfStatementNode ParseIfStatement()
        {
            //Console.WriteLine("ParseIfStatement: start");

            var ifStatement = new IfStatementNode
            {
                Line = Current().Line,
                Column = Current().Column,
                Branches = new List<IfBranchNode>()
            };

            // Потребляем if
            Consume(); // if

            // Парсим if ветку (без потребления if)
            var ifBranch = ParseIfBranch("if");
            if (ifBranch == null)
            {
                //Console.WriteLine("ParseIfStatement: ifBranch is null");
                return null;
            }
            ifStatement.Branches.Add(ifBranch);

            // Пропускаем newline после блока if
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            // Продолжаем парсить, пока встречаем elif
            while (!IsAtEnd() && Current().Type == TokenType.Elif)
            {
                //Console.WriteLine("ParseIfStatement: found elif");

                // Потребляем elif токен
                Consume(); // elif

                // Парсим ветку elif
                var elifBranch = ParseIfBranch("elif");
                if (elifBranch == null)
                {
                    break;
                }
                ifStatement.Branches.Add(elifBranch);

                // Пропускаем newline после блока elif
                while (!IsAtEnd() && Current().Type == TokenType.Newline)
                {
                    Consume();
                }
            }

            // Парсим else если есть
            if (!IsAtEnd() && Current().Type == TokenType.Else)
            {
                //Console.WriteLine("ParseIfStatement: found else");
                Consume(); // else

                // Пропускаем newline перед блоком
                while (!IsAtEnd() && Current().Type == TokenType.Newline)
                {
                    Consume();
                }

                ifStatement.ElseBlock = ParseBlock();
            }

            //Console.WriteLine("ParseIfStatement: success");
            return ifStatement;
        }

        private IfBranchNode ParseIfBranch(string branchType)
        {
            //Console.WriteLine($"ParseIfBranch: {branchType}, current token = {Current().Type}");

            var branch = new IfBranchNode();

            // Пропускаем newline перед скобкой
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            // Парсим условие в скобках
            if (Current().Type != TokenType.LeftParen)
            {
                AddError(Current(), $"Expected '(' after {branchType}");
                //Console.WriteLine($"ParseIfBranch: expected '(' but got {Current().Type}");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // (

            // Пропускаем newline внутри скобок (если есть)
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            branch.Condition = ParseExpression();
            if (branch.Condition == null)
            {
                //Console.WriteLine("ParseIfBranch: condition is null");
                SkipToNextNewlineOrBrace();
                return null;
            }

            // Проверяем, что условие корректно (для if должно быть булевым выражением)
            // Это семантическая проверка, добавим позже

            // Пропускаем newline перед закрывающей скобкой
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            if (Current().Type != TokenType.RightParen)
            {
                AddError(Current(), "Expected ')' after condition");
                //Console.WriteLine($"ParseIfBranch: expected ')' but got {Current().Type}");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // )

            // Пропускаем newline перед блоком
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            // Проверяем, есть ли блок
            if (Current().Type != TokenType.LeftBrace)
            {
                AddError(Current(), $"Expected '{{' after {branchType} condition");
                SkipToNextNewlineOrBrace();
                return null;
            }

            // Парсим блок
            branch.Block = ParseBlock();
            if (branch.Block == null)
            {
                //Console.WriteLine("ParseIfBranch: block is null");
                return null;
            }

            branch.Line = branch.Condition.Line;
            branch.Column = branch.Condition.Column;

            //Console.WriteLine($"ParseIfBranch: success");
            return branch;
        }

        private WhileStatementNode ParseWhileStatement()
        {
            var whileStatement = new WhileStatementNode
            {
                Line = Current().Line,
                Column = Current().Column
            };

            Consume(); // while

            // Пропускаем newline после while
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            if (Current().Type != TokenType.LeftParen)
            {
                AddError(Current(), "Expected '(' after while");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // (

            whileStatement.Condition = ParseExpression();
            if (whileStatement.Condition == null)
            {
                SkipToNextNewlineOrBrace();
                return null;
            }

            if (Current().Type != TokenType.RightParen)
            {
                AddError(Current(), "Expected ')' after while condition");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // )

            // Пропускаем newline перед блоком
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            whileStatement.Block = ParseBlock();
            if (whileStatement.Block == null)
            {
                return null;
            }

            return whileStatement;
        }

        private MotionCommandNode ParseMotionCommand()
        {
            var motion = new MotionCommandNode
            {
                Line = Current().Line,
                Column = Current().Column,
                IsPtp = Current().Type == TokenType.PtpPoint
            };

            Consume(); // ptp_point или lin_point

            if (Current().Type != TokenType.LeftParen)
            {
                AddError(Current(), "Expected '(' after motion command");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // (

            if (Current().Type != TokenType.Identifier && Current().Type != TokenType.IntegerLiteral)
            {
                AddError(Current(), "Expected point name");
                SkipToNextNewlineOrBrace();
                return null;
            }

            motion.PointName = Current().Value;
            Consume();

            if (Current().Type != TokenType.RightParen)
            {
                AddError(Current(), "Expected ')' after point name");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // )

            return motion;
        }

        private WaitCommandNode ParseWaitCommand()
        {
            var wait = new WaitCommandNode
            {
                Line = Current().Line,
                Column = Current().Column,
                IsWaitFor = Current().Type == TokenType.WaitFor
            };

            Consume(); // wait или wait_for

            if (Current().Type != TokenType.LeftParen)
            {
                AddError(Current(), "Expected '(' after wait command");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // (

            if (wait.IsWaitFor)
            {
                wait.Condition = ParseExpression();
                if (wait.Condition == null)
                {
                    SkipToNextNewlineOrBrace();
                    return null;
                }
            }
            else
            {
                if (Current().Type != TokenType.FloatLiteral && Current().Type != TokenType.IntegerLiteral)
                {
                    AddError(Current(), "Expected number in wait()");
                    SkipToNextNewlineOrBrace();
                    return null;
                }

                if (!double.TryParse(Current().Value, out double seconds))
                {
                    AddError(Current(), "Invalid number format");
                    SkipToNextNewlineOrBrace();
                    return null;
                }
                wait.Seconds = seconds;
                Consume();
            }

            if (Current().Type != TokenType.RightParen)
            {
                AddError(Current(), "Expected ')' after wait argument");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // )

            return wait;
        }

        private CallCommandNode ParseCallCommand()
        {
            var call = new CallCommandNode
            {
                Line = Current().Line,
                Column = Current().Column
            };

            Consume(); // subprogram

            if (Current().Type != TokenType.LeftParen)
            {
                AddError(Current(), "Expected '(' after subprogram");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // (

            if (Current().Type != TokenType.StringLiteral)
            {
                AddError(Current(), "Expected string literal for subprogram file");
                SkipToNextNewlineOrBrace();
                return null;
            }

            call.SubprogramFile = Current().Value;
            Consume();

            if (Current().Type != TokenType.RightParen)
            {
                AddError(Current(), "Expected ')' after subprogram file");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // )

            return call;
        }

        private BlockNode ParseBlock()
        {
            //Console.WriteLine($"ParseBlock: start, current token = {Current().Type}");

            // Сохраняем позицию открывающей скобки
            int openBraceLine = Current().Line;
            int openBraceColumn = Current().Column;

            var block = new BlockNode
            {
                Line = Current().Line,
                Column = Current().Column,
                Statements = new List<StatementNode>()
            };

            if (Current().Type != TokenType.LeftBrace)
            {
                AddError(Current(), "Expected '{'");
                SkipToNextNewlineOrBrace();
                return null;
            }
            Consume(); // {

            // Пропускаем начальные newline
            while (!IsAtEnd() && Current().Type == TokenType.Newline)
            {
                Consume();
            }

            // Парсим операторы внутри блока
            while (!IsAtEnd() && Current().Type != TokenType.RightBrace && Current().Type != TokenType.EndOfFile)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                }

                // Пропускаем newline между операторами (необязательно)
                while (!IsAtEnd() && Current().Type == TokenType.Newline)
                {
                    Consume();
                }
            }

            if (IsAtEnd() || Current().Type == TokenType.EndOfFile)
            {
                _errors.Add(new CompilationError(
                    "Expected '}' to close block",
                    Current().Line, Current().Column,
                    openBraceLine, openBraceColumn,
                    "Block started here"));
                return block;
            }

            if (Current().Type != TokenType.RightBrace)
            {
                _errors.Add(new CompilationError(
                    "Expected '}' to close block",
                    Current().Line, Current().Column,
                    openBraceLine, openBraceColumn,
                    "Block started here"));
                return block;
            }
            Consume(); // }

            //Console.WriteLine($"ParseBlock: success, {block.Statements.Count} statements");
            return block;
        }

        private void SkipToNextNewlineOrBrace()
        {
            while (!IsAtEnd() &&
                   Current().Type != TokenType.Newline &&
                   Current().Type != TokenType.RightBrace &&
                   Current().Type != TokenType.LeftBrace &&
                   Current().Type != TokenType.EndOfFile)
            {
                Consume();
            }

            //// Пропускаем newline если есть
            //if (!IsAtEnd() && Current().Type == TokenType.Newline)
            //{
            //    Consume();
            //}
        }

        private ExpressionNode ParseExpression()
        {
            if (IsAtEnd()) return null;
            return ParseOrExpression();
        }

        private ExpressionNode ParseOrExpression()
        {
            var left = ParseAndExpression();
            if (left == null) return null;

            while (!IsAtEnd() && Current().Type == TokenType.Or)
            {
                var op = Current().Type;
                Consume();
                var right = ParseAndExpression();
                if (right == null) return null;

                left = new BinaryExpressionNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }

            return left;
        }

        private ExpressionNode ParseAndExpression()
        {
            var left = ParseNotExpression();
            if (left == null) return null;

            while (!IsAtEnd() && Current().Type == TokenType.And)
            {
                var op = Current().Type;
                Consume();
                var right = ParseNotExpression();
                if (right == null) return null;

                left = new BinaryExpressionNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }

            return left;
        }

        private ExpressionNode ParseNotExpression()
        {
            if (!IsAtEnd() && Current().Type == TokenType.Not)
            {
                var op = Current().Type;
                int line = Current().Line;
                int column = Current().Column;
                Consume();
                var operand = ParseNotExpression();
                if (operand == null) return null;

                return new UnaryExpressionNode
                {
                    Operator = op,
                    Operand = operand,
                    Line = line,
                    Column = column
                };
            }

            return ParseComparisonExpression();
        }

        private ExpressionNode ParseComparisonExpression()
        {
            var left = ParseAdditiveExpression();
            if (left == null) return null;

            // Проверяем, что если left имеет тип In или Identifier, то должен следовать оператор сравнения
            // Это упрощенная проверка, лучше делать после парсинга

            if (Current().Type == TokenType.Equal ||
                Current().Type == TokenType.NotEqual ||
                Current().Type == TokenType.Greater ||
                Current().Type == TokenType.Less ||
                Current().Type == TokenType.GreaterOrEqual ||
                Current().Type == TokenType.LessOrEqual)
            {
                var op = Current().Type;
                Consume();
                var right = ParseAdditiveExpression();
                if (right == null) return null;

                return new BinaryExpressionNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }

            // Если нет оператора сравнения, но left - это не булево значение, то это ошибка
            // Но лучше проверять типы после парсинга

            return left;
        }

        private ExpressionNode ParseAdditiveExpression()
        {
            var left = ParseMultiplicativeExpression();
            if (left == null) return null;

            while (!IsAtEnd() && (Current().Type == TokenType.Plus || Current().Type == TokenType.Minus))
            {
                var op = Current().Type;
                Consume();
                var right = ParseMultiplicativeExpression();
                if (right == null) return null;

                left = new BinaryExpressionNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }

            return left;
        }

        private ExpressionNode ParseMultiplicativeExpression()
        {
            var left = ParsePrimaryExpression();
            if (left == null) return null;

            while (!IsAtEnd() && (Current().Type == TokenType.Multiply || Current().Type == TokenType.Divide))
            {
                var op = Current().Type;
                Consume();
                var right = ParsePrimaryExpression();
                if (right == null) return null;

                left = new BinaryExpressionNode
                {
                    Operator = op,
                    Left = left,
                    Right = right,
                    Line = left.Line,
                    Column = left.Column
                };
            }

            return left;
        }

        private ExpressionNode ParsePrimaryExpression()
        {
            if (IsAtEnd()) return null;

            Token token = Current();

            //Console.WriteLine($"ParsePrimaryExpression: token = {token.Type}, value = '{token.Value}'");

            switch (token.Type)
            {
                case TokenType.IntegerLiteral:
                    Consume();
                    return new LiteralNode
                    {
                        Type = token.Type,
                        Value = int.Parse(token.Value),
                        Line = token.Line,
                        Column = token.Column
                    };

                case TokenType.FloatLiteral:
                    Consume();
                    return new LiteralNode
                    {
                        Type = token.Type,
                        Value = double.Parse(token.Value),
                        Line = token.Line,
                        Column = token.Column
                    };

                case TokenType.True:
                    Consume();
                    return new LiteralNode
                    {
                        Type = token.Type,
                        Value = true,
                        Line = token.Line,
                        Column = token.Column
                    };

                case TokenType.False:
                    Consume();
                    return new LiteralNode
                    {
                        Type = token.Type,
                        Value = false,
                        Line = token.Line,
                        Column = token.Column
                    };

                case TokenType.Identifier:
                    Consume();
                    return new IdentifierNode
                    {
                        Name = token.Value,
                        Line = token.Line,
                        Column = token.Column
                    };

                case TokenType.In:
                    Consume(); // in

                    if (Current().Type != TokenType.LeftParen)
                    {
                        AddError(Current(), "Expected '(' after in");
                        SkipToNextNewlineOrBrace();
                        return null;
                    }
                    Consume(); // (

                    if (Current().Type != TokenType.IntegerLiteral)
                    {
                        AddError(Current(), "Expected integer in in()");
                        SkipToNextNewlineOrBrace();
                        return null;
                    }

                    if (!int.TryParse(Current().Value, out int inputNumber))
                    {
                        AddError(Current(), "Invalid input number");
                        SkipToNextNewlineOrBrace();
                        return null;
                    }
                    Consume();

                    if (Current().Type != TokenType.RightParen)
                    {
                        AddError(Current(), "Expected ')' after in number");
                        SkipToNextNewlineOrBrace();
                        return null;
                    }
                    Consume(); // )

                    return new InFunctionNode
                    {
                        InputNumber = inputNumber,
                        Line = token.Line,
                        Column = token.Column
                    };

                case TokenType.LeftParen:
                    Consume(); // (
                    var expr = ParseExpression();
                    if (expr == null)
                    {
                        SkipToNextNewlineOrBrace();
                        return null;
                    }

                    // Пропускаем newline перед закрывающей скобкой
                    while (!IsAtEnd() && Current().Type == TokenType.Newline)
                    {
                        Consume();
                    }

                    if (Current().Type != TokenType.RightParen)
                    {
                        AddError(Current(), "Expected ')'");
                        SkipToNextNewlineOrBrace();
                        return null;
                    }
                    Consume(); // )
                    return expr;

                default:
                    AddError(token, $"Unexpected token in expression: {token.Type}");
                    SkipToNextNewlineOrBrace();
                    return null;
            }
        }

        private Token Current()
        {
            if (_position >= _tokens.Count)
                return new Token(TokenType.EndOfFile, "", 0, 0);
            return _tokens[_position];
        }

        private void Consume()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count || _tokens[_position].Type == TokenType.EndOfFile;
        }

        private void AddError(Token token, string message)
        {
            _errors.Add(new CompilationError(message, token.Line, token.Column));
        }

    }
}