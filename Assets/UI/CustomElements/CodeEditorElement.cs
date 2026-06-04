using System;
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
        private bool isUpdatingFromCode = false;

        private int currentCursorLine = 1;
        private int currentCursorColumn = 1;

        private int previousTextLength = 0;

        private ISyntaxHighlighter currentHighlighter;

        private const int MAX_LINE_LENGTH = 75;
        private const int MAX_LINES = 999;

        public event Action<string> OnTextChanged;
        public event Action<int, int> OnCursorPositionChanged;

        public CodeEditorElement()
        {
            InitUI();
            RegisterCallbacks();
            SetText("");
        }

        /// <summary>
        /// Устанавливает подсветщик синтаксиса для текущего языка
        /// </summary>
        /// <param name="highlighter">Экземпляр подсветщика синтаксиса</param>
        public void SetHighlighter(ISyntaxHighlighter highlighter)
        {
            currentHighlighter = highlighter;
            UpdateHighlightingNow();
        }

        /// <summary>
        /// Инициализирует визуальные элементы редактора
        /// </summary>
        private void InitUI()
        {
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.85f, 0.85f, 0.85f);

            scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.mode = ScrollViewMode.Vertical;
            Add(scrollView);

            var rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;
            rowContainer.style.flexGrow = 1;
            rowContainer.style.minWidth = 0;
            scrollView.contentContainer.Add(rowContainer);

            Font courierFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
            var fontDef = new FontDefinition();
            fontDef.font = courierFont;

            // Номера строк
            lineNumbersLabel = new Label();
            lineNumbersLabel.style.width = 50;
            lineNumbersLabel.style.minWidth = 50;
            lineNumbersLabel.style.maxWidth = 70;
            lineNumbersLabel.style.unityFontDefinition = fontDef;
            lineNumbersLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
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

            // Подсвеченный текст (нижний слой)
            codeHighlight = new TextElement();
            codeHighlight.style.fontSize = 16;
            codeHighlight.style.unityFontDefinition = fontDef;
            codeHighlight.style.unityFontStyleAndWeight = FontStyle.Bold;
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

            // Поле ввода (верхний слой)
            codeInput = new TextField();
            codeInput.style.position = Position.Absolute;
            codeInput.style.top = 0;
            codeInput.style.left = 0;
            codeInput.style.right = 0;
            codeInput.style.bottom = 0;
            codeInput.style.fontSize = 16;
            codeInput.style.unityFontDefinition = fontDef;
            codeInput.style.unityFontStyleAndWeight = FontStyle.Bold;
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

        /// <summary>
        /// Регистрирует обработчики событий для поля ввода
        /// </summary>
        private void RegisterCallbacks()
        {
            codeInput.RegisterCallback<ChangeEvent<string>>(OnCodeInputChanged);
            codeInput.RegisterCallback<FocusOutEvent>(e => UpdateHighlightingNow());
            codeInput.RegisterCallback<MouseDownEvent>(e => ScheduleCursorUpdate(), TrickleDown.TrickleDown);
            codeInput.RegisterCallback<KeyDownEvent>(e => ScheduleCursorUpdate(), TrickleDown.TrickleDown);
        }

        /// <summary>
        /// Обработчик изменения текста в поле ввода
        /// </summary>
        private void OnCodeInputChanged(ChangeEvent<string> evt)
        {
            if (isUpdatingFromCode) return;

            string Text = evt.newValue;
            int cursorPos = codeInput.cursorIndex;
            int cursorCorrection = 0;

            // 1. Обработка Enter (добавление отступа)
            if (cursorPos > 0 && Text[cursorPos - 1] == '\n' && Text.Length == previousTextLength + 1)
            {
                int lineStart = cursorPos - 1;

                while (lineStart > 0 && Text[lineStart - 1] != '\n')
                {
                    lineStart--;
                }

                int indentLength = 0;
                for (int i = lineStart; i < Text.Length && Text[i] == ' '; i++)
                {
                    indentLength++;
                }

                if (indentLength > 0)
                {
                    string indent = new string(' ', indentLength);
                    Text = Text.Insert(cursorPos, indent);
                }
                cursorCorrection = indentLength;
            }

            // 2. Удаляем символы возврата каретки
            Text = Text.Replace("\r", "");

            // 3. Заменяем табуляции на пробелы
            int oldLength = Text.Length;
            Text = Text.Replace("\t", "    ");
            int newLength = Text.Length;

            cursorCorrection += newLength - oldLength;

            // 4. Разбиваем длинные строки
            Text = WrapLines(Text);

            // 5. Обновляем текст, если он изменился
            if (Text != currentText)
            {
                isUpdatingFromCode = true;
                cursorPos = codeInput.cursorIndex += cursorCorrection;
                codeInput.SetValueWithoutNotify(Text);
                codeInput.cursorIndex = codeInput.selectIndex = cursorPos < Text.Length ? cursorPos : Text.Length;
                isUpdatingFromCode = false;
                currentText = Text;

                OnTextChanged?.Invoke(currentText);

                UpdateLineNumbers();
                UpdateCursorPosition();
                ScheduleHighlighting();
            }
            previousTextLength = currentText.Length;
        }

        /// <summary>
        /// Разбивает длинные строки на несколько по максимальной длине
        /// </summary>
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

        /// <summary>
        /// Планирует отложенное обновление подсветки синтаксиса
        /// </summary>
        private void ScheduleHighlighting()
        {
            pendingHighlightText = currentText;

            if (!isHighlightingScheduled)
            {
                isHighlightingScheduled = true;
                schedule.Execute(() => DelayedHighlightUpdate()).StartingIn(50);
            }
        }

        /// <summary>
        /// Выполняет отложенное обновление подсветки синтаксиса
        /// </summary>
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

        /// <summary>
        /// Планирует обновление позиции курсора на следующем кадре
        /// </summary>
        private void ScheduleCursorUpdate()
        {
            schedule.Execute(() => UpdateCursorPosition()).StartingIn(0);
        }

        /// <summary>
        /// Обновляет отображаемую позицию курсора (строка, столбец)
        /// </summary>
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

        /// <summary>
        /// Обновляет отображение номеров строк
        /// </summary>
        private void UpdateLineNumbers()
        {
            if (string.IsNullOrEmpty(currentText))
            {
                lineNumbersLabel.text = "1";
                return;
            }

            string[] lines = currentText.Split('\n');
            int lineCount = Math.Min(lines.Length, MAX_LINES);

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

        /// <summary>
        /// Немедленно обновляет подсветку синтаксиса
        /// </summary>
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

                string highlightedText = currentHighlighter != null
                    ? currentHighlighter.GetHighlightedText(currentText)
                    : currentText;

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

        /// <summary>
        /// Устанавливает текст в редактор
        /// </summary>
        /// <param name="text">Новый текст</param>
        public void SetText(string text)
        {
            string processed = text ?? "";
            processed = processed.Replace("\r", "");
            processed = processed.Replace("\t", "    ");
            processed = WrapLines(processed);

            isUpdatingFromCode = true;
            codeInput.SetValueWithoutNotify(processed);
            isUpdatingFromCode = false;

            currentText = processed;
            previousTextLength = processed.Length;

            UpdateLineNumbers();
            UpdateHighlightingNow();
            UpdateCursorPosition();
        }

        /// <summary>
        /// Возвращает текущий текст из редактора
        /// </summary>
        /// <returns>Текст редактора</returns>
        public string GetText()
        {
            return codeInput.text;
        }
    }
}