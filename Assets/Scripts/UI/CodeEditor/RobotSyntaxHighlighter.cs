using RobotLanguageCompiler.Robot;
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
        private static readonly Color NumberColor = new Color(0.0f, 0.6f, 0.0f);       // Зелёный
        private static readonly Color ErrorColor = new Color(0.9f, 0.2f, 0.2f);        // Красный
        private static readonly Color IdentifierColor = Color.black;                   // Черный

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
                
                return BuildHighlightedText(sourceCode, tokens);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Robot highlighting error: {e.Message}");
                return sourceCode;
            }
        }
        
        public List<string> GetErrors(string sourceCode)
        {
            var lexer = new RobotLexer(sourceCode);
            lexer.Tokenize();
            return lexer.Errors;
        }
        
        private string BuildHighlightedText(string source, List<RobotToken> tokens)
        {
            var colorSpans = new List<(int start, int end, Color color)>();
            
            foreach (var token in tokens)
            {
                int tokenStart = GetTokenPosition(source, token);
                if (tokenStart < 0) continue;
                
                int tokenEnd = tokenStart + token.Value.Length;
                Color color = GetTokenColor(token.Type);
                
                colorSpans.Add((tokenStart, tokenEnd, color));
            }
            
            colorSpans.Sort((a, b) => a.start.CompareTo(b.start));
            
            StringBuilder result = new StringBuilder();
            int lastPos = 0;
            
            foreach (var span in colorSpans)
            {
                if (span.start > lastPos)
                {
                    result.Append(source.Substring(lastPos, span.start - lastPos));
                }
                
                string textSegment = source.Substring(span.start, span.end - span.start);
                string colorHex = ColorUtility.ToHtmlStringRGB(span.color);
                result.Append($"<color=#{colorHex}>{textSegment}</color>");
                
                lastPos = span.end;
            }
            
            if (lastPos < source.Length)
            {
                result.Append(source.Substring(lastPos));
            }
            
            return result.ToString();
        }
        
        private int GetTokenPosition(string source, RobotToken token)
        {
            string[] lines = source.Split('\n');
            
            if (token.Line - 1 >= lines.Length)
                return -1;
            
            int position = 0;
            for (int i = 0; i < token.Line - 1; i++)
            {
                position += lines[i].Length + 1;
            }
            
            position += token.Column - 1;
            return position;
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
            
            return IdentifierColor;
        }
    }
}