using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class BooleanExpressionParser
{

    public static bool Evaluate(string expression,
                                Dictionary<string, bool> boolVars,
                                Dictionary<string, int> intVars)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

  
        string expr = expression.Replace(" ", "").ToLowerInvariant();

        expr = expr.Replace("true", "1").Replace("false", "0");

        
        foreach (var kvp in boolVars)
        {
            string val = kvp.Value ? "1" : "0";
            expr = Regex.Replace(expr, $@"\b{kvp.Key}\b", val, RegexOptions.IgnoreCase);
        }

       
        foreach (var kvp in intVars)
        {
            expr = Regex.Replace(expr, $@"\b{kvp.Key}\b", kvp.Value.ToString(), RegexOptions.IgnoreCase);
        }

        return EvaluateExpression(expr);
    }


    private static bool EvaluateExpression(string expr)
    {
        expr = expr.Trim();

        if (string.IsNullOrEmpty(expr)) return false;

        if (expr.StartsWith("(") && expr.EndsWith(")"))
        {
            int bracketCount = 0;
            bool isOuter = true;
            for (int i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '(') bracketCount++;
                if (expr[i] == ')') bracketCount--;
                if (bracketCount == 0 && i != expr.Length - 1)
                {
                    isOuter = false;
                    break;
                }
            }
            if (isOuter)
                return EvaluateExpression(expr.Substring(1, expr.Length - 2));
        }

       
        int orIndex = FindOperatorOutsideBrackets(expr, "||");
        if (orIndex != -1)
        {
            string left = expr.Substring(0, orIndex);
            string right = expr.Substring(orIndex + 2);
            return EvaluateExpression(left) || EvaluateExpression(right);
        }

        
        int andIndex = FindOperatorOutsideBrackets(expr, "&&");
        if (andIndex != -1)
        {
            string left = expr.Substring(0, andIndex);
            string right = expr.Substring(andIndex + 2);
            return EvaluateExpression(left) && EvaluateExpression(right);
        }

        if (expr.StartsWith("!"))
        {
            return !EvaluateExpression(expr.Substring(1));
        }

        return EvaluateComparison(expr);
    }

    private static int FindOperatorOutsideBrackets(string expr, string op)
    {
        int bracketLevel = 0;
        for (int i = 0; i < expr.Length - op.Length + 1; i++)
        {
            if (expr[i] == '(') bracketLevel++;
            if (expr[i] == ')') bracketLevel--;

            if (bracketLevel == 0 && expr.Substring(i, op.Length) == op)
                return i;
        }
        return -1;
    }

    private static bool EvaluateComparison(string expr)
    {
        string[] operators = { ">=", "<=", "==", "!=", ">", "<" };

        foreach (var op in operators)
        {
            int index = FindOperatorOutsideBrackets(expr, op);
            if (index != -1)
            {
                string leftStr = expr.Substring(0, index).Trim();
                string rightStr = expr.Substring(index + op.Length).Trim();

                if (!int.TryParse(leftStr, out int left) || !int.TryParse(rightStr, out int right))
                {
                    
                    return false;
                }

                return op switch
                {
                    "==" => left == right,
                    "!=" => left != right,
                    ">" => left > right,
                    "<" => left < right,
                    ">=" => left >= right,
                    "<=" => left <= right,
                    _ => false
                };
            }
        }

        if (int.TryParse(expr, out int num))
            return num != 0;   

        return false;
    }
}