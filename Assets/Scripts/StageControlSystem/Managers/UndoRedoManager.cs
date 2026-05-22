using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Менеджер системы Undo/Redo (отмена/повтор действий).
    /// Реализует паттерн Command для отслеживания и отката действий пользователя.
    /// Позволяет записывать операции, отменять их и повторять.
    /// Реализует интерфейс IService для интеграции с ServiceManager.
    /// </summary>
    public class UndoRedoManager : MonoBehaviour, IService
    {
        /// <summary>
        /// Флаг, указывающий, выполняется ли запись действий в историю.
        /// True - действия записываются, False - запись приостановлена.
        /// Используется для внешних операций, которые не должны попадать в историю.
        /// </summary>
        public bool IsRecording { get; private set; } = true;
        private EventBus _eventBus;
        /// <summary>
        /// Стек для хранения выполненных команд (история действий).
        /// При отмене (Undo) команды извлекаются из этого стека.
        /// </summary>
        private readonly Stack<ICommand> undoStack = new();
        /// <summary>
        /// Стек для хранения отмененных команд.
        /// При повторе (Redo) команды извлекаются из этого стека.
        /// </summary>
        private readonly Stack<ICommand> redoStack = new();
        /// <summary>
        /// Инициализация менеджера Undo/Redo.
        /// Получает ссылку на EventBus через ServiceManager и подписывается на события.
        /// </summary>
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<ClearSceneSignal>(OnClearScene);
        }

        /// <summary>
        /// Обработчик события очистки сцены.
        /// Полностью очищает историю действий, так как объекты на сцене были удалены.
        /// </summary>
        /// <param name="s">Сигнал очистки сцены</param>
        private void OnClearScene(ClearSceneSignal s)
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        /// <summary>
        /// Выполняет команду и добавляет её в историю.
        /// Это основной метод для регистрации действий пользователя.
        /// </summary>
        /// <param name="command">Команда, которая должна быть выполнена</param>
        /// <remarks>
        /// Процесс выполнения:
        /// 1. При выполнении новой команды стек Redo очищается (т.к. новая ветка истории)
        /// 2. Команда выполняется
        /// 3. Если запись активна (IsRecording = true), команда добавляется в стек Undo
        /// 4. Отправляется событие о выполнении команды для обновления UI
        /// </remarks>
        public void Execute(ICommand command)
        {
            if (redoStack.Count > 0)
            {
                redoStack.Clear();
            }
            command.Execute();
            if (!IsRecording)
                return;

            undoStack.Push(command);
            _eventBus.Invoke(new ExecuteCommandSignal(command));
        }

        /// <summary>
        /// Отменяет последнее выполненное действие.
        /// </summary>
        /// <remarks>
        /// Процесс отмены:
        /// 1. Извлекаем последнюю команду из стека Undo
        /// 2. Вызываем её метод Undo() для отката изменений
        /// 3. Помещаем команду в стек Redo для возможного повтора
        /// 4. Уведомляем подписчиков об отмене команды
        /// </remarks>
        public void Undo()
        {
            if (undoStack.Count == 0) return;

            var cmd = undoStack.Pop();
            cmd.Undo();
            redoStack.Push(cmd);
            _eventBus.Invoke(new UndoneCommandSignal(cmd));
        }

        /// <summary>
        /// Повторяет ранее отмененное действие.
        /// </summary>
        /// <remarks>
        /// Процесс повтора:
        /// 1. Извлекаем команду из стека Redo
        /// 2. Выполняем её снова
        /// 3. Возвращаем команду в стек Undo
        /// 4. Уведомляем подписчиков о выполнении команды
        /// </remarks>
        public void Redo()
        {
            if (redoStack.Count == 0) return;

            var cmd = redoStack.Pop();
            cmd.Execute();
            undoStack.Push(cmd);
            _eventBus.Invoke(new ExecuteCommandSignal(cmd));
        }

        /// <summary>
        /// Устанавливает состояние записи действий.
        /// </summary>
        /// <param name="value">True - включить запись, False - выключить</param>
        private void SetRecording(bool value)
        {
            IsRecording = value;
        }

        /// <summary>
        /// Начинает внешнюю операцию, которая не должна записываться в историю.
        /// Приостанавливает запись и очищает историю.
        /// Используется для операций типа "загрузка сцены".
        /// </summary>
        public void BeginExternalOperation()
        {
            SetRecording(false);
            ClearHistory();
        }

        /// <summary>
        /// Завершает внешнюю операцию.
        /// Возобновляет запись действий в историю.
        /// </summary>
        public void EndExternalOperation()
        {
            SetRecording(true);
        }

        /// <summary>
        /// Полностью очищает историю действий (оба стека).
        /// Полезно при перезагрузке сцены или сбросе состояния.
        /// </summary>
        public void ClearHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}
