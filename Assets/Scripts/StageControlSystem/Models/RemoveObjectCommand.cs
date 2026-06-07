using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Lines;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Utils;
using System;
using System.Linq;
using UnityEngine;

public class RemoveObjectCommand : ICommand, IDestructiveCommand
{
    private SceneObjectsManager _sceneObjectManager;
    private SceneObject instance;
    private string _parentId;
    private int _originalIndex;
    private bool _wasRemoved;
    private GameObject _originalParent;
    private int _siblingIndex;

    public SceneObject Instance => instance;

    public RemoveObjectCommand(SceneObject instance)
    {
        _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        this.instance = instance;
        _wasRemoved = false;
    }

    public void Execute()
    {
        if (instance == null || instance.Reference == null)
            throw new Exception("Объекта не существует");

        _parentId = instance.ParentId;
        _originalParent = instance.Reference.transform.parent?.gameObject;
        _siblingIndex = instance.Reference.transform.GetSiblingIndex();

        // Сохраняем индекс в OrderedDictionary
        _originalIndex = OrderedDictionaryExtensions.IndexOf(_sceneObjectManager.Items, instance.Id);

        // НЕ УДАЛЯЕМ из коллекций, только деактивируем и скрываем
        instance.Reference.SetActive(false);

        // Опционально: перемещаем в специальный контейнер "Deleted"
        var deletedContainer = GameObject.Find("DeletedObjects");
        if (deletedContainer != null)
        {
            instance.Reference.transform.SetParent(deletedContainer.transform, false);
        }

        // Убираем из активных списков (но не удаляем полностью)
        _sceneObjectManager.Items.Remove(instance.Id);

        // Для команд - тоже удаляем из активных списков
        if (instance is CommandObject command)
        {
            var program = GetProgramContainingCommand(command);
            if (program != null)
            {
                program.Items.Remove(command);
            }
        }
        else if (instance is RobotProgramObject program)
        {
            // Удаляем программу из активных списков робота
            foreach (var robot in _sceneObjectManager.GetGameObjectsList())
            {
                if (robot.Type != ObjectType.Robot) continue;
                _sceneObjectManager.Commands.GetSubPrograms(robot.Id).Remove(program);
                break;
            }
        }

        _wasRemoved = true;

        // Отправляем сигнал об удалении
        var eventBus = ServiceManager.Current.Get<EventBus>();
        eventBus.Invoke(new RemoveSceneObjectSignal(instance));
        eventBus.Invoke(new UpdateLineDrawer());
    }

    public void Undo()
    {
        if (!_wasRemoved || instance == null || instance.Reference == null)
            return;

        // Восстанавливаем GameObject
        instance.Reference.SetActive(true);

        // Восстанавливаем родителя
        if (_originalParent != null)
        {
            instance.Reference.transform.SetParent(_originalParent.transform, false);
            instance.Reference.transform.SetSiblingIndex(_siblingIndex);
        }

        // Восстанавливаем в коллекции
        if (_originalIndex >= 0 && _originalIndex <= _sceneObjectManager.Items.Count)
        {
            _sceneObjectManager.Items.Insert(_originalIndex, instance.Id, instance);
        }
        else
        {
            _sceneObjectManager.Items.Add(instance.Id, instance);
        }

        // Восстанавливаем команды
        if (instance is CommandObject command)
        {
            var program = GetProgramById(command.ParentId);
            if (program != null && !program.Items.Contains(command))
            {
                program.Items.Add(command);
            }
        }
        else if (instance is RobotProgramObject program)
        {
            // Находим робота и восстанавливаем программу
            foreach (var robot in _sceneObjectManager.GetGameObjectsList())
            {
                if (robot.Type != ObjectType.Robot) continue;
                var programs = _sceneObjectManager.Commands.GetSubPrograms(robot.Id);
                if (!programs.Contains(program))
                {
                    programs.Add(program);
                }
                break;
            }
        }

        // Восстанавливаем родительскую связь в модели
        if (!string.IsNullOrEmpty(_parentId))
        {
            instance.SetParent(_parentId);
        }

        _wasRemoved = false;

        // Отправляем сигнал о восстановлении
        var eventBus = ServiceManager.Current.Get<EventBus>();
        eventBus.Invoke(new AddSceneObjectSignal(instance));
        eventBus.Invoke(new UpdateLineDrawer());
    }

    public void FinalizeDestroy()
    {
        // ТОЛЬКО здесь реально уничтожаем объект (когда Undo уже невозможен)
        if (instance != null && instance.Reference != null)
        {
            UnityEngine.Object.Destroy(instance.Reference);
        }
    }

    private RobotProgramObject GetProgramContainingCommand(CommandObject command)
    {
        foreach (var robot in _sceneObjectManager.GetGameObjectsList())
        {
            if (robot.Type != ObjectType.Robot) continue;

            var programs = _sceneObjectManager.Commands.GetSubPrograms(robot.Id);
            foreach (var program in programs)
            {
                if (program.Items.Contains(command))
                    return program;
            }
        }
        return null;
    }

    private RobotProgramObject GetProgramById(string programId)
    {
        foreach (var robot in _sceneObjectManager.GetGameObjectsList())
        {
            if (robot.Type != ObjectType.Robot) continue;

            var programs = _sceneObjectManager.Commands.GetSubPrograms(robot.Id);
            var program = programs.FirstOrDefault(p => p.Id == programId);
            if (program != null)
                return program;
        }
        return null;
    }
}