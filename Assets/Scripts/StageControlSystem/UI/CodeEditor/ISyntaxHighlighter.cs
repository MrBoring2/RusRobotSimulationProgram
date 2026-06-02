using System.Collections.Generic;

namespace Assets.UI.CodeEditor
{
    /// <summary>
    /// Интерфейс для подсветки синтаксиса разных языков
    /// </summary>
    public interface ISyntaxHighlighter
    {
        /// <summary>
        /// Возвращает текст с rich-text тегами для подсветки
        /// </summary>
        string GetHighlightedText(string sourceCode);
        
        /// <summary>
        /// Возвращает ошибки лексера (если есть)
        /// </summary>
        List<string> GetErrors(string sourceCode);
    }
}