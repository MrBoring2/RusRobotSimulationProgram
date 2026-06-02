using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class RemoveObjectCommand : ICommand, IDestructiveCommand
    {
        private SceneObjectsManager _sceneObjectManager;
        private SceneObject instance;
        private string _parentId;
        private int _originalIndex;
        private bool _wasRemoved;

        // Определяем тип списка для восстановления
        private enum StorageType
        {
            Items,           // Основной список объектов
            Commands,        // Список команд роботов
            PLC              // Список PLC команд
        }
        private StorageType _storageType;

        // Дополнительные данные для восстановления в зависимости от типа
        private string _robotId;           // Для команд - ID робота
        private string _programId;         // Для команд - ID программы
        private PLCBase _plcItem;          // Для PLC - сам элемент
        private IList<PLCBase> _plcList;   // Для PLC - список, в котором был элемент
        private int _plcIndex;             // Для PLC - индекс в списке

        public SceneObject Instance => instance;

        public RemoveObjectCommand(SceneObject instance)
        {
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
            this.instance = instance;
            _wasRemoved = false;

            // Определяем тип хранилища
            DetermineStorageType();
        }

        private void DetermineStorageType()
        {
            if (instance == null) return;

            // Проверяем, является ли объект командой робота
            if (instance is CommandObject || instance is RobotProgramObject)
            {
                _storageType = StorageType.Commands;

                // Находим робота и программу для команды
                foreach (var robot in _sceneObjectManager.GetGameObjectsList())
                {
                    if (robot.Type != ObjectType.Robot) continue;

                    var programs = _sceneObjectManager.Commands.GetSubPrograms(robot.Id);
                    foreach (var program in programs)
                    {
                        if (program.Id == instance.Id)
                        {
                            _robotId = robot.Id;
                            _programId = instance.ParentId;
                            break;
                        }

                        if (program.Items.Contains(instance))
                        {
                            _robotId = robot.Id;
                            _programId = program.Id;
                            _originalIndex = program.Items.IndexOf((CommandObject)instance);
                            break;
                        }
                    }
                    if (_robotId != null) break;
                }
            }
            else
            {
                _storageType = StorageType.Items;
            }
        }

        public void Execute()
        {
            if (instance == null || instance.Reference == null)
                throw new Exception("Объекта не существует");

            _parentId = instance.ParentId;

            switch (_storageType)
            {
                case StorageType.Items:
                    // Сохраняем индекс в OrderedDictionary
                    _originalIndex = OrderedDictionaryExtensions.IndexOf(_sceneObjectManager.Items, instance.Id);
                    _sceneObjectManager.Remove(instance.Id, false);
                    break;

                case StorageType.Commands:
                    // Удаляем команду из программы
                    if (!string.IsNullOrEmpty(_robotId) && !string.IsNullOrEmpty(_programId))
                    {
                        var program = _sceneObjectManager.Commands.GetSubProgram(_programId);
                        if (program != null && program.Items.Contains(instance))
                        {
                            _originalIndex = program.Items.IndexOf((CommandObject)instance);
                            program.Items.Remove((CommandObject)instance);
                        }
                    }
                    // Скрываем GameObject
                    if (instance.Reference != null)
                    {
                        instance.Reference.SetActive(false);
                    }
                    break;
            }

            _wasRemoved = true;
        }

        public void Undo()
        {
            if (!_wasRemoved || instance == null || instance.Reference == null)
                return;

            // Восстанавливаем GameObject
            instance.Reference.SetActive(true);

            switch (_storageType)
            {
                case StorageType.Items:
                    // Восстанавливаем в основной список
                    if (!_sceneObjectManager.Items.Contains(instance.Id))
                    {
                        if (_originalIndex >= 0 && _originalIndex <= _sceneObjectManager.Items.Count)
                        {
                            _sceneObjectManager.Items.Insert(_originalIndex, instance.Id, instance);
                        }
                        else
                        {
                            _sceneObjectManager.Items.Add(instance.Id, instance);
                        }

                        // Восстанавливаем родительскую связь
                        RestoreParentRelationship();

                        // Отправляем сигналы
                        var eventBus = ServiceManager.Current.Get<EventBus>();
                        eventBus.Invoke(new AddSceneObjectSignal(instance));
                        eventBus.Invoke(new UpdateLineDrawer());
                    }
                    break;

                case StorageType.Commands:
                    // Восстанавливаем команду в программу
                    if (!string.IsNullOrEmpty(_robotId) && !string.IsNullOrEmpty(_programId))
                    {
                        var program = _sceneObjectManager.Commands.GetSubProgram(_programId);
                        if (program != null)
                        {
                            if (_originalIndex >= 0 && _originalIndex <= program.Items.Count)
                            {
                                program.Items.Insert(_originalIndex, (CommandObject)instance);
                            }
                            else
                            {
                                program.Items.Add((CommandObject)instance);
                            }

                            // Восстанавливаем родительскую связь в Transform
                            if (program.Reference != null)
                            {
                                instance.Reference.transform.SetParent(program.Reference.transform, false);
                            }

                            // Отправляем сигналы
                            var eventBus = ServiceManager.Current.Get<EventBus>();
                            eventBus.Invoke(new AddSceneObjectSignal(instance));
                            eventBus.Invoke(new UpdateLineDrawer());
                        }
                    }
                    break;
            }

            _wasRemoved = false;
        }

        private void RestoreParentRelationship()
        {
            if (!string.IsNullOrEmpty(_parentId) && _sceneObjectManager.Items.Contains(_parentId))
            {
                var parent = ((SceneObject)_sceneObjectManager.Items[_parentId])?.Reference;
                if (parent != null)
                {
                    instance.Reference.transform.SetParent(parent.transform, false);
                    instance.SetParent(_parentId);
                }
            }
        }

        public void FinalizeDestroy()
        {
            if (instance != null && _wasRemoved)
            {
                // Окончательное уничтожение GameObject
                if (instance.Reference != null)
                {
                    UnityEngine.Object.Destroy(instance.Reference);
                }

                // Удаляем из соответствующего списка если еще не удален
                switch (_storageType)
                {
                    case StorageType.Items:
                        if (_sceneObjectManager.Items.Contains(instance.Id))
                        {
                            _sceneObjectManager.Items.Remove(instance.Id);
                        }
                        break;

                    case StorageType.Commands:
                        if (!string.IsNullOrEmpty(_robotId) && !string.IsNullOrEmpty(_programId))
                        {
                            var program = _sceneObjectManager.Commands.GetSubProgram(_programId);
                            if (program != null && program.Items.Contains(instance))
                            {
                                program.Items.Remove((CommandObject)instance);
                            }
                        }
                        break;
                }
            }
        }
    }
}
