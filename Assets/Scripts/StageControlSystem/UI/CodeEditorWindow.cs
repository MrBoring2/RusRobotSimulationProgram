using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.RobotPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using RobotLanguageCompiler.PLC;
using RobotLanguageCompiler.Robot;
using SFB;
using System;
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
        private UIStatusManager _uiStatusManager;

        private List<CodeFile> codeFiles = new List<CodeFile>();
        private int lastIndex = -1;

        private const int MAX_LINE_LENGTH = 65;
        private const int MAX_LINES = 999;

        private SceneObjectsManager sceneObjectsManager;

        private readonly PLCSyntaxHighlighter plcHighlighter = new PLCSyntaxHighlighter();
        private readonly RobotSyntaxHighlighter robotHighlighter = new RobotSyntaxHighlighter();

        protected override void Start()
        {
            base.Start();
            _uiStatusManager = ServiceManager.Current.Get<UIStatusManager>();
            var modalService = ServiceManager.Current.Get<ModalWindowServiceManager>();
            if (modalService != null)
            {
                modalService.RegisterWindow(this);
            }
            else
            {
                Debug.LogError("ModalWindowServiceManager не найден!");
            }
            LoadDataFromScene();
        }

        protected override void InitializeElements(VisualElement root)
        {
            base.InitializeElements(root);

            _eventBus = ServiceManager.Current.Get<EventBus>();

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

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            codeEditor.RegisterCallback<FocusInEvent>(e => _uiStatusManager.SetInputMode(true));
            codeEditor.RegisterCallback<FocusOutEvent>(e => _uiStatusManager.SetInputMode(false));
        }

        protected override void OnBeforeShow(ModalParameters parameters)
        {
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

            // Получаем ID из параметров (целевой файл для открытия)
            parameters.TryGet("FileToOpen", out string targetId);

            // синхронизируем список файлов (добавляем/удаляем роботов, обновляем имена)
            SyncFileListOnly();

            // Определяем, какой файл открыть
            int indexToOpen = DetermineFileToOpen(targetId);

            // Открываем файл
            if (indexToOpen >= 0 && indexToOpen < codeFiles.Count)
            {
                openFilesDropdown.index = indexToOpen;
                lastIndex = indexToOpen;
                LoadFileContent(indexToOpen);
            }
        }

        /// <summary>
        /// Синхронизирует ТОЛЬКО список файлов (добавляет/удаляет роботов, обновляет имена)
        /// Содержимое файлов НЕ трогает
        /// </summary>
        private void SyncFileListOnly()
        {
            // 1. Убеждаемся, что PLC файл существует (но не обновляем его содержимое)
            var plcFile = codeFiles.FirstOrDefault(f => f.Type == CodeFileType.PLC);
            if (plcFile == null)
            {
                codeFiles.Add(new CodeFile("", CodeFileType.PLC, "ПЛК"));
            }

            // 2. Получаем актуальных роботов со сцены
            var robotsOnScene = sceneObjectsManager.GetGameObjectsList()
                .Where(obj => obj.Type == ObjectType.Robot)
                .ToList();

            var robotIdsOnScene = new HashSet<string>(robotsOnScene.Select(r => r.Id));
            var robotNamesOnScene = new Dictionary<string, string>();
            foreach (var robot in robotsOnScene)
            {
                string name = robot.PropertyProvider?.Name ?? robot.Reference.name;
                robotNamesOnScene[robot.Id] = name;
            }

            // 3. Обновляем существующие файлы роботов
            var existingRobotIds = new HashSet<string>();
            for (int i = codeFiles.Count - 1; i >= 0; i--)
            {
                var file = codeFiles[i];
                if (file.Type == CodeFileType.Robot)
                {
                    existingRobotIds.Add(file.RobotId);

                    // Если робот был удалён со сцены — удаляем файл
                    if (!robotIdsOnScene.Contains(file.RobotId))
                    {
                        codeFiles.RemoveAt(i);
                        continue;
                    }

                    // Если имя робота изменилось — обновляем отображаемое имя (содержимое НЕ трогаем)
                    string currentRobotName = robotNamesOnScene[file.RobotId];
                    if (file.DisplayName != currentRobotName || file.RobotName != currentRobotName)
                    {
                        file.DisplayName = currentRobotName;
                        file.RobotName = currentRobotName;
                    }
                }
            }

            // 4. Добавляем файлы для новых роботов (с пустым содержимым)
            foreach (var robot in robotsOnScene)
            {
                if (!existingRobotIds.Contains(robot.Id))
                {
                    try
                    {
                        string robotName = robot.PropertyProvider?.Name ?? robot.Reference.name;
                        var programs = sceneObjectsManager.Commands.GetSubPrograms(robot.Id, true);
                        var robotData = RobotDataAdapter.ToCompilerData(robotName, programs);
                        var robotGenerator = new RobotGenerator();
                        string robotContent = robotGenerator.Generate(robotData);

                        codeFiles.Add(new CodeFile(robotContent, CodeFileType.Robot, robotName, robot.Id, robotName));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Ошибка загрузки данных робота {robot.Id}: {e.Message}");
                        string robotName = robot.PropertyProvider?.Name ?? robot.Reference.name;
                        codeFiles.Add(new CodeFile("", CodeFileType.Robot, robotName, robot.Id, robotName));
                    }
                }
            }

            // 5. Обновляем дропдаун
            UpdateDropdown();
        }

        /// <summary>
        /// Полностью перезаписывает список файлов и содержимое из данных сцены
        /// </summary>
        private void SyncFilesWithContent()
        {
            // Полностью перестраиваем список файлов
            var newCodeFiles = new List<CodeFile>();

            // 1. PLC файл
            var plcData = sceneObjectsManager.PLCData;
            if (plcData != null)
            {
                var plcDataWithNames = ReplaceRobotIdsWithNames(plcData);
                var plcGenerator = new PLCGenerator();
                string plcContent = plcGenerator.Generate(plcDataWithNames);
                newCodeFiles.Add(new CodeFile(plcContent, CodeFileType.PLC, "ПЛК"));
            }
            else
            {
                newCodeFiles.Add(new CodeFile("", CodeFileType.PLC, "ПЛК"));
            }

            // 2. Роботы
            var robots = sceneObjectsManager.GetGameObjectsList()
                .Where(obj => obj.Type == ObjectType.Robot)
                .ToList();

            foreach (var robot in robots)
            {
                try
                {
                    string robotName = robot.PropertyProvider?.Name ?? robot.Reference.name;
                    var programs = sceneObjectsManager.Commands.GetSubPrograms(robot.Id, true);
                    var robotData = RobotDataAdapter.ToCompilerData(robotName, programs);
                    var robotGenerator = new RobotGenerator();
                    string robotContent = robotGenerator.Generate(robotData);

                    newCodeFiles.Add(new CodeFile(robotContent, CodeFileType.Robot, robotName, robot.Id, robotName));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Ошибка загрузки данных робота {robot.Id}: {e.Message}");
                    string robotName = robot.PropertyProvider?.Name ?? robot.Reference.name;
                    newCodeFiles.Add(new CodeFile("", CodeFileType.Robot, robotName, robot.Id, robotName));
                }
            }

            codeFiles = newCodeFiles;
            UpdateDropdown();
        }

        /// <summary>
        /// Полностью перезаписывает содержимое всех файлов из данных сцены
        /// </summary>
        private void LoadDataFromScene()
        {
            if (sceneObjectsManager == null)
                sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();

            if (sceneObjectsManager == null)
            {
                compilationStatus.text = "Ошибка: SceneObjectsManager не найден";
                compilationStatus.AddToClassList("error-status");
                return;
            }

            // Сохраняем ID текущего открытого файла
            string currentFileId = (lastIndex >= 0 && lastIndex < codeFiles.Count)
                ? GetFileId(codeFiles[lastIndex])
                : null;

            // Полностью синхронизируем список и содержимое файлов
            SyncFilesWithContent();

            // Восстанавливаем открытый файл
            int indexToOpen = FindFileIndexById(currentFileId);
            if (indexToOpen < 0)
                indexToOpen = codeFiles.FindIndex(f => f.Type == CodeFileType.PLC);

            if (indexToOpen >= 0 && indexToOpen < codeFiles.Count)
            {
                openFilesDropdown.index = indexToOpen;
                lastIndex = indexToOpen;
                LoadFileContent(indexToOpen);
            }

            compilationStatus.text = "Данные загружены из сцены";
            compilationStatus.RemoveFromClassList("error-status");
            compilationStatus.AddToClassList("success-status");
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

            // Словарь для хранения ID программ всех роботов
            var allProgramIds = new Dictionary<string, Dictionary<string, string>>(); // robotId → (programName → programId)

            // 1. СНАЧАЛА СОХРАНЯЕМ ВСЕХ РОБОТОВ (чтобы получить ID их программ)
            foreach (var file in codeFiles)
            {
                if (file.Type == CodeFileType.Robot && !string.IsNullOrEmpty(file.RobotId))
                {
                    try
                    {
                        var lexer = new RobotLexer(file.Content);
                        var tokens = lexer.Tokenize();

                        if (lexer.Errors.Count > 0)
                        {
                            compilationStatus.text = $"Ошибка лексики Robot ({file.DisplayName}): {string.Join(",\n", lexer.Errors)}";
                            compilationStatus.AddToClassList("error-status");
                            compilationStatus.RemoveFromClassList("success-status");
                            return;
                        }

                        var parser = new RobotParser(tokens);
                        string robotName = file.DisplayName;
                        var robotData = parser.Parse(robotName);

                        if (parser.Errors.Count > 0)
                        {
                            compilationStatus.text = $"Ошибка парсинга Robot ({file.DisplayName}): {string.Join(",\n", parser.Errors)}";
                            compilationStatus.AddToClassList("error-status");
                            compilationStatus.RemoveFromClassList("success-status");
                            return;
                        }

                        // Обновляем данные робота и получаем словарь ID программ
                        var programIdMap = RobotDataAdapter.UpdateFromCompilerData(sceneObjectsManager, file.RobotId, robotData);
                        allProgramIds[file.RobotId] = programIdMap;
                    }
                    catch (Exception e)
                    {
                        compilationStatus.text = $"Ошибка парсинга Robot ({file.DisplayName}): {e.Message}";
                        compilationStatus.AddToClassList("error-status");
                        compilationStatus.RemoveFromClassList("success-status");
                        return;
                    }
                }
            }

            // 2. ТЕПЕРЬ СОХРАНЯЕМ PLC (с уже известными ID программ роботов)
            foreach (var file in codeFiles)
            {
                if (file.Type == CodeFileType.PLC)
                {
                    try
                    {
                        var lexer = new PLCLexer(file.Content);
                        var tokens = lexer.Tokenize();

                        if (lexer.Errors.Count > 0)
                        {
                            compilationStatus.text = $"Ошибка лексики PLC ({file.DisplayName}): {string.Join(",\n", lexer.Errors)}";
                            compilationStatus.AddToClassList("error-status");
                            compilationStatus.RemoveFromClassList("success-status");
                            return;
                        }

                        var parser = new PLCParser(tokens);
                        var plcDataWithNames = parser.Parse();

                        if (parser.Errors.Count > 0)
                        {
                            compilationStatus.text = $"Ошибка парсинга PLC: {string.Join(",\n", parser.Errors)}";
                            compilationStatus.AddToClassList("error-status");
                            compilationStatus.RemoveFromClassList("success-status");
                            return;
                        }

                        // Заменяем имена роботов на ID
                        var plcDataWithIds = ReplaceRobotNamesWithIds(plcDataWithNames);
                        if (plcDataWithIds == null)
                        {
                            return;
                        }

                        // Заполняем ProgramId в командах StartProgram
                        FillProgramIds(plcDataWithIds, allProgramIds);

                        sceneObjectsManager.SetPLCData(plcDataWithIds);
                        break;
                    }
                    catch (Exception e)
                    {
                        compilationStatus.text = $"Ошибка парсинга PLC: {e.Message}";
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
        /// Заполняет ProgramId в командах StartProgram на основе имён программ
        /// </summary>
        private void FillProgramIds(PLCData plcData, Dictionary<string, Dictionary<string, string>> allProgramIds)
        {
            // Обрабатываем блоки роботов
            foreach (var robotBlock in plcData.RobotCommandsBlockItems)
            {
                // Для каждого блока робота знаем его ID
                string robotId = robotBlock.RobotId;

                // Получаем словарь ID программ для этого робота
                if (allProgramIds.TryGetValue(robotId, out var programIdMap))
                {
                    FillProgramIdsInConditions(robotBlock.ConditionsList, programIdMap);
                }
            }
        }

        /// <summary>
        /// Рекурсивно обходит условия и заполняет ProgramId
        /// </summary>
        private void FillProgramIdsInConditions(List<PLCBase> items, Dictionary<string, string> programIdMap)
        {
            foreach (var item in items)
            {
                if (item is PLCBlockCondition blockCond)
                {
                    FillProgramIdsInCondition(blockCond.IfCondition, programIdMap);
                    foreach (var elif in blockCond.ElifConditions)
                    {
                        FillProgramIdsInCondition(elif, programIdMap);
                    }
                    FillProgramIdsInCondition(blockCond.ElseCondition, programIdMap);
                }
                else if (item is PLCCondition condition)
                {
                    FillProgramIdsInCondition(condition, programIdMap);
                }
            }
        }

        /// <summary>
        /// Заполняет ProgramId в одной ветке условия
        /// </summary>
        private void FillProgramIdsInCondition(PLCCondition condition, Dictionary<string, string> programIdMap)
        {
            if (condition == null) return;

            foreach (var content in condition.Content)
            {
                if (content is PLCStartProgram startProgram)
                {
                    if (programIdMap.TryGetValue(startProgram.ProgramName, out string programId))
                    {
                        startProgram.ProgramId = programId;
                    }
                }
                else if (content is PLCBlockCondition nestedBlock)
                {
                    FillProgramIdsInCondition(nestedBlock.IfCondition, programIdMap);
                    foreach (var elif in nestedBlock.ElifConditions)
                    {
                        FillProgramIdsInCondition(elif, programIdMap);
                    }
                    FillProgramIdsInCondition(nestedBlock.ElseCondition, programIdMap);
                }
            }
        }

        /// <summary>
        /// Определяет, какой файл нужно открыть
        /// </summary>
        private int DetermineFileToOpen(string targetId)
        {
            // Если передан targetId и есть соответствующий файл — открываем его
            if (!string.IsNullOrEmpty(targetId))
            {
                int targetIndex = FindFileIndexById(targetId);
                if (targetIndex >= 0)
                {
                    return targetIndex;
                }
            }

            // Иначе открываем ПЛК
            int plcIndex = codeFiles.FindIndex(f => f.Type == CodeFileType.PLC);
            return plcIndex >= 0 ? plcIndex : 0;
        }

        /// <summary>
        /// Находит индекс файла по ID (для роботов — RobotId, для PLC — "PLC")
        /// </summary>
        private int FindFileIndexById(string id)
        {
            if (id == "PLC")
            {
                return codeFiles.FindIndex(f => f.Type == CodeFileType.PLC);
            }
            return codeFiles.FindIndex(f => f.RobotId == id);
        }

        /// <summary>
        /// Возвращает ID файла (для роботов — RobotId, для PLC — "PLC")
        /// </summary>
        private string GetFileId(CodeFile file)
        {
            if (file.Type == CodeFileType.PLC)
                return "PLC";
            return file.RobotId;
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
                robotId = robot.PropertyProvider?.Name ?? robot.Reference.name;
            }
            return robotId;
        }

        /// <summary>
        /// Преобразует имя робота в ID (обратное преобразование для PLC)
        /// </summary>
        private string GetRobotIdByName(string robotName)
        {
            if (string.IsNullOrEmpty(robotName)) return robotName;

            var robots = sceneObjectsManager?.GetGameObjectsList()
                .Where(obj => obj.Type == ObjectType.Robot)
                .ToList();

            if (robots != null)
            {
                foreach (var robot in robots)
                {
                    string name = robot.PropertyProvider?.Name ?? robot.Reference.name;
                    if (name == robotName)
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

            if (file.Type == CodeFileType.PLC)
            {
                codeEditor.SetText(file.Content, plcHighlighter);
            }
            else
            {
                codeEditor.SetText(file.Content, robotHighlighter);
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

                string[] lines = content.Split('\n');
                int linesN = lines.Length;

                foreach (string line in lines)
                {
                    linesN += line.Length / MAX_LINE_LENGTH;
                }

                if (linesN > MAX_LINES)
                {
                    compilationStatus.text = $"Количество строк в загружаемом файле {lines} превышает допустимое {MAX_LINES}";
                    compilationStatus.RemoveFromClassList("success-status");
                    compilationStatus.AddToClassList("error-status");
                    return;
                }

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