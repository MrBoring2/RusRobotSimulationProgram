using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.RobotPanel;
using Assets.Scripts.CustomServiceManager;
using SFB;

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
        
        private List<string> txts = new List<string>();
        private List<string> filePaths = new List<string>();
        private int lastIndex = -1;
        private bool isDragging;
        private Vector2 dragOffset;
        private EventBus _eventBus;
        public UIBlocker UIBlocker;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            windowRoot = treeAsset.CloneTree();

            InitializeUI();
            SubscribeToEvents();
        }
        
        private void InitializeUI()
        {
            // Получаем элементы
            openFilesDropdown = windowRoot.Q<DropdownField>("openFilesDropdown");
            codeEditor = windowRoot.Q<CodeEditorElement>("codeEditor");
            compilationStatus = windowRoot.Q<Label>("compilationStatus");
            cursorPosition = windowRoot.Q<Label>("cursorPosition");
            
            var loadButton = windowRoot.Q<Button>("loadButton");
            var saveButton = windowRoot.Q<Button>("saveButton");
            var exportButton = windowRoot.Q<Button>("exportButton");
            var importButton = windowRoot.Q<Button>("importButton");
            var closeButton = windowRoot.Q<Button>("closeButton");
            
            // Подписываем кнопки
            loadButton.clicked += Load;
            saveButton.clicked += Save;
            exportButton.clicked += Export;
            importButton.clicked += Import;
            closeButton.clicked += Close;
            
            // Подписываемся на события редактора
            codeEditor.OnTextChanged += OnCodeChanged;
            codeEditor.OnCursorPositionChanged += OnCursorMoved;
            
            // Настраиваем выпадающий список
            openFilesDropdown.choices = new List<string>();
            openFilesDropdown.index = -1;
            openFilesDropdown.RegisterValueChangedCallback(OnFileSelected);
            
            // Создаем начальный пустой файл
            CreateNewUnsavedFile();
            
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
        
        private void CreateNewUnsavedFile()
        {
            txts.Add("");
            filePaths.Add(null);
            openFilesDropdown.choices.Add("[Новый файл]");
            openFilesDropdown.index = openFilesDropdown.choices.Count - 1;
            lastIndex = openFilesDropdown.index;
            codeEditor.SetText("");
        }
        
        private void OnFileSelected(ChangeEvent<string> evt)
        {
            if (lastIndex != openFilesDropdown.index)
            {
                // Сохраняем изменения предыдущего файла
                if (lastIndex >= 0 && lastIndex < txts.Count)
                {
                    txts[lastIndex] = codeEditor.GetText();
                }
                
                // Загружаем выбранный файл
                if (openFilesDropdown.index >= 0 && openFilesDropdown.index < txts.Count)
                {
                    codeEditor.SetText(txts[openFilesDropdown.index]);
                }
                
                lastIndex = openFilesDropdown.index;
            }
        }
        
        private void OnCodeChanged(string newText)
        {
            if (lastIndex >= 0 && lastIndex < txts.Count)
            {
                txts[lastIndex] = newText;
            }
        }
        
        private void OnCursorMoved(int line, int column)
        {
            cursorPosition.text = $"Стр: {line} \nСимв: {column}";
        }
        
        private void Import()
        {
            var extensionsList = new[] { new ExtensionFilter("Текстовый документ", "txt") };
            StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extensionsList, false, LoadFile);
        }
        
        private void LoadFile(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            
            string filename = Path.GetFileName(paths[0]);
            string content = File.ReadAllText(paths[0]);
            
            if (!openFilesDropdown.choices.Contains(filename))
            {
                openFilesDropdown.choices.Add(filename);
                txts.Add(content);
                filePaths.Add(paths[0]);
                openFilesDropdown.index = openFilesDropdown.choices.Count - 1;
            }
            else
            {
                int index = openFilesDropdown.choices.IndexOf(filename);
                openFilesDropdown.index = index;
                txts[index] = content;
                filePaths[index] = paths[0];
            }
            
            if (openFilesDropdown.index == lastIndex)
            {
                codeEditor.SetText(txts[openFilesDropdown.index]);
            }
            
            compilationStatus.text = $"Файл загружен: {filename}";
            compilationStatus.RemoveFromClassList("error-status");
            compilationStatus.AddToClassList("success-status");
        }
        
        //private void SaveFile()
        //{
        //    if (filePaths.Count > lastIndex && !string.IsNullOrEmpty(filePaths[lastIndex]))
        //    {
        //        File.WriteAllText(filePaths[lastIndex], codeEditor.GetText());
        //        compilationStatus.text = $"Сохранено: {Path.GetFileName(filePaths[lastIndex])}";
        //        compilationStatus.RemoveFromClassList("error-status");
        //        compilationStatus.AddToClassList("success-status");
        //    }
        //    else
        //    {
        //        Export();
        //    }
        //}
        
        private void Export()
        {
            var extensionsList = new[] { new ExtensionFilter("Текстовый документ", "txt") };
            StandaloneFileBrowser.SaveFilePanelAsync("Сохранить как", "", "program", extensionsList, (string path) =>
            {
                if (string.IsNullOrEmpty(path)) return;
                
                File.WriteAllText(path, codeEditor.GetText());
                string filename = Path.GetFileName(path);
                
                if (lastIndex >= 0 && openFilesDropdown.choices[lastIndex] == "[Новый файл]")
                {
                    // Заменяем безымянный файл
                    openFilesDropdown.choices[lastIndex] = filename;
                    filePaths[lastIndex] = path;
                    openFilesDropdown.index = lastIndex;
                    openFilesDropdown.SetValueWithoutNotify(filename);
                }
                else
                {
                    // Создаем новую запись
                    openFilesDropdown.choices.Add(filename);
                    txts.Add(codeEditor.GetText());
                    filePaths.Add(path);
                    openFilesDropdown.index = openFilesDropdown.choices.Count - 1;
                }
                
                compilationStatus.text = $"Сохранено как: {filename}";
                compilationStatus.RemoveFromClassList("error-status");
                compilationStatus.AddToClassList("success-status");
            });
        }

        private void Load()
        {
            compilationStatus.text = "Load is not yet implemented";
        }

        private void Save()
        {
            compilationStatus.text = "Save is not yet implemented";
        }
        
        //private void CompileCode()
        //{
        //    string code = codeEditor.GetText();
        //    compilationStatus.text = "Компиляция...";
        //    compilationStatus.RemoveFromClassList("error-status");
        //    compilationStatus.RemoveFromClassList("success-status");
            
        //    // Здесь вызываем ваш компилятор
        //    try
        //    {
        //        // var result = YourCompiler.Compile(code);
        //        // if (result.HasErrors)
        //        // {
        //        //     compilationStatus.text = $"Ошибка: {result.Errors[0].Message} (строка {result.Errors[0].Line})";
        //        //     compilationStatus.AddToClassList("error-status");
        //        // }
        //        // else
        //        // {
        //        //     compilationStatus.text = "Компиляция успешна!";
        //        //     compilationStatus.AddToClassList("success-status");
        //        //     // Отправляем команды роботу через EventBus
        //        // }
                
        //        // Временная заглушка
        //        compilationStatus.text = "Компиляция: временно отключена (интегрируйте ваш компилятор)";
        //    }
        //    catch (System.Exception e)
        //    {
        //        compilationStatus.text = $"Ошибка компиляции: {e.Message}";
        //        compilationStatus.AddToClassList("error-status");
        //    }
        //}
        
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