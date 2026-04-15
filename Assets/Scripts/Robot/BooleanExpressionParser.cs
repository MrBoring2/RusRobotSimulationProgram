using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class BooleanExpressionParser
{
    /// <summary>
    /// Главный метод: вычисляет строковое булево выражение
    /// </summary>
    public static bool Evaluate(string expression,
                                Dictionary<string, bool> boolVars,
                                Dictionary<string, int> intVars)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        // Убираем пробелы и приводим к нижнему регистру для удобства
        string expr = expression.Replace(" ", "").ToLowerInvariant();

        // Заменяем true и false
        expr = expr.Replace("true", "1").Replace("false", "0");

        // Заменяем булевы переменные на 1/0
        foreach (var kvp in boolVars)
        {
            string val = kvp.Value ? "1" : "0";
            expr = Regex.Replace(expr, $@"\b{kvp.Key}\b", val, RegexOptions.IgnoreCase);
        }

        // Заменяем целочисленные переменные
        foreach (var kvp in intVars)
        {
            expr = Regex.Replace(expr, $@"\b{kvp.Key}\b", kvp.Value.ToString(), RegexOptions.IgnoreCase);
        }

        // Теперь у нас должно остаться только математическое/логическое выражение
        return EvaluateExpression(expr);
    }

    // Рекурсивный парсер с поддержкой скобок, &&, ||, !, сравнений
    private static bool EvaluateExpression(string expr)
    {
        expr = expr.Trim();

        if (string.IsNullOrEmpty(expr)) return false;

        // Обработка скобок (рекурсия)
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

        // Разделяем по || (самый низкий приоритет)
        int orIndex = FindOperatorOutsideBrackets(expr, "||");
        if (orIndex != -1)
        {
            string left = expr.Substring(0, orIndex);
            string right = expr.Substring(orIndex + 2);
            return EvaluateExpression(left) || EvaluateExpression(right);
        }

        // Разделяем по && 
        int andIndex = FindOperatorOutsideBrackets(expr, "&&");
        if (andIndex != -1)
        {
            string left = expr.Substring(0, andIndex);
            string right = expr.Substring(andIndex + 2);
            return EvaluateExpression(left) && EvaluateExpression(right);
        }

        // Отрицание !
        if (expr.StartsWith("!"))
        {
            return !EvaluateExpression(expr.Substring(1));
        }

        // Сравнения: == != > < >= <=
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
        // Поддерживаемые операторы сравнения
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
                    //Debug.LogError($"Не удалось преобразовать в число: {leftStr} или {rightStr}");
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

        // Если нет операторов сравнения — считаем, что это просто число или true/false
        if (int.TryParse(expr, out int num))
            return num != 0;   // любое ненулевое число = true

        //Debug.LogWarning($"Неизвестное выражение: {expr}");
        return false;
    }
}