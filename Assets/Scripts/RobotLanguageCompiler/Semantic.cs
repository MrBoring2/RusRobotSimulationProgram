using RobotLanguageCompiler;
using RobotLanguageCompiler.AST;
using System;
using System.Collections.Generic;
using System.IO;

public class SemanticAnalyzer
{
    private readonly Dictionary<string, TokenType> _variables = new();
    private readonly HashSet<string> _definedPoints;
    private readonly Dictionary<string, string> _pointTypes = new(); // имя точки -> тип (ptp/lin)
    private readonly HashSet<string> _calledSubprograms = new();
    private readonly int _maxOutputs = 8;  // Максимальное количество выходов
    private readonly int _maxInputs = 8;    // Максимальное количество входов
    private readonly string _mainDirectory = "C:\\Users\\Anton\\Desktop\\Универ\\Диплом\\ТЕСТЫ\\";
    private readonly List<CompilationError> _errors = new();

    public SemanticAnalyzer(string pointsFile)
    {
        (_definedPoints, _pointTypes) = LoadPoints(pointsFile);
    }

    private (HashSet<string> names, Dictionary<string, string> types) LoadPoints(string pointsFile)
    {
        var names = new HashSet<string>();
        var types = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(pointsFile) || !File.Exists(pointsFile))
        {
            return (names, types);
        }

        foreach (var line in File.ReadAllLines(pointsFile))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                string pointName = parts[0];
                string pointType = parts[1].ToLower(); // ptp или lin

