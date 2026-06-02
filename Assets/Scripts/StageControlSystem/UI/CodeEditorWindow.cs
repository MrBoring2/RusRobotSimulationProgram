using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.RobotPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using RobotLanguageCompiler.PLC;
using RobotLanguageCompiler.Robot;
using SFB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.UI.CodeEditor
{
    public class CodeEditorWindow : BaseModalWindow
    {
        private DropdownField openFilesDropdown;
        private CodeEditorElement codeEditor;
        private Label compilationStatus;
        private Label cursorPosition;

        private List<CodeFile> codeFiles = new List<CodeFile>();
        private int lastIndex = -1;

        private SceneObjectsManager sceneObjectsManager;

        private readonly PLCSyntaxHighlighter plcHighlighter = new PLCSyntaxHighlighter();
        private readonly RobotSyntaxHighlighter robotHighlighter = new RobotSyntaxHighlighter();

        protected override void Start()
        {
            base.Start();
            // Регистрируем окно в ModalWindowServiceManager
            var modalService = ServiceManager.Current.Get<ModalWindowServiceManager>();
            if (modalService != null)
            {
                modalService.RegisterWindow(this);
            }
            else
            {
                Debug.LogError("ModalWindowServiceManager не найден!");
            }

           
        }

        protected override void InitializeElements(VisualElement root)
        {
            base.InitializeElements(root);

            _eventBus = ServiceManager.Current.Get<EventBus>();

            // Ищем элементы в windowRoot
            openFilesDropdown = windowRoot.Q<DropdownField>("openFilesDropdown");
            codeEditor = windowRoot.Q<CodeEditorElement>("codeEditor");
            compilationStatus = windowRoot.Q<Label>("compilationStatus");
            cursorPosition = windowRoot.Q<Label>("cursorPosition");

            var loadButton = windowRoot.Q<Button>("loadButton");
            var saveButton = windowRoot.Q<Button>("saveButton");
            var importButton = windowRoot.Q<Button>("importButton");
            var exportButton = windowRoot.Q<Button>("exportButton");
            var closeButton = windowRoot.Q<Button>("closeButton");

            if (loadButton != null)
                loadButton.clicked += LoadDataFromScene;
            if (saveButton != null)
                saveButton.clicked += Save;
            if (importButton != null)
                importButton.clicked += Import;
            if (exportButton != null)
                exportButton.clicked += Export;
            if (closeButton != null)
                closeButton.clicked += () => Hide();

            codeEditor.OnTextChanged += OnCodeChanged;
            codeEditor.OnCursorPositionChanged += OnCursorMoved;

            openFilesDropdown.choices = new List<string>();
            openFilesDropdown.index = -1;
            openFilesDropdown.RegisterValueChangedCallback(OnFileSelected);
        }

        protected override void OnBeforeShow(ModalParameters parameters)
        {
            // Получаем менеджеры
            sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            if (sceneObjectsManager == null)
            {
                Debug.LogError("SceneObjectsManager не найден!");
                if (compilationStatus != null)
                {
                    compilationStatus.text = "Ошибка: SceneObjectsManager не найден";
                    compilationStatus.AddToClassList("error-status");
                }
                return;
            }

            // Загружаем данные из сцены
            LoadDataFromScene();
        }

        private void LoadDataFromScene()
        {
            codeFiles.Clear();

            // 1. Загружаем PLC данные с ПОДМЕНОЙ ID на имена
            var plcData = sceneObjectsManager.PLCData;
            if (plcData != null)
            {
                // ПОДМЕНА: ID роботов → имена для генератора
                var plcDataWithNames = ReplaceRobotIdsWithNames(plcData);

                var plcGenerator = new PLCGenerator();
                string plcContent = plcGenerator.Generate(plcDataWithNames);
                codeFiles.Add(new CodeFile(plcContent, CodeFileType.PLC, "ПЛК"));
            }
            else
            {
                Debug.LogWarning("PLCData не найден, создаём пустой");
                codeFiles.Add(new CodeFile("", CodeFileType.PLC, "ПЛК"));
            }

            // 2. Загружаем данные роботов
            var robots = sceneObjectsManager.GetGameObjectsList()
                .Where(obj => obj.Type == ObjectType.Robot)
                .ToList();

            foreach (var robot in robots)
            {
                try
                {
                    var programs = sceneObjectsManager.Commands.GetSubPrograms(robot.Id, false);
                    string robotName = robot.PropertyProvider?.Name ?? robot.Reference.name;
                    var robotData = RobotDataAdapter.ToCompilerData(robotName, programs);

                    var robotGenerator = new RobotGenerator();
                    string robotContent = robotGenerator.Generate(robotData);

                    codeFiles.Add(new CodeFile(robotContent, CodeFileType.Robot, robotName, robot.Id, robotName));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Ошибка загрузки данных робота {robot.Id}: {e.Message}");
                    string robotName = robot.PropertyProvider?.Name ?? robot.Reference.name;
                    codeFiles.Add(new CodeFile("", CodeFileType.Robot, robotName, robot.Id, robotName));
                }
            }

            UpdateDropdown();

            if (codeFiles.Count > 0)
            {
                openFilesDropdown.index = 0;
                lastIndex = 0;
                LoadFileContent(0);
            }
        }

        private void SaveDataToScene()
        {
            if (sceneObjectsManager == null)
                sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();

            if (sceneObjectsManager == null)
            {
                compilationStatus.text = "Ошибка: SceneObjectsManager не найден";
                compilationStatus.AddToClassList("error-status");
                return;
            }

            foreach (var file in codeFiles)
            {
                if (file.Type == CodeFileType.PLC)
                {
                    // Парсим PLC
                    try
                    {
                        var lexer = new PLCLexer(file.Content);
                        var tokens = lexer.Tokenize();
                        var parser = new PLCParser(tokens);
                        var plcDataWithNames = parser.Parse();

                        if (parser.Errors.Count > 0)
                        {
                            compilationStatus.text = $"Ошибка парсинга PLC: {string.Join(", ", parser.Errors)}";
                            compilationStatus.AddToClassList("error-status");
                            compilationStatus.RemoveFromClassList("success-status");
                            return;
                        }

                        // ПОДМЕНА: имена роботов → ID для сохранения в SceneObjectsManager
                        var plcDataWithIds = ReplaceRobotNamesWithIds(plcDataWithNames);

                        // Если преобразование вернуло null - значит робот не найден, прерываем сохранение
                        if (plcDataWithIds == null)
                        {
                            // compilationStatus уже установлен в ReplaceRobotNamesWithIds
                            return;
                        }

                        sceneObjectsManager.SetPLCData(plcDataWithIds);
                    }
                    catch (System.Exception e)
                    {
                        compilationStatus.text = $"Ошибка парсинга PLC: {e.Message}";
                        compilationStatus.AddToClassList("error-status");
                        compilationStatus.RemoveFromClassList("success-status");
                        return;
                    }
                }
                else if (file.Type == CodeFileType.Robot && !string.IsNullOrEmpty(file.RobotId))
                {
                    // Парсим Robot
                    try
                    {
                        var lexer = new RobotLexer(file.Content);
                        var tokens = lexer.Tokenize();

                        if (lexer.Errors.Count > 0)
                        {
                            compilationStatus.text = $"Ошибка лексики Robot: {string.Join(", ", lexer.Errors)}";
                            compilationStatus.AddToClassList("error-status");
                            compilationStatus.RemoveFromClassList("success-status");
                            return;
                        }

                        var parser = new RobotParser(tokens);
                        string robotName = file.DisplayName;
                        var robotData = parser.Parse(robotName);

                        if (parser.Errors.Count > 0)
                        {
                            compilationStatus.text = $"Ошибка парсинга Robot: {string.Join(", ", parser.Errors)}";
                            compilationStatus.AddToClassList("error-status");
                            compilationStatus.RemoveFromClassList("success-status");
                            return;
                        }

                        RobotDataAdapter.UpdateFromCompilerData(sceneObjectsManager, file.RobotId, robotData);
                    }
                    catch (System.Exception e)
                    {
                        compilationStatus.text = $"Ошибка парсинга Robot: {e.Message}";
                        compilationStatus.AddToClassList("error-status");
                        compilationStatus.RemoveFromClassList("success-status");
                        return;
                    }
                }
            }

            compilationStatus.text = "Сохранено успешно";
            compilationStatus.RemoveFromClassList("error-status");
            compilationStatus.AddToClassList("success-status");
            _eventBus.Invoke(new UpdatePLCData());
        }

        /// <summary>
        /// Преобразует ID робота в имя для отображения в коде PLC
        /// </summary>
        private string GetRobotName(string robotId)
        {
            if (string.IsNullOrEmpty(robotId)) return robotId;

            var robot = sceneObjectsManager?.GetById(robotId);
            if (robot != null)
            {
                string name = robot.PropertyProvider?.Name ?? robot.Reference.name;
                // Заменяем пробелы на подчёркивания для совместимости с компилятором
                return name.Replace(" ", "_");
            }
            return robotId;
        }

        /// <summary>
        /// Преобразует имя робота в ID (обратное преобразование для PLC)
        /// </summary>
        private string GetRobotIdByName(string robotName)
        {
            if (string.IsNullOrEmpty(robotName)) return robotName;

            // Восстанавливаем исходное имя (заменяем _ обратно на пробелы)
            string originalName = robotName.Replace("_", " ");

            var robots = sceneObjectsManager?.GetGameObjectsList()
                .Where(obj => obj.Type == ObjectType.Robot)
                .ToList();

            if (robots != null)
            {
                foreach (var robot in robots)
                {
                    string name = robot.PropertyProvider?.Name ?? robot.Reference.name;
                    if (name == originalName)
                    {
                        return robot.Id;
                    }
                }
            }
            return null; // Возвращаем null, если робот не найден
        }

        /// <summary>
        /// Рекурсивно заменяет ID роботов на имена в PLCData
        /// </summary>
        private PLCData ReplaceRobotIdsWithNames(PLCData data)
        {
            if (data == null) return data;

            var newData = new PLCData
            {
                Id = data.Id,
                InitBlockItems = data.InitBlockItems,
                Variables = data.Variables,
                LogicBlockItems = data.LogicBlockItems
            };

            newData.RobotCommandsBlockItems = new List<PLCRobotBlock>();
            foreach (var robotBlock in data.RobotCommandsBlockItems)
            {
                string robotName = GetRobotName(robotBlock.RobotId);
                var newBlock = new PLCRobotBlock(robotName);
                newBlock.ConditionsList = robotBlock.ConditionsList;
                newData.RobotCommandsBlockItems.Add(newBlock);
            }

            return newData;
        }

        /// <summary>
        /// Рекурсивно заменяет имена роботов на ID в PLCData
        /// </summary>
        /// <returns>PLCData с ID, или null если какой-то робот не найден</returns>
        private PLCData ReplaceRobotNamesWithIds(PLCData data)
        {
            if (data == null) return data;

            var newData = new PLCData
            {
                Id = data.Id,
                InitBlockItems = data.InitBlockItems,
                Variables = data.Variables,
                LogicBlockItems = data.LogicBlockItems
            };

            newData.RobotCommandsBlockItems = new List<PLCRobotBlock>();

            foreach (var robotBlock in data.RobotCommandsBlockItems)
            {
                string robotId = GetRobotIdByName(robotBlock.RobotId);

                if (string.IsNullOrEmpty(robotId))
                {
                    // Робот не найден - показываем ошибку и возвращаем null
                    compilationStatus.text = $"Ошибка: робот с именем '{robotBlock.RobotId}' не найден на сцене. Сохранение отменено.";
                    compilationStatus.AddToClassList("error-status");
                    compilationStatus.RemoveFromClassList("success-status");
                    return null;
                }

                var newBlock = new PLCRobotBlock(robotId);
                newBlock.ConditionsList = robotBlock.ConditionsList;
                newData.RobotCommandsBlockItems.Add(newBlock);
            }

            return newData;
        }

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

        private void LoadFileContent(int index)
        {
            if (index < 0 || index >= codeFiles.Count) return;

            var file = codeFiles[index];
            codeEditor.SetText(file.Content);

            // Устанавливаем подсветку
            if (file.Type == CodeFileType.PLC)
            {
                codeEditor.SetHighlighter(plcHighlighter);
            }
            else
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
            cursorPosition.text = $"Стр: {line}\nСимв: {column}";
        }

        private void Import()
        {
            if (lastIndex < 0 || lastIndex >= codeFiles.Count) return;

            var extensionsList = new[] { new ExtensionFilter("Текстовый документ", "txt") };
            StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extensionsList, false, (string[] paths) =>
            {
                if (paths == null || paths.Length == 0) return;

                string filename = Path.GetFileName(paths[0]);
                string content = File.ReadAllText(paths[0]);

                // Нормализуем окончания строк
                content = content.Replace("\r\n", "\n").Replace("\r", "");

                codeFiles[lastIndex].Content = content;
                codeEditor.SetText(content);

                compilationStatus.text = $"Импортирован: {filename} → {codeFiles[lastIndex].DisplayName}";
                compilationStatus.RemoveFromClassList("error-status");
                compilationStatus.AddToClassList("success-status");
            });
        }

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

        // Кнопка Save
        public void Save()
        {
            // Сохраняем текущий текст в файл
            if (lastIndex >= 0 && lastIndex < codeFiles.Count)
            {
                codeFiles[lastIndex].Content = codeEditor.GetText();
            }

            // Сохраняем в структуры сцены
            SaveDataToScene();
        }

        // Метод для открытия окна через ModalWindowServiceManager
        public void Open()
        {
            var modalService = ServiceManager.Current.Get<ModalWindowServiceManager>();
            if (modalService != null)
            {
                modalService.ShowWindow(WindowId, "", new ModalParameters());
            }
            else
            {
                Debug.LogError("ModalWindowServiceManager не найден!");
                // Fallback: вызываем Show напрямую
                Show("", new ModalParameters());
            }
        }

        // Для тестов: метод, который можно вызвать из кнопки в инспекторе
        [ContextMenu("OpenTestWindow")]
        public void OpenTestWindow()
        {
            Open();
        }
    }
}