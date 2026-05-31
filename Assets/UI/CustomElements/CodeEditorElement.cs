using RobotLanguageCompiler;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.UI.CodeEditor
{
    /// <summary>
    /// Кастомный элемент редактора кода с подсветкой синтаксиса
    /// </summary>
    [UxmlElement]
    public partial class CodeEditorElement : VisualElement
    {
        private ScrollView scrollView;
        private Label lineNumbersLabel;
        private TextField codeInput;
        private TextElement codeHighlight;

        private string currentText = "";
        private bool isHighlightingScheduled = false;
        private string pendingHighlightText = "";

        private int currentCursorLine = 1;
        private int currentCursorColumn = 1;

        private const int MAX_LINE_LENGTH = 75;

        // Цвета для подсветки синтаксиса (светлая тема)
        private readonly Color keywordColor = new Color(0.0f, 0.0f, 0.8f);
        private readonly Color operatorColor = new Color(0.8f, 0.4f, 0.0f);
        private readonly Color numberColor = new Color(0.0f, 0.6f, 0.0f);
        private readonly Color stringColor = new Color(0.8f, 0.2f, 0.2f);
        private readonly Color commentColor = new Color(0.2f, 0.5f, 0.2f);
        private readonly Color identifierColor = Color.black;
        private readonly Color errorColor = new Color(0.9f, 0.2f, 0.2f);

        private readonly HashSet<TokenType> keywords = new HashSet<TokenType>
        {
            TokenType.If, TokenType.Elif, TokenType.Else, TokenType.While,
            TokenType.Int, TokenType.Bool, TokenType.PtpPoint, TokenType.LinPoint,
            TokenType.Wait, TokenType.WaitFor, TokenType.Subprogram,
            TokenType.True, TokenType.False, TokenType.In, TokenType.Out
        };

        private readonly HashSet<TokenType> operators = new HashSet<TokenType>
        {
            TokenType.Assign, TokenType.Plus, TokenType.Minus,
            TokenType.Multiply, TokenType.Divide, TokenType.Equal,
            TokenType.NotEqual, TokenType.Greater, TokenType.Less,
            TokenType.GreaterOrEqual, TokenType.LessOrEqual,
            TokenType.And, TokenType.Or, TokenType.Not
        };

        public event Action<string> OnTextChanged;
        public event Action<int, int> OnCursorPositionChanged;

        public string Text
        {
            get => codeInput.text;
            set => SetText(value);
        }

        public CodeEditorElement()
        {
            InitUI();
            RegisterCallbacks();
            SetText("");
        }

        private void InitUI()
        {
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.85f, 0.85f, 0.85f);

            // Основной ScrollView
            scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.mode = ScrollViewMode.Vertical;
            Add(scrollView);

            // Контейнер для строки с номерами и кодом
            var rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;
            rowContainer.style.flexGrow = 1;
            rowContainer.style.minWidth = 0;
            scrollView.contentContainer.Add(rowContainer);

            // Номера строк
            lineNumbersLabel = new Label();
            lineNumbersLabel.style.width = 50;
            lineNumbersLabel.style.minWidth = 50;
            lineNumbersLabel.style.maxWidth = 70;
            lineNumbersLabel.style.backgroundColor = Color.clear;
            lineNumbersLabel.style.paddingTop = 5;
            lineNumbersLabel.style.paddingBottom = 5;
            lineNumbersLabel.style.paddingLeft = 5;
            lineNumbersLabel.style.paddingRight = 5;
            lineNumbersLabel.style.marginTop = 0;
            lineNumbersLabel.style.marginBottom = 0;
            lineNumbersLabel.style.marginLeft = 0;
            lineNumbersLabel.style.marginRight = 0;

            lineNumbersLabel.style.borderRightWidth = 5;
            lineNumbersLabel.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            lineNumbersLabel.style.fontSize = 16;
            lineNumbersLabel.style.color = new Color(0.3f, 0.3f, 0.3f);
            lineNumbersLabel.style.whiteSpace = WhiteSpace.Normal;
            lineNumbersLabel.style.unityTextAlign = TextAnchor.UpperRight;
            rowContainer.Add(lineNumbersLabel);

            // Контейнер для кода
            var codeContainer = new VisualElement();
            codeContainer.style.flexGrow = 1;
            codeContainer.style.minWidth = 0;
            rowContainer.Add(codeContainer);

            // Подсвеченный текст (верхний слой)
            codeHighlight = new TextElement();
            codeHighlight.style.fontSize = 16;
            codeHighlight.style.color = Color.black;
            codeHighlight.style.backgroundColor = Color.clear;
            codeHighlight.style.paddingTop = 5;
            codeHighlight.style.paddingBottom = 5;
            codeHighlight.style.paddingLeft = 7;
            codeHighlight.style.paddingRight = 5;

            codeHighlight.style.minHeight = 500;
            codeHighlight.style.whiteSpace = WhiteSpace.Normal;
            codeHighlight.style.unityTextAlign = TextAnchor.UpperLeft;
            codeHighlight.enableRichText = true;
            codeContainer.Add(codeHighlight);

            // Поле ввода (нижний слой)
            codeInput = new TextField();
            codeInput.style.position = Position.Absolute;
            codeInput.style.top = 0;
            codeInput.style.left = 0;
            codeInput.style.right = 0;
            codeInput.style.bottom = 0;
            codeInput.style.fontSize = 16;
            codeInput.style.color = Color.clear;
            codeInput.style.backgroundColor = Color.clear;
            codeInput.style.paddingTop = 5;
            codeInput.style.paddingBottom = 5;
            codeInput.style.paddingLeft = 5;
            codeInput.style.paddingRight = 5;

            codeInput.style.marginTop = 0;
            codeInput.style.marginBottom = 0;
            codeInput.style.marginLeft = 0;
            codeInput.style.marginRight = 0;
            codeInput.style.minHeight = 500;
            codeInput.style.whiteSpace = WhiteSpace.Normal;
            codeInput.multiline = true;
            codeInput.selectAllOnFocus = false;
            codeInput.style.unityTextAlign = TextAnchor.UpperLeft;
            codeContainer.Add(codeInput);
        }

        private void RegisterCallbacks()
        {
            codeInput.RegisterCallback<ChangeEvent<string>>(OnCodeInputChanged);
            codeInput.RegisterCallback<FocusOutEvent>(e => UpdateHighlightingNow());
            codeInput.RegisterCallback<MouseDownEvent>(e => ScheduleCursorUpdate(), TrickleDown.TrickleDown);
            codeInput.RegisterCallback<KeyDownEvent>(e => ScheduleCursorUpdate(), TrickleDown.TrickleDown);
        }

        private void OnCodeInputChanged(ChangeEvent<string> evt)
        {
            string newText = evt.newValue;

            // Ограничиваем длину строк
            string wrappedText = WrapLines(newText);

            if (wrappedText != newText)
            {
                // Если пришлось обернуть строки, обновляем поле без вызова события
                int cursorPos = codeInput.cursorIndex;
                codeInput.SetValueWithoutNotify(wrappedText);
                codeInput.cursorIndex = Math.Min(cursorPos, wrappedText.Length);
                currentText = wrappedText;
            }
            else
            {
                currentText = newText;
            }

            OnTextChanged?.Invoke(currentText);

            UpdateLineNumbers();
            UpdateCursorPosition();

            ScheduleHighlighting();
        }

        private string WrapLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string[] lines = text.Split('\n');
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Length <= MAX_LINE_LENGTH)
                {
                    result.Append(line);
                }
                else
                {
                    // Разбиваем длинную строку на части
                    for (int j = 0; j < line.Length; j += MAX_LINE_LENGTH)
                    {
                        int length = Math.Min(MAX_LINE_LENGTH, line.Length - j);
                        result.Append(line.Substring(j, length));
                        if (j + length < line.Length)
                        {
                            result.Append('\n');
                        }
                    }
                }

                if (i < lines.Length - 1)
                {
                    result.Append('\n');
                }
            }

            return result.ToString();
        }

        private void ScheduleHighlighting()
        {
            pendingHighlightText = currentText;

            if (!isHighlightingScheduled)
            {
                isHighlightingScheduled = true;
                schedule.Execute(() => DelayedHighlightUpdate()).StartingIn(50);
            }
        }

        private void DelayedHighlightUpdate()
        {
            isHighlightingScheduled = false;

            if (pendingHighlightText != currentText)
            {
                pendingHighlightText = currentText;
                schedule.Execute(() => DelayedHighlightUpdate()).StartingIn(50);
                return;
            }

            UpdateHighlightingNow();
        }

        private void ScheduleCursorUpdate()
        {
            schedule.Execute(() => UpdateCursorPosition()).StartingIn(0);
        }

        private void UpdateCursorPosition()
        {
            int cursorPos = codeInput.cursorIndex;
            string text = codeInput.text;

            currentCursorLine = 1;
            currentCursorColumn = 1;

            for (int i = 0; i < cursorPos && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    currentCursorLine++;
                    currentCursorColumn = 1;
                }
                else
                {
                    currentCursorColumn++;
                }
            }

            OnCursorPositionChanged?.Invoke(currentCursorLine, currentCursorColumn);
        }

        private void UpdateLineNumbers()
        {
            if (string.IsNullOrEmpty(currentText))
            {
                lineNumbersLabel.text = "1";
                return;
            }

            string[] lines = currentText.Split('\n');
            int lineCount = Math.Min(lines.Length, 9999);

            StringBuilder numbersBuilder = new StringBuilder();
            for (int i = 1; i <= lineCount; i++)
            {
                numbersBuilder.Append(i);
                numbersBuilder.Append('\n');
            }

            if (numbersBuilder.Length > 0)
                numbersBuilder.Length--;

            lineNumbersLabel.text = numbersBuilder.ToString();
        }

        private void UpdateHighlightingNow()
        {
            if (string.IsNullOrEmpty(currentText))
            {
                codeHighlight.text = "";
                return;
            }

            try
            {
                int cursorPos = codeInput.cursorIndex;
                int selectStart = codeInput.selectIndex;
                int selectEnd = codeInput.selectIndex;

                var lexer = new Lexer(currentText);
                var tokens = lexer.Tokenize();
                string highlightedText = BuildHighlightedText(tokens);
                codeHighlight.text = highlightedText;

                codeInput.cursorIndex = cursorPos;
                if (selectStart != selectEnd)
                {
                    codeInput.selectIndex = selectStart;
                    codeInput.selectIndex = selectEnd;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Highlighting error: {e.Message}");
                codeHighlight.text = currentText;
            }
        }

        private string BuildHighlightedText(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return EscapeRichText(currentText);

            var colorSpans = new List<(int start, int end, Color color)>();

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Newline || token.Type == TokenType.EndOfFile)
                    continue;

                int tokenStart = GetTokenPosition(token);
                if (tokenStart < 0) continue;

                int tokenEnd = tokenStart + token.Value.Length;
                Color color = GetTokenColor(token);

                colorSpans.Add((tokenStart, tokenEnd, color));
            }

            //colorSpans.Sort((a, b) => a.start.CompareTo(b.start));

            StringBuilder result = new StringBuilder();
            int lastPos = 0;

            foreach (var span in colorSpans)
            {
                if (span.start > lastPos)
                {
                    string between = currentText.Substring(lastPos, span.start - lastPos);
                    result.Append(between);
                }

                string textSegment = currentText.Substring(span.start, span.end - span.start);
                string colorHex = ColorUtility.ToHtmlStringRGB(span.color);
                result.Append($"<color=#{colorHex}>{textSegment}</color>");

                lastPos = span.end;
            }

            if (lastPos < currentText.Length)
            {
                string remaining = currentText.Substring(lastPos);
                result.Append(remaining);
            }

            return result.ToString();
        }

        private int GetTokenPosition(Token token)
        {
            string[] lines = currentText.Split('\n');

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

        private Color GetTokenColor(Token token)
        {
            if (token.Type == TokenType.Error)
                return errorColor;

            if (keywords.Contains(token.Type))
                return keywordColor;

            if (operators.Contains(token.Type))
                return operatorColor;

            if (token.Type == TokenType.IntegerLiteral || token.Type == TokenType.FloatLiteral)
                return numberColor;

            if (token.Type == TokenType.StringLiteral)
                return stringColor;

            if (token.Type == TokenType.Comment)
                return commentColor;

            return identifierColor;
        }

        private string EscapeRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public void SetText(string text)
        {
            currentText = text ?? "";
            string wrappedText = WrapLines(currentText);
            codeInput.SetValueWithoutNotify(wrappedText);
            if (wrappedText != currentText)
            {
                currentText = wrappedText;
            }
            UpdateLineNumbers();
            UpdateHighlightingNow();
            UpdateCursorPosition();
        }

        public string GetText()
        {
            return codeInput.text;
        }

        public (int line, int column) GetCursorPosition()
        {
            return (currentCursorLine, currentCursorColumn);
        }
    }
}