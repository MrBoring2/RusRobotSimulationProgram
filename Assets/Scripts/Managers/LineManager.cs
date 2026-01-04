using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class LineData : MonoBehaviour
    {
        public string FromId;
        public string ToId;
    }
    public class LineManager : MonoBehaviour, IService
    {

        [SerializeField] private Material lineMaterial;
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private Color lineColor = Color.cyan;

        private EventBus _eventBus;
        private SceneObjectsManager _sceneManager;
        private List<LineRenderer> _activeLines = new List<LineRenderer>();
        private string _currentProgramId;
        private bool _isDrawing = false;

        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _sceneManager = ServiceManager.Current.Get<SceneObjectsManager>();

            _eventBus.Subscribe<StartLineDrawer>(OnDrawProgramRoute);
            _eventBus.Subscribe<StopLineDrawer>(OnStopDrawingRoute);
            _eventBus.Subscribe<UpdateLineDrawer>(OnUpdateRawingRoute);
        }

        private void OnUpdateRawingRoute(UpdateLineDrawer drawer)
        {
            if(_isDrawing)
                DrawProgramRoute(_currentProgramId);
        }

        void OnDrawProgramRoute(StartLineDrawer signal)
        {
            Debug.Log($"LineManager: Получен сигнал StartLineDrawer для программы {signal.ProgramId}");

            ClearAllLines();
            _currentProgramId = signal.ProgramId;
            _isDrawing = true;

            // Сразу рисуем маршрут
            DrawProgramRoute(_currentProgramId);

            Debug.Log($"LineManager: Отрисовано {_activeLines.Count} линий");
        }

        void OnStopDrawingRoute(StopLineDrawer signal)
        {
            ClearAllLines();
            _currentProgramId = null;
            _isDrawing = false;
        }

        void DrawProgramRoute(string programId)
        {
            // Получаем ВСЕХ детей программы (команды + подпрограммы)
            var allChildren = GetChildrenOfProgram(programId);

            Debug.Log($"LineManager: Детей у программы {programId}: {allChildren.Count}");

            // Разделяем на секции
            var sections = new List<List<string>>();
            var currentSection = new List<string>();

            foreach (var child in allChildren)
            {
                if (child.Type == ObjectType.LinearMoveCommand)
                {
                    // Добавляем команду в текущую секцию
                    currentSection.Add(child.Id);
                }
                else if (child.Type == ObjectType.Program)
                {
                    // Встретили подпрограмму - завершаем текущую секцию
                    if (currentSection.Count > 0)
                    {
                        sections.Add(new List<string>(currentSection));
                        currentSection.Clear();
                    }
                    // Подпрограмму пропускаем (не рисуем к ней линии)
                }
            }

            // Добавляем последнюю секцию
            if (currentSection.Count > 0)
            {
                sections.Add(new List<string>(currentSection));
            }

            Debug.Log($"LineManager: Секций маршрута: {sections.Count}");

            // Рисуем каждую секцию
            foreach (var section in sections)
            {
                if (section.Count >= 2)
                {
                    for (int i = 0; i < section.Count - 1; i++)
                    {
                        CreateLineBetweenCommands(section[i], section[i + 1]);
                    }
                }
            }
        }

        // Получаем детей программы в правильном порядке
        List<SceneObject> GetChildrenOfProgram(string programId)
        {
            var children = new List<SceneObject>();

            if (_sceneManager?.Items == null) return children;

            // Получаем Items как OrderedDictionary
            var itemsDict = _sceneManager.Items;
            if (itemsDict == null) return children;

            // Перебираем в порядке добавления
            foreach (DictionaryEntry entry in itemsDict)
            {
                var obj = entry.Value as SceneObject;
                if (obj != null && obj.ParentId == programId)
                {
                    children.Add(obj);
                    Debug.Log($"  Ребёнок: {obj.Id}, Тип: {obj.Type}");
                }
            }
            //// Так как Items отсортирован, дети программы идут подряд
            //foreach (var kvp in _sceneManager.Items.Where(p => p.Value.ParentId == programId))
            //{
            //    var obj = kvp.Value;
            //    if (obj.ParentId == programId)
            //    {
            //        children.Add(obj);
            //        Debug.Log($"  Ребёнок: {obj.Id}, Тип: {obj.Type}");
            //    }
            //}

            return children;
        }

        void CreateLineBetweenCommands(string fromId, string toId)
        {
            // Получаем объекты из менеджера
            var fromObj = _sceneManager.GetById(fromId);
            var toObj = _sceneManager.GetById(toId);

            if (fromObj == null || toObj == null)
            {
                Debug.LogError($"LineManager: Не найдены объекты для линии {fromId} -> {toId}");
                return;
            }

            // Создаём GameObject для линии
            GameObject lineObj = new GameObject($"Line_{fromId}_to_{toId}");
            lineObj.transform.SetParent(transform); // Делаем дочерним от LineManager

            // Добавляем LineRenderer
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;

            // Устанавливаем позиции
            lr.SetPosition(0, fromObj.Reference.transform.position);
            lr.SetPosition(1, toObj.Reference.transform.position);

            // Сохраняем IDs для обновления позиций
            var lineData = lineObj.AddComponent<LineData>();
            lineData.FromId = fromId;
            lineData.ToId = toId;

            _activeLines.Add(lr);

            Debug.Log($"LineManager: Создана линия {fromId} -> {toId}");
        }

        void UpdateLinePosition(LineRenderer line, SceneObject fromObj, SceneObject toObj)
        {
            if (line == null || fromObj == null || toObj == null) return;

            line.SetPosition(0, fromObj.Reference.transform.position);
            line.SetPosition(1, toObj.Reference.transform.position);
        }

        void Update()
        {
            if (!_isDrawing || string.IsNullOrEmpty(_currentProgramId)) return;

            // Обновляем позиции всех линий в реальном времени
            foreach (var line in _activeLines)
            {
                if (line == null) continue;

                var lineData = line.GetComponent<LineData>();
                if (lineData == null) continue;

                var fromObj = _sceneManager.GetById(lineData.FromId);
                var toObj = _sceneManager.GetById(lineData.ToId);

                UpdateLinePosition(line, fromObj, toObj);
            }
        }

        List<string> GetCommandsInProgram(string programId)
        {
            var commands = new List<string>();

            if (_sceneManager == null || _sceneManager.Items == null)
                return commands;

            var itemsDict = _sceneManager.Items as OrderedDictionary;
            if (itemsDict == null) return commands;

            // Перебираем в порядке добавления
            foreach (DictionaryEntry entry in itemsDict)
            {
                var obj = entry.Value as SceneObject;
                if (obj != null &&
                    obj.ParentId == programId &&
                    (obj.Type == ObjectType.LinearMoveCommand ||
                     obj.Type == ObjectType.StateEndEffectorCommand))
                {
                    commands.Add(obj.Id);
                }
            }

            return commands;
        }

        void ClearAllLines()
        {
            foreach (var line in _activeLines)
            {
                if (line != null && line.gameObject != null)
                    Destroy(line.gameObject);
            }
            _activeLines.Clear();
        }

        void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubcribe<StartLineDrawer>(OnDrawProgramRoute);
                _eventBus.Unsubcribe<StopLineDrawer>(OnStopDrawingRoute);
            }
        }

        public bool IsCommandInCurrentProgram(string commandId)
        {
            if (string.IsNullOrEmpty(_currentProgramId)) return false;

            var command = _sceneManager.GetById(commandId);
            if (command != null)
            {
                // Проверяем, принадлежит ли команда текущей программе
                return command.ParentId == _currentProgramId;
            }

            return false;
        }

        public string GetCurrentProgramId() => _currentProgramId;
    }
}
