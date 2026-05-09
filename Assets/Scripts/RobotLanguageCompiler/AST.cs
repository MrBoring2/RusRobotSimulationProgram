using System.Collections.Generic;

namespace RobotLanguageCompiler.AST
{
    public abstract class AstNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    // Корневой узел
    public class ProgramNode : AstNode
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();
    }

    // Базовый класс для всех операторов и объявлений
    public abstract class StatementNode : AstNode { }

    // Объявление переменной - теперь наследуется от StatementNode
    public class DeclarationNode : StatementNode
    {
        public TokenType Type { get; set; } // Int или Bool
        public string Identifier { get; set; }
        public ExpressionNode Initializer { get; set; }
    }

    // Присваивание
    public class AssignmentNode : StatementNode
    {
        public string Identifier { get; set; }
        public bool IsOutAssignment { get; set; }
        public int OutNumber { get; set; }
        public ExpressionNode Value { get; set; }
    }

    // Условный оператор
    public class IfStatementNode : StatementNode
    {
        public List<IfBranchNode> Branches { get; set; } = new List<IfBranchNode>();
        public BlockNode ElseBlock { get; set; }
    }

    public class IfBranchNode : AstNode
    {
        public ExpressionNode Condition { get; set; }
        public BlockNode Block { get; set; }
    }

    // Цикл while
    public class WhileStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public BlockNode Block { get; set; }
    }

    // Команды движения
    public class MotionCommandNode : StatementNode
    {
        public bool IsPtp { get; set; } // true = ptp, false = lin
        public string PointName { get; set; }
    }

    // Команды ожидания
    public class WaitCommandNode : StatementNode
    {
        public double Seconds { get; set; } // для wait
        public ExpressionNode Condition { get; set; } // для wait_for
        public bool IsWaitFor { get; set; }
    }

    // Вызов подпрограммы
    public class CallCommandNode : StatementNode
    {
        public string SubprogramFile { get; set; }
    }

    // Блок операторов
    public class BlockNode : StatementNode
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();
    }

    // Выражения
    public abstract class ExpressionNode : AstNode { }

    public class BinaryExpressionNode : ExpressionNode
    {
        public TokenType Operator { get; set; }
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }
    }

    public class UnaryExpressionNode : ExpressionNode
    {
        public TokenType Operator { get; set; }
        public ExpressionNode Operand { get; set; }
    }

    public class LiteralNode : ExpressionNode
    {
        public TokenType Type { get; set; }
        public object Value { get; set; }
    }

    public class IdentifierNode : ExpressionNode
    {
        public string Name { get; set; }
    }

    public class InFunctionNode : ExpressionNode
    {
        public int InputNumber { get; set; }
    }
}