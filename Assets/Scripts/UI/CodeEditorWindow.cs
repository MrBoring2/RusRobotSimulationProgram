using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.RobotPanel;
using Assets.Scripts.CustomServiceManager;
using RobotLanguageCompiler.PLC;
using RobotLanguageCompiler.Robot;
using SFB;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.UI.CodeEditor
{
    public class CodeEditorWindow : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset treeAsset;
        [SerializeField] private Texture2D openIcon;
        [SerializeField] private Texture2D saveIcon;
        [SerializeField] private Texture2D saveAsIcon;
        [SerializeField] private Texture2D compileIcon;
        [SerializeField] private Texture2D closeIcon;

        private VisualElement root;
        private VisualElement windowRoot;

        private DropdownField openFilesDropdown;
        private CodeEditorElement codeEditor;
        private Label compilationStatus;
        private Label cursorPosition;

        private List<CodeFile> codeFiles = new List<CodeFile>();
        private int lastIndex = -1;

        private bool isDragging;
        private Vector2 dragOffset;
        private EventBus _eventBus;
        public UIBlocker UIBlocker;

        // Текущие подсветщики
        private readonly PLCSyntaxHighlighter plcHighlighter = new PLCSyntaxHighlighter();
        private readonly RobotSyntaxHighlighter robotHighlighter = new RobotSyntaxHighlighter();

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            windowRoot = treeAsset.CloneTree();

            InitializeUI();
            SubscribeToEvents();

            // Заглушка: загружаем тестовые данные
            LoadTestData();
        }

        private void InitializeUI()
        {
            openFilesDropdown = windowRoot.Q<DropdownField>("openFilesDropdown");
            codeEditor = windowRoot.Q<CodeEditorElement>("codeEditor");
            compilationStatus = windowRoot.Q<Label>("compilationStatus");
            cursorPosition = windowRoot.Q<Label>("cursorPosition");

            var loadButton = windowRoot.Q<Button>("loadButton");
            var saveButton = windowRoot.Q<Button>("saveButton");
            var exportButton = windowRoot.Q<Button>("exportButton");
            var importButton = windowRoot.Q<Button>("importButton");
            var closeButton = windowRoot.Q<Button>("closeButton");

            loadButton.clicked += Load;
            saveButton.clicked += Save;
            exportButton.clicked += Export;
            importButton.clicked += Import;
            closeButton.clicked += Close;

            codeEditor.OnTextChanged += OnCodeChanged;
            codeEditor.OnCursorPositionChanged += OnCursorMoved;

            openFilesDropdown.choices = new List<string>();
            openFilesDropdown.index = -1;
            openFilesDropdown.RegisterValueChangedCallback(OnFileSelected);

            EnableDrag();
        }

        private void SubscribeToEvents()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<OpenRobotPanelSignal>(OnOpenEditor);
        }

        private void OnOpenEditor(OpenRobotPanelSignal a)
        {
            Show();
        }

        /// <summary>
        /// Загрузка тестовых данных (заглушка, пока нет реальной интеграции)
        /// </summary>
        private void LoadTestData()
        {
            // Создаём тестовый PLC файл
            var testPLCData = new PLCData();
            var plcGenerator = new PLCGenerator();
            string plcContent = plcGenerator.Generate(testPLCData);

            codeFiles.Add(new CodeFile(plcContent, CodeFileType.PLC, "ПЛК"));

            // Создаём тестовый Robot файл
            var testRobotData = new RobotProgramData("Arm1");
            var robotGenerator = new RobotGenerator();
            string robotContent = robotGenerator.Generate(testRobotData);

            codeFiles.Add(new CodeFile(robotContent, CodeFileType.Robot, "Arm1", "Arm1"));

            // Обновляем дропдаун
            UpdateDropdown();

            // Открываем первый файл
            if (codeFiles.Count > 0)
            {
                openFilesDropdown.index = 0;
                lastIndex = 0;
                LoadFileContent(0);
            }
        }

        /// <summary>
        /// Обновляет выпадающий список на основе codeFiles
        /// </summary>
        private void UpdateDropdown()
        {
            openFilesDropdown.choices.Clear();
            foreach (var file in codeFiles)
            {
                openFilesDropdown.choices.Add(file.DisplayName);
            }

            if (codeFiles.Count > 0 && openFilesDropdown.index < 0)
            {
                openFilesDropdown.index = 0;
            }
        }

        /// <summary>
        /// Загружает содержимое файла в редактор
        /// </summary>
        private void LoadFileContent(int index)
        {
            if (index < 0 || index >= codeFiles.Count) return;

            var file = codeFiles[index];
            codeEditor.SetText(file.Content);

            // Устанавливаем правильную подсветку
            if (file.Type == CodeFileType.PLC)
            {
                codeEditor.SetHighlighter(plcHighlighter);
            }
            else if (file.Type == CodeFileType.Robot)
            {
                codeEditor.SetHighlighter(robotHighlighter);
            }

            compilationStatus.text = $"Открыт: {file.DisplayName}";
            compilationStatus.RemoveFromClassList("error-status");
            compilationStatus.AddToClassList("success-status");
        }

        private void OnFileSelected(ChangeEvent<string> evt)
        {
            if (lastIndex != openFilesDropdown.index)
            {
                // Сохраняем изменения предыдущего файла
                if (lastIndex >= 0 && lastIndex < codeFiles.Count)
                {
                    codeFiles[lastIndex].Content = codeEditor.GetText();
                }

                // Загружаем выбранный файл
                if (openFilesDropdown.index >= 0 && openFilesDropdown.index < codeFiles.Count)
                {
                    LoadFileContent(openFilesDropdown.index);
                }

                lastIndex = openFilesDropdown.index;
            }
        }

        private void OnCodeChanged(string newText)
        {
            if (lastIndex >= 0 && lastIndex < codeFiles.Count)
            {
                codeFiles[lastIndex].Content = newText;
            }
        }

        private void OnCursorMoved(int line, int column)
        {
            cursorPosition.text = $"Стр: {line} \nСимв: {column}";
        }

        /// <summary>
        /// Загрузка из файловой системы (импорт)
        /// </summary>
        private void Import()
        {
            var extensionsList = new[] { new ExtensionFilter("Текстовый документ", "txt") };
            StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extensionsList, false, LoadFile);
        }

        private void LoadFile(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            if (lastIndex < 0 || lastIndex >= codeFiles.Count) return;

            string filename = Path.GetFileName(paths[0]);
            string content = File.ReadAllText(paths[0]);

            // Перезаписываем текущий файл
            codeFiles[lastIndex].Content = content;
            codeEditor.SetText(content);

            compilationStatus.text = $"Загружен: {filename} → {codeFiles[lastIndex].DisplayName}";
            compilationStatus.RemoveFromClassList("error-status");
            compilationStatus.AddToClassList("success-status");
        }

        /// <summary>
        /// Экспорт текущего файла в txt
        /// </summary>
        private void Export()
        {
            if (lastIndex < 0 || lastIndex >= codeFiles.Count) return;

            var file = codeFiles[lastIndex];
            string defaultName = file.DisplayName + ".txt";

            var extensionsList = new[] { new ExtensionFilter("Текстовый документ", "txt") };
            StandaloneFileBrowser.SaveFilePanelAsync("Сохранить как", "", defaultName, extensionsList, (string path) =>
            {
                if (string.IsNullOrEmpty(path)) return;

                File.WriteAllText(path, codeEditor.GetText());
                compilationStatus.text = $"Экспортирован: {Path.GetFileName(path)}";
                compilationStatus.RemoveFromClassList("error-status");
                compilationStatus.AddToClassList("success-status");
            });
        }

        /// <summary>
        /// Загрузка из структур основной программы (заглушка)
        /// </summary>
        private void Load()
        {
            compilationStatus.text = "Загрузка из структур пока не реализована";
            compilationStatus.AddToClassList("error-status");
            compilationStatus.RemoveFromClassList("success-status");
        }

        /// <summary>
        /// Сохранение в структуры основной программы (заглушка)
        /// </summary>
        private void Save()
        {
            if (lastIndex >= 0 && lastIndex < codeFiles.Count)
            {
                codeFiles[lastIndex].Content = codeEditor.GetText();
            }

            compilationStatus.text = "Сохранение в структуры пока не реализовано";
            compilationStatus.AddToClassList("error-status");
            compilationStatus.RemoveFromClassList("success-status");
        }

        public void Show()
        {
            if (windowRoot.parent == null)
                root.Q("overlay").Add(windowRoot);
            windowRoot.style.display = DisplayStyle.Flex;

            windowRoot.RegisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
            UIBlocker.AddNewModalWindow(windowRoot);
            UIBlocker.EnableInputMode();
        }

        private void OnWindowSizeChanged(GeometryChangedEvent evt)
        {
            var rootElement = windowRoot.Q<VisualElement>("Tablet");
            if (rootElement == null) return;

            float windowWidth = rootElement.resolvedStyle.width;
            float windowHeight = rootElement.resolvedStyle.height;
            float screenWidth = root.resolvedStyle.width;
            float screenHeight = root.resolvedStyle.height;

            windowRoot.style.left = (screenWidth - windowWidth) / 2;
            windowRoot.style.top = (screenHeight - windowHeight) / 2;
            windowRoot.UnregisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
        }

        private void EnableDrag()
        {
            var rootElement = windowRoot.Q<VisualElement>("Tablet");
            if (rootElement == null) return;

            rootElement.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    isDragging = true;
                    dragOffset = evt.mousePosition - rootElement.layout.position;
                }
            });

            rootElement.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (isDragging)
                {
                    rootElement.style.left = evt.mousePosition.x - dragOffset.x;
                    rootElement.style.top = evt.mousePosition.y - dragOffset.y;
                }
            });

            rootElement.RegisterCallback<MouseUpEvent>(evt => isDragging = false);
        }

        public void Close()
        {
            windowRoot.style.display = DisplayStyle.None;
            UIBlocker.DisableInputMode();
            UIBlocker.RemoveModalWindow(windowRoot);
        }
    }
}