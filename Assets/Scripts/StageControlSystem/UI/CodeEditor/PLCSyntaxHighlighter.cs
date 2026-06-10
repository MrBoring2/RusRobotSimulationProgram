using RobotLanguageCompiler.PLC;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.UI.CodeEditor
{
    /// <summary>
    /// Подсветка синтаксиса для PLC языка
    /// </summary>
    public class PLCSyntaxHighlighter : ISyntaxHighlighter
    {
        // Цвета для подсветки
        private static readonly Color KeywordColor = new Color(0.0f, 0.0f, 0.8f);      // Синий
        private static readonly Color SectionColor = new Color(0.1f, 0.6f, 0.6f);      // Бирюзовый
        private static readonly Color OperatorColor = new Color(0.8f, 0.4f, 0.0f);     // Оранжевый
        private static readonly Color NumberColor = new Color(0.0f, 0.6f, 0.0f);       // Зелёный
        private static readonly Color IdentifierColor = Color.black;                    // Чёрный
        private static readonly Color StringColor = new Color(0.6f, 0.2f, 0.0f);        // Тёмно-ораньжевый
        private static readonly Color ErrorColor = new Color(0.9f, 0.2f, 0.2f);        // Красный
        
        // Ключевые слова
        private static readonly HashSet<PLCTokenType> Keywords = new()
        {
            PLCTokenType.Robot, PLCTokenType.If, PLCTokenType.Elif, PLCTokenType.Else,
            PLCTokenType.StartProgram, PLCTokenType.Int, PLCTokenType.Bool,
            PLCTokenType.True, PLCTokenType.False
        };
        
        // Секции
        private static readonly HashSet<PLCTokenType> Sections = new()
        {
            PLCTokenType.InitSection, PLCTokenType.RobotsBlocksSection, PLCTokenType.LogicSection
        };
        
        // Операторы
        private static readonly HashSet<PLCTokenType> Operators = new()
        {
            PLCTokenType.Assign, PLCTokenType.Increment, PLCTokenType.Decrement,
            PLCTokenType.Equal, PLCTokenType.NotEqual, PLCTokenType.Greater,
            PLCTokenType.Less, PLCTokenType.GreaterOrEqual, PLCTokenType.LessOrEqual
        };
        
        public string GetHighlightedText(string sourceCode)
        {
            if (string.IsNullOrEmpty(sourceCode))
                return "";
            
            try
            {
                var lexer = new PLCLexer(sourceCode);
                var tokens = lexer.Tokenize();
                
                if (tokens == null || tokens.Count == 0)
                    return sourceCode;

                StringBuilder result = new StringBuilder();
                int start = sourceCode.Length;
                int end = 0;

                for (int i = tokens.Count - 1; i >= 0; i--)
                {
                    end = tokens[i].Position + tokens[i].Value.Length;

                    if (tokens[i].Type == PLCTokenType.String) end += 2;

                    result.Insert(0, sourceCode.Substring(end, start - end));
                    start = tokens[i].Position;

                    string textSegment = sourceCode.Substring(start, end - start);
                    string colorHex = ColorUtility.ToHtmlStringRGB(GetTokenColor(tokens[i].Type));
                    result.Insert(0, $"<color=#{colorHex}>{textSegment}</color>");
                }
                result.Insert(0, sourceCode.Substring(0, start));

                return result.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"PLC highlighting error: {e.Message}");
                return sourceCode;
            }
        }
        
        public List<string> GetErrors(string sourceCode)
        {
            var lexer = new PLCLexer(sourceCode);
            lexer.Tokenize();
            return lexer.Errors;
        }
        
        private Color GetTokenColor(PLCTokenType type)
        {
            if (type == PLCTokenType.Error)
                return ErrorColor;
            
            if (Keywords.Contains(type))
                return KeywordColor;
            
            if (Sections.Contains(type))
                return SectionColor;
            
            if (Operators.Contains(type))
                return OperatorColor;
            
            if (type == PLCTokenType.Number)
                return NumberColor;

            if (type == PLCTokenType.String)
                return StringColor;
            
            return IdentifierColor;
        }
    }
}