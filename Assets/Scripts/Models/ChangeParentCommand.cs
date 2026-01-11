using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
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
    public class ChangeParentCommand : ICommand
    {
        private readonly SceneObjectsManager _sceneObjectsManager;
        private string _objectId;
        private string _newParentId;
        private int? _insertIndex;

        // Для Undo
        private string _oldParentId;
        private int _oldIndexInParent;
        private int _oldIndexInDictionary;

        public ChangeParentCommand(string objectId, string newParentId, int? insertIndex)
        {
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _objectId = objectId;
            _newParentId = newParentId;
            _insertIndex = insertIndex;
        }

        public void Execute()
        {
            var sceneObject = _sceneObjectsManager.GetById(_objectId);
            if (sceneObject == null) return;

            _oldParentId = sceneObject.ParentId;
            _oldIndexInParent = _sceneObjectsManager.GetSiblingIndex(_objectId, _oldParentId);
            _oldIndexInDictionary = OrderedDictionaryExtensions.IndexOf(_sceneObjectsManager.Items, _objectId);

            _sceneObjectsManager.ChangeObjectOrder(_objectId, _newParentId, _insertIndex);
            UpdateSceneHierarchy(sceneObject, _insertIndex);

            var eventBus = ServiceManager.Current.Get<EventBus>();
            eventBus.Invoke(new UpdateHierarchySignal());
        }

        public void Undo()
        {

            _sceneObjectsManager.ChangeObjectOrder(_objectId, _oldParentId, _oldIndexInParent);
            UpdateSceneHierarchy(_sceneObjectsManager.GetById(_objectId), _insertIndex);

            var eventBus = ServiceManager.Current.Get<EventBus>();
            eventBus.Invoke(new UpdateHierarchySignal());
        }

        private void UpdateSceneHierarchy(SceneObject obj, int? insertIndex)
        {
            GameObject parentObj = null;

            // Находим родительский объект
            if (!string.IsNullOrEmpty(obj.ParentId))
            {
                var parent = _sceneObjectsManager.GetById(obj.ParentId);
                parentObj = parent?.Reference;
            }

            // Устанавливаем родителя
            obj.Reference.transform.SetParent(parentObj?.transform, false);

            // Устанавливаем правильную позицию среди детей
            if (parentObj != null && insertIndex.HasValue)
            {
                // Получаем текущих детей (уже с учетом добавленного объекта)
                int childCount = parentObj.transform.childCount;

                // Корректируем индекс: если перемещаем объект вниз по списку, нужно учесть,
                // что он временно удален из списка детей
                int oldSiblingIndex = GetCurrentSiblingIndex(obj.Reference.transform);
                int newSiblingIndex = insertIndex.Value;

                // Если перемещаем вниз по списку, уменьшаем целевой индекс на 1
                if (newSiblingIndex > oldSiblingIndex && oldSiblingIndex != -1)
                {
                    newSiblingIndex--;
                }

                // Ограничиваем индекс допустимыми значениями
                newSiblingIndex = Mathf.Clamp(newSiblingIndex, 0, Mathf.Max(0, childCount - 1));

                // Устанавливаем позицию
                obj.Reference.transform.SetSiblingIndex(newSiblingIndex);
            }
            else if (parentObj != null)
            {
                // Если индекс не указан - ставим в конец
                obj.Reference.transform.SetAsLastSibling();
            }
        }

        private int GetCurrentSiblingIndex(Transform transform)
        {
            if (transform.parent == null) return -1;

            for (int i = 0; i < transform.parent.childCount; i++)
            {
                if (transform.parent.GetChild(i) == transform)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetObjectIndex(string objectId)
        {
            var items = _sceneObjectsManager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items.Cast<System.Collections.DictionaryEntry>().ElementAt(i).Key.ToString() == objectId)
                {
                    return i;
                }
            }
            return items.Count;
        }
    }
}