                names.Add(pointName);
                types[pointName] = pointType;
            }
        }

        return (names, types);
    }

    public List<CompilationError> Analyze(ProgramNode program)
    {
        foreach (var statement in program.Statements)
        {
            AnalyzeStatement(statement);
        }

        return _errors;
    }

    private void AnalyzeStatement(StatementNode statement)
    {
        switch (statement)
        {
            case DeclarationNode decl:
                AnalyzeDeclaration(decl);
                break;

            case AssignmentNode assign:
                AnalyzeAssignment(assign);
                break;

            case IfStatementNode ifStmt:
                AnalyzeIfStatement(ifStmt);
                break;

            case WhileStatementNode whileStmt:
                AnalyzeWhileStatement(whileStmt);
                break;

            case MotionCommandNode motion:
                AnalyzeMotionCommand(motion);
                break;

            case WaitCommandNode wait:
                AnalyzeWaitCommand(wait);
                break;

            case CallCommandNode call:
                AnalyzeCallCommand(call);
                break;

            case BlockNode block:
                AnalyzeBlock(block);
                break;
        }
    }

    private void AnalyzeDeclaration(DeclarationNode decl)
    {
        // Проверка: повторное объявление
        if (_variables.ContainsKey(decl.Identifier))
        {
            AddError(decl, $"Variable '{decl.Identifier}' already declared");
        }
        else
        {
            _variables[decl.Identifier] = decl.Type;

            // Проверка: несоответствие типов при инициализации
            if (decl.Initializer != null)
            {
                var initType = GetExpressionType(decl.Initializer);
                if (initType != decl.Type && initType != TokenType.Error)
                {
                    AddError(decl.Initializer,
                        $"Cannot initialize {decl.Type} with {initType}");
                }
            }
        }
    }

    private void AnalyzeAssignment(AssignmentNode assign)
    {
        // Проверка: необъявленная переменная
        if (!assign.IsOutAssignment && !_variables.ContainsKey(assign.Identifier))
        {
            AddError(assign, $"Variable '{assign.Identifier}' not declared");
            return;
        }

        // Проверка: out() в допустимом диапазоне
        if (assign.IsOutAssignment)
        {
            if (assign.OutNumber < 1 || assign.OutNumber > _maxOutputs)
            {
                AddError(assign, $"Output number {assign.OutNumber} out of range (1-{_maxOutputs})");
            }

            var exprType = GetExpressionType(assign.Value);
            if (exprType != TokenType.Bool && exprType != TokenType.True &&
                exprType != TokenType.False && exprType != TokenType.Error)
            {
                AddError(assign, $"out() expects boolean value, got {exprType}");
            }
            return;
        }

        // Проверка: несоответствие типов
        if (_variables.TryGetValue(assign.Identifier, out var varType))
        {
            var exprType = GetExpressionType(assign.Value);
            if (varType != exprType && exprType != TokenType.Error)
            {
                AddError(assign, $"Cannot assign {exprType} to {varType}");
            }
        }
    }

    private void AnalyzeIfStatement(IfStatementNode ifStmt)
    {
        // Проверка: условия должны быть bool
        foreach (var branch in ifStmt.Branches)
        {
            var condType = GetExpressionType(branch.Condition);
            if (condType != TokenType.Bool && condType != TokenType.True &&
                condType != TokenType.False && condType != TokenType.Error)
            {
                AddError(branch.Condition, "Condition must be boolean");
            }
            AnalyzeBlock(branch.Block);
        }

        if (ifStmt.ElseBlock != null)
        {
            AnalyzeBlock(ifStmt.ElseBlock);
        }
    }

    private void AnalyzeWhileStatement(WhileStatementNode whileStmt)
    {
        var condType = GetExpressionType(whileStmt.Condition);
        if (condType != TokenType.Bool && condType != TokenType.True &&
            condType != TokenType.False && condType != TokenType.Error)
        {
            AddError(whileStmt.Condition, "While condition must be boolean");
        }
        AnalyzeBlock(whileStmt.Block);
    }

    private void AnalyzeMotionCommand(MotionCommandNode motion)
    {
        // Проверка: существование точки
        if (!_definedPoints.Contains(motion.PointName))
        {
            AddError(motion, $"Point '{motion.PointName}' not defined in points file");
            return;
        }

        // Проверка: соответствие типа движения
        string expectedType = motion.IsPtp ? "ptp" : "lin";
        if (_pointTypes.TryGetValue(motion.PointName, out string actualType))
        {
            if (actualType != expectedType)
            {
                AddError(motion,
                    $"Point '{motion.PointName}' is defined as {actualType}, cannot use with {expectedType}_point");
            }
        }
    }

    private void AnalyzeWaitCommand(WaitCommandNode wait)
    {
        if (wait.IsWaitFor && wait.Condition != null)
        {
            var condType = GetExpressionType(wait.Condition);
            if (condType != TokenType.Bool && condType != TokenType.True &&
                condType != TokenType.False && condType != TokenType.Error)
            {
                AddError(wait.Condition, "wait_for condition must be boolean");
            }
        }
        // wait(seconds) не требует проверки типов, так как число всегда валидно
    }

    private void AnalyzeCallCommand(CallCommandNode call)
    {
        // Проверка: существование файла подпрограммы
        if (!File.Exists(_mainDirectory + call.SubprogramFile))
        {
            AddError(call, $"Subprogram file '{call.SubprogramFile}' not found");
            return;
        }

        // Проверка: рекурсивный вызов
        if (_calledSubprograms.Contains(call.SubprogramFile))
        {
            AddError(call, $"Recursive call detected: '{call.SubprogramFile}' calls itself");
        }
        else
        {
            _calledSubprograms.Add(call.SubprogramFile);
        }
    }

    private void AnalyzeBlock(BlockNode block)
    {
        // Сохраняем текущие переменные для восстановления после блока
        var savedVariables = new Dictionary<string, TokenType>(_variables);

        foreach (var statement in block.Statements)
        {
            AnalyzeStatement(statement);
        }

        // Восстанавливаем переменные (в языке все переменные глобальные,
        // поэтому этот блок может быть пустым, оставлен для возможного расширения)
        // _variables = savedVariables; // Раскомментировать если нужна локальная область видимости
    }

    private TokenType GetPrimaryExpressionType(ExpressionNode expr)
    {
        if (expr is InFunctionNode inFunc)
        {
            // Проверка: in() в допустимом диапазоне
            if (inFunc.InputNumber < 1 || inFunc.InputNumber > _maxInputs)
            {
                AddError(expr, $"Input number {inFunc.InputNumber} out of range (1-{_maxInputs})");
            }
            return TokenType.Bool;
        }

        return TokenType.Error;
    }

    private TokenType GetExpressionType(ExpressionNode expr)
    {
        if (expr == null) return TokenType.Error;

        return expr switch
        {
            LiteralNode lit => GetLiteralType(lit),
            IdentifierNode id => GetIdentifierType(id),
            BinaryExpressionNode bin => GetBinaryExpressionType(bin),
            UnaryExpressionNode un => GetUnaryExpressionType(un),
            InFunctionNode => GetPrimaryExpressionType(expr),
            _ => TokenType.Error
        };
    }

    private TokenType GetLiteralType(LiteralNode lit)
    {
        return lit.Type switch
        {
            TokenType.IntegerLiteral => TokenType.Int,
            TokenType.FloatLiteral => TokenType.FloatLiteral,
            TokenType.True or TokenType.False => TokenType.Bool,
            _ => TokenType.Error
        };
    }

    private TokenType GetIdentifierType(IdentifierNode id)
    {
        if (_variables.TryGetValue(id.Name, out var type))
        {
            return type;
        }

        AddError(id, $"Variable '{id.Name}' not declared");
        return TokenType.Error;
    }

    private TokenType GetBinaryExpressionType(BinaryExpressionNode bin)
    {
        var leftType = GetExpressionType(bin.Left);
        var rightType = GetExpressionType(bin.Right);

        // Если один из операндов ошибочный, не выдаем лишних ошибок
        if (leftType == TokenType.Error || rightType == TokenType.Error)
        {
            return TokenType.Error;
        }

        // Арифметические операторы
        if (bin.Operator == TokenType.Plus || bin.Operator == TokenType.Minus ||
            bin.Operator == TokenType.Multiply || bin.Operator == TokenType.Divide)
        {
            if (leftType == TokenType.Int && rightType == TokenType.Int)
            {
                return TokenType.Int;
            }
            AddError(bin, $"Arithmetic operations require integers, got {leftType} and {rightType}");
            return TokenType.Error;
        }

        // Операторы сравнения возвращают bool
        if (bin.Operator == TokenType.Equal || bin.Operator == TokenType.NotEqual)
        {
            // == и != могут сравнивать любые типы, но типы должны совпадать
            if (leftType == rightType)
            {
                return TokenType.Bool;
            }
            AddError(bin, $"Cannot compare {leftType} with {rightType}");
            return TokenType.Error;
        }

        if (bin.Operator == TokenType.Greater || bin.Operator == TokenType.Less ||
            bin.Operator == TokenType.GreaterOrEqual || bin.Operator == TokenType.LessOrEqual)
        {
            // Числовые сравнения
            if (leftType == TokenType.Int && rightType == TokenType.Int)
            {
                return TokenType.Bool;
            }
            AddError(bin, $"Comparison operators require integers, got {leftType} and {rightType}");
            return TokenType.Error;
        }

        // Логические операторы
        if (bin.Operator == TokenType.And || bin.Operator == TokenType.Or)
        {
            if ((leftType == TokenType.Bool || leftType == TokenType.True || leftType == TokenType.False) &&
                (rightType == TokenType.Bool || rightType == TokenType.True || rightType == TokenType.False))
            {
                return TokenType.Bool;
            }
            AddError(bin, $"Logical operations require boolean operands, got {leftType} and {rightType}");
            return TokenType.Error;
        }

        return TokenType.Error;
    }

    private TokenType GetUnaryExpressionType(UnaryExpressionNode un)
    {
        var operandType = GetExpressionType(un.Operand);

        if (operandType == TokenType.Error)
        {
            return TokenType.Error;
        }

        if (un.Operator == TokenType.Not)
        {
            if (operandType == TokenType.Bool || operandType == TokenType.True ||
                operandType == TokenType.False)
            {
                return TokenType.Bool;
            }
            AddError(un, $"Cannot apply 'not' to {operandType}");
            return TokenType.Error;
        }

        // Минус для чисел
        if (un.Operator == TokenType.Minus)
        {
            if (operandType == TokenType.Int)
            {
                return TokenType.Int;
            }
            AddError(un, $"Cannot apply unary minus to {operandType}");
            return TokenType.Error;
        }

        AddError(un, $"Unknown unary operator {un.Operator}");
        return TokenType.Error;
    }

    private void AddError(AstNode node, string message)
    {
        _errors.Add(new CompilationError(message, node.Line, node.Column));
    }
}