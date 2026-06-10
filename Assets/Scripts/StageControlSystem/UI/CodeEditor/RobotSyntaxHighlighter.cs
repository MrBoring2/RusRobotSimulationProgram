using RobotLanguageCompiler.PLC;
using RobotLanguageCompiler.Robot;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.UI.CodeEditor
{
    /// <summary>
    /// Подсветка синтаксиса для Robot языка
    /// </summary>
    public class RobotSyntaxHighlighter : ISyntaxHighlighter
    {
        // Цвета для подсветки
        private static readonly Color KeywordColor = new Color(0.0f, 0.0f, 0.8f);      // Синий
        private static readonly Color IdentifierColor = Color.black;                   // Чёрный
        private static readonly Color NumberColor = new Color(0.0f, 0.6f, 0.0f);       // Зелёный
        private static readonly Color StringColor = new Color(0.6f, 0.2f, 0.0f);        // Тёмно-ораньжевый
        private static readonly Color ErrorColor = new Color(0.9f, 0.2f, 0.2f);        // Красный
        
        // Ключевые слова
        private static readonly HashSet<RobotTokenType> Keywords = new()
        {
            RobotTokenType.PtpPoint, RobotTokenType.LinPoint, RobotTokenType.Wait,
            RobotTokenType.OpenEffector, RobotTokenType.CloseEffector
        };

        public string GetHighlightedText(string sourceCode)
        {
            if (string.IsNullOrEmpty(sourceCode))
                return "";

            try
            {
                var lexer = new RobotLexer(sourceCode);
                var tokens = lexer.Tokenize();

                if (tokens == null || tokens.Count == 0)
                    return sourceCode;

                StringBuilder result = new StringBuilder();
                int start = sourceCode.Length;
                int end = 0;

                for (int i = tokens.Count - 1; i >= 0; i--)
                {
                    end = tokens[i].Position + tokens[i].Value.Length;

                    if (tokens[i].Type == RobotTokenType.String) end += 2;

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
            var lexer = new RobotLexer(sourceCode);
            lexer.Tokenize();
            return lexer.Errors;
        }
        
        private Color GetTokenColor(RobotTokenType type)
        {
            if (type == RobotTokenType.Error)
                return ErrorColor;
            
            if (Keywords.Contains(type))
                return KeywordColor;
            
            if (type == RobotTokenType.Identifier)
                return IdentifierColor;
            
            if (type == RobotTokenType.Number)
                return NumberColor;

            if (type == RobotTokenType.String)
                return StringColor;

            return IdentifierColor;
        }
    }
}