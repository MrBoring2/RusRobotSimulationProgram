using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.MessageBox;

namespace Assets.UI.CustomElements
{
    [UxmlElement]
    public partial class CustomCodeEditor : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<CustomCodeEditor, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((CustomCodeEditor)ve).Initialize();
            }
        }

        private ScrollView scrollView;
        private VisualElement contentContainer;
        private List<VisualElement> lineElements = new List<VisualElement>();
        private List<string> lines = new List<string>();

        public System.Action<int, int> OnCursorPositionChanged;
        public System.Action<string> OnTextChanged;

        private int currentLineIndex = 0;
        private int currentColumnIndex = 0;
        private bool isFocusable = true;

        private int cursorLineVisualIndex = 0;
        private int cursorColumnVisualIndex = 0;

        public CustomCodeEditor()
        {
            Initialize();
        }

        private void Initialize()
        {
            focusable = true;
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.12f, 0.12f, 0.125f);

            scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;

            contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.style.flexDirection = FlexDirection.Column;

            scrollView.Add(contentContainer);
            hierarchy.Add(scrollView);

            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            lines.Add("");
            RenderLines();
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            // Подсветка активной строки
            UpdateActiveLineHighlight();
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            RemoveActiveLineHighlight();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            Focus();

            // Поиск строки по позиции мыши
            Vector2 localMousePos = evt.localMousePosition;
            float yOffset = scrollView.scrollOffset.y;
            float absoluteY = localMousePos.y + yOffset;

            float currentY = 0;
            for (int i = 0; i < lineElements.Count; i++)
            {
                float lineHeight = lineElements[i].resolvedStyle.height;
                if (absoluteY >= currentY && absoluteY < currentY + lineHeight)
                {
                    SetCursorPosition(i, 0);
                    break;
                }
                currentY += lineHeight;
            }

            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!focusable) return;

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    MoveCursorUp();
                    evt.StopPropagation();
                    break;
                case KeyCode.DownArrow:
                    MoveCursorDown();
                    evt.StopPropagation();
                    break;
                case KeyCode.LeftArrow:
                    MoveCursorLeft();
                    evt.StopPropagation();
                    break;
                case KeyCode.RightArrow:
                    MoveCursorRight();
                    evt.StopPropagation();
                    break;
                case KeyCode.Home:
                    MoveCursorToLineStart();
                    evt.StopPropagation();
                    break;
                case KeyCode.End:
                    MoveCursorToLineEnd();
                    evt.StopPropagation();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    InsertNewLine();
                    evt.StopPropagation();
                    break;
                case KeyCode.Backspace:
                    DeletePreviousCharacter();
                    evt.StopPropagation();
                    break;
                case KeyCode.Delete:
                    DeleteNextCharacter();
                    evt.StopPropagation();
                    break;
                case KeyCode.Tab:
                    InsertTab();
                    evt.StopPropagation();
                    break;
                default:
                    if (evt.character != '\0' && !char.IsControl(evt.character))
                    {
                        InsertCharacter(evt.character);
                        evt.StopPropagation();
                    }
                    break;
            }
        }

        private void MoveCursorUp()
        {
            if (currentLineIndex > 0)
            {
                currentLineIndex--;
                int newColumn = Mathf.Min(currentColumnIndex, lines[currentLineIndex].Length);
                SetCursorPosition(currentLineIndex, newColumn);
            }
        }

        private void MoveCursorDown()
        {
            if (currentLineIndex < lines.Count - 1)
            {
                currentLineIndex++;
                int newColumn = Mathf.Min(currentColumnIndex, lines[currentLineIndex].Length);
                SetCursorPosition(currentLineIndex, newColumn);
            }
        }

        private void MoveCursorLeft()
        {
            if (currentColumnIndex > 0)
            {
                SetCursorPosition(currentLineIndex, currentColumnIndex - 1);
            }
            else if (currentLineIndex > 0)
            {
                currentLineIndex--;
                SetCursorPosition(currentLineIndex, lines[currentLineIndex].Length);
            }
        }

        private void MoveCursorRight()
        {
            if (currentColumnIndex < lines[currentLineIndex].Length)
            {
                SetCursorPosition(currentLineIndex, currentColumnIndex + 1);
            }
            else if (currentLineIndex < lines.Count - 1)
            {
                currentLineIndex++;
                SetCursorPosition(currentLineIndex, 0);
            }
        }

        private void MoveCursorToLineStart()
        {
            SetCursorPosition(currentLineIndex, 0);
        }

        private void MoveCursorToLineEnd()
        {
            SetCursorPosition(currentLineIndex, lines[currentLineIndex].Length);
        }

        private void InsertNewLine()
        {
            string currentLine = lines[currentLineIndex];
            string beforeCursor = currentLine.Substring(0, currentColumnIndex);
            string afterCursor = currentLine.Substring(currentColumnIndex);

            lines[currentLineIndex] = beforeCursor;
            lines.Insert(currentLineIndex + 1, afterCursor);

            currentLineIndex++;
            currentColumnIndex = 0;

            RenderLines();
            SetCursorPosition(currentLineIndex, 0);
            OnTextChanged?.Invoke(GetText());
        }

        private void DeletePreviousCharacter()
        {
            if (currentColumnIndex > 0)
            {
                string line = lines[currentLineIndex];
                string newLine = line.Remove(currentColumnIndex - 1, 1);
                lines[currentLineIndex] = newLine;
                currentColumnIndex--;
                RenderLines();
                SetCursorPosition(currentLineIndex, currentColumnIndex);
                OnTextChanged?.Invoke(GetText());
            }
            else if (currentLineIndex > 0)
            {
                // Слияние с предыдущей строкой
                string prevLine = lines[currentLineIndex - 1];
                string currentLine = lines[currentLineIndex];
                lines[currentLineIndex - 1] = prevLine + currentLine;
                lines.RemoveAt(currentLineIndex);
                currentLineIndex--;
                currentColumnIndex = prevLine.Length;
                RenderLines();
                SetCursorPosition(currentLineIndex, currentColumnIndex);
                OnTextChanged?.Invoke(GetText());
            }
        }

        private void DeleteNextCharacter()
        {
            if (currentColumnIndex < lines[currentLineIndex].Length)
            {
                string line = lines[currentLineIndex];
                string newLine = line.Remove(currentColumnIndex, 1);
                lines[currentLineIndex] = newLine;
                RenderLines();
                SetCursorPosition(currentLineIndex, currentColumnIndex);
                OnTextChanged?.Invoke(GetText());
            }
            else if (currentLineIndex < lines.Count - 1)
            {
                // Слияние со следующей строкой
                string currentLine = lines[currentLineIndex];
                string nextLine = lines[currentLineIndex + 1];
                lines[currentLineIndex] = currentLine + nextLine;
                lines.RemoveAt(currentLineIndex + 1);
                RenderLines();
                SetCursorPosition(currentLineIndex, currentColumnIndex);
                OnTextChanged?.Invoke(GetText());
            }
        }

        private void InsertTab()
        {
            InsertCharacter(' ');
            InsertCharacter(' ');
            InsertCharacter(' ');
            InsertCharacter(' ');
        }

        private void InsertCharacter(char c)
        {
            string line = lines[currentLineIndex];
            string newLine = line.Insert(currentColumnIndex, c.ToString());
            lines[currentLineIndex] = newLine;
            currentColumnIndex++;
            RenderLines();
            SetCursorPosition(currentLineIndex, currentColumnIndex);
            OnTextChanged?.Invoke(GetText());
        }

        private void RenderLines()
        {
            contentContainer.Clear();
            lineElements.Clear();

            for (int i = 0; i < lines.Count; i++)
            {
                var lineContainer = new VisualElement();
                lineContainer.name = $"Line_{i}";
                lineContainer.style.flexDirection = FlexDirection.Row;
                lineContainer.style.flexWrap = Wrap.Wrap;
                lineContainer.style.minHeight = 20;
                lineContainer.style.paddingLeft = 4;

                //// Создаём токены для строки
                //var tokens = LexLineForEditor(lines[i], i + 1);
                //foreach (var token in tokens)
                //{
                //    var tokenLabel = new Label(token.Value);
                //    tokenLabel.AddToClassList(GetCssClassForToken(token.Type));
                //    tokenLabel.style.whiteSpace = WhiteSpace.Normal;
                //    tokenLabel.style.fontSize = 13;

                //    if (token.Type == TokenType.Error)
                //    {
                //        tokenLabel.AddToClassList("code-error");
                //    }

                //    lineContainer.Add(tokenLabel);
                //}

                //if (string.IsNullOrEmpty(lines[i]) || tokens.Count == 0)
                //{
                //    var spaceLabel = new Label(" ");
                //    spaceLabel.style.minHeight = 20;
                //    lineContainer.Add(spaceLabel);
                //}

                contentContainer.Add(lineContainer);
                lineElements.Add(lineContainer);
            }

            UpdateActiveLineHighlight();
        }

        private void UpdateActiveLineHighlight()
        {
            RemoveActiveLineHighlight();
            if (currentLineIndex >= 0 && currentLineIndex < lineElements.Count)
            {
                lineElements[currentLineIndex].AddToClassList("active-line");
            }
        }

        private void RemoveActiveLineHighlight()
        {
            foreach (var line in lineElements)
            {
                line.RemoveFromClassList("active-line");
            }
        }

        private void SetCursorPosition(int lineIndex, int columnIndex)
        {
            currentLineIndex = Mathf.Clamp(lineIndex, 0, lines.Count - 1);
            currentColumnIndex = Mathf.Clamp(columnIndex, 0, lines[currentLineIndex].Length);

            UpdateActiveLineHighlight();

            OnCursorPositionChanged?.Invoke(currentLineIndex + 1, currentColumnIndex + 1);

            // Прокрутка к курсору
            ScrollToCursor();
        }

        private void ScrollToCursor()
        {
            if (currentLineIndex >= 0 && currentLineIndex < lineElements.Count)
            {
                var lineElement = lineElements[currentLineIndex];
                float elementTop = lineElement.worldBound.y - contentContainer.worldBound.y;
                float elementBottom = elementTop + lineElement.resolvedStyle.height;

                float viewportTop = scrollView.scrollOffset.y;
                float viewportBottom = viewportTop + scrollView.contentViewport.resolvedStyle.height;

                if (elementTop < viewportTop)
                {
                    scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, elementTop - 10);
                }
                else if (elementBottom > viewportBottom)
                {
                    scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, elementBottom - scrollView.contentViewport.resolvedStyle.height + 10);
                }
            }
        }

        public void SetText(string text)
        {
            lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            if (lines.Count == 0) lines.Add("");
            currentLineIndex = 0;
            currentColumnIndex = 0;
            RenderLines();
            OnTextChanged?.Invoke(GetText());
            SetCursorPosition(0, 0);
        }

        public string GetText()
        {
            return string.Join("\n", lines);
        }

        // Временная упрощённая лексика для редактора
        //private List<Token> LexLineForEditor(string line, int lineNumber)
        //{
        //    var tokens = new List<Token>();
        //    if (string.IsNullOrEmpty(line)) return tokens;

        //    string[] keywords = { "if", "elif", "else", "while", "int", "bool", "ptppoint", "linpoint", "wait", "waitfor", "subprogram", "true", "false", "in", "out" };
        //    string[] operators = { "=", "+", "-", "*", "/", "==", "!=", ">", "<", ">=", "<=", "and", "or", "not" };

        //    int pos = 0;
        //    while (pos < line.Length)
        //    {
        //        if (char.IsWhiteSpace(line[pos]))
        //        {
        //            int start = pos;
        //            while (pos < line.Length && char.IsWhiteSpace(line[pos])) pos++;
        //            tokens.Add(new Token(TokenType.Identifier, line.Substring(start, pos - start), lineNumber, start + 1));
        //            continue;
        //        }

        //        if (line[pos] == '/' && pos + 1 < line.Length && line[pos + 1] == '/')
        //        {
        //            tokens.Add(new Token(TokenType.Comment, line.Substring(pos), lineNumber, pos + 1));
        //            break;
        //        }

        //        if (char.IsLetter(line[pos]) || line[pos] == '_')
        //        {
        //            int start = pos;
        //            while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_')) pos++;
        //            string word = line.Substring(start, pos - start);
        //            TokenType type = keywords.Contains(word.ToLower()) ? TokenType.If : TokenType.Identifier;
        //            tokens.Add(new Token(type, word, lineNumber, start + 1));
        //            continue;
        //        }

        //        if (char.IsDigit(line[pos]))
        //        {
        //            int start = pos;
        //            while (pos < line.Length && (char.IsDigit(line[pos]) || line[pos] == '.')) pos++;
        //            tokens.Add(new Token(TokenType.IntegerLiteral, line.Substring(start, pos - start), lineNumber, start + 1));
        //            continue;
        //        }

        //        if (line[pos] == '"')
        //        {
        //            int start = pos;
        //            pos++;
        //            while (pos < line.Length && line[pos] != '"') pos++;
        //            if (pos < line.Length && line[pos] == '"') pos++;
        //            tokens.Add(new Token(TokenType.StringLiteral, line.Substring(start, pos - start), lineNumber, start + 1));
        //            continue;
        //        }

        //        bool matched = false;
        //        foreach (string op in operators.OrderByDescending(x => x.Length))
        //        {
        //            if (pos + op.Length <= line.Length && line.Substring(pos, op.Length) == op)
        //            {
        //                tokens.Add(new Token(TokenType.Assign, op, lineNumber, pos + 1));
        //                pos += op.Length;
        //                matched = true;
        //                break;
        //            }
        //        }
        //        if (matched) continue;

        //        tokens.Add(new Token(TokenType.Error, line[pos].ToString(), lineNumber, pos + 1));
        //        pos++;
        //    }

        //    return tokens;
        //}

        //private string GetCssClassForToken(TokenType type)
        //{
        //    switch (type)
        //    {
        //        case TokenType.If:
        //        case TokenType.Elif:
        //        case TokenType.Else:
        //        case TokenType.While:
        //        case TokenType.Int:
        //        case TokenType.Bool:
        //        case TokenType.PtpPoint:
        //        case TokenType.LinPoint:
        //        case TokenType.Wait:
        //        case TokenType.WaitFor:
        //        case TokenType.Subprogram:
        //        case TokenType.True:
        //        case TokenType.False:
        //        case TokenType.In:
        //        case TokenType.Out:
        //            return "code-keyword";

        //        case TokenType.Assign:
        //        case TokenType.Plus:
        //        case TokenType.Minus:
        //        case TokenType.Multiply:
        //        case TokenType.Divide:
        //        case TokenType.Equal:
        //        case TokenType.NotEqual:
        //        case TokenType.Greater:
        //        case TokenType.Less:
        //        case TokenType.GreaterOrEqual:
        //        case TokenType.LessOrEqual:
        //        case TokenType.And:
        //        case TokenType.Or:
        //        case TokenType.Not:
        //            return "code-operator";

        //        case TokenType.Comment:
        //            return "code-comment";

        //        case TokenType.IntegerLiteral:
        //            return "code-number";

        //        case TokenType.StringLiteral:
        //            return "code-string";

        //        case TokenType.Error:
        //            return "code-error";

        //        default:
        //            return "code-identifier";
        //    }
        //}
    }
}
