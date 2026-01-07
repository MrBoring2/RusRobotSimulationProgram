using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class ChangeParentCommand : ICommand
    {
        private readonly SceneObjectsManager _sceneObjectsManager;
        private string _objectId;
        private string _newParentId;
        private string _oldParentId;
        private int? _insertAtIndex;
        private int _oldIndex;

        public ChangeParentCommand(string objectId, string newParentId, int? insertAtIndex = null)
        {
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _objectId = objectId;
            _newParentId = newParentId;

            var obj = _sceneObjectsManager.GetById(objectId);
            _oldParentId = obj?.ParentId;
            _oldIndex = GetObjectIndex(objectId);
            _insertAtIndex = insertAtIndex;
        }

        public void Execute()
        {
            var obj = _sceneObjectsManager.GetById(_objectId);
            if (obj == null) return;

            // Сохраняем старые данные для Undo
            var oldParentId = obj.ParentId;
            var oldIndex = _oldIndex;

            // Меняем parentId
            obj.SetParent(_newParentId);

            // Получаем OrderedDictionary для манипуляций
            var items = _sceneObjectsManager.Items;

            // Удаляем объект из текущей позиции
            items.Remove(_objectId);

            // Вставляем на новую позицию
            if (_insertAtIndex.HasValue && _insertAtIndex.Value < items.Count)
            {
                // Вставляем по индексу
                items.Insert(_insertAtIndex.Value, _objectId, obj);
            }
            else
            {
                // Добавляем в конец
                items.Add(_objectId, obj);
            }

            // Обновляем иерархию в сцене
            UpdateSceneHierarchy(obj);

            // Сохраняем новые данные для Redo
            _oldParentId = oldParentId;
            _oldIndex = oldIndex;

            // Обновляем UI
            var eventBus = ServiceManager.Current.Get<EventBus>();
            eventBus.Invoke(new UpdateHierarchySignal());
        }

        public void Undo()
        {
            var obj = _sceneObjectsManager.GetById(_objectId);
            if (obj == null) return;

            // Восстанавливаем старый parentId
            obj.SetParent(_oldParentId);

            // Получаем OrderedDictionary
            var items = _sceneObjectsManager.Items;

            // Удаляем объект из текущей позиции
            items.Remove(_objectId);

            // Вставляем на старую позицию
            if (_oldIndex < items.Count)
            {
                items.Insert(_oldIndex, _objectId, obj);
            }
            else
            {
                items.Add(_objectId, obj);
            }

            // Восстанавливаем иерархию в сцене
            UpdateSceneHierarchy(obj);

            // Обновляем UI
            var eventBus = ServiceManager.Current.Get<EventBus>();
            eventBus.Invoke(new UpdateHierarchySignal());
        }

        private void UpdateSceneHierarchy(SceneObject obj)
        {
            if (!string.IsNullOrEmpty(obj.ParentId))
            {
                var parent = _sceneObjectsManager.GetById(obj.ParentId);
                if (parent != null)
                {
                    obj.Reference.transform.SetParent(parent.Reference.transform, false);
                    obj.Reference.transform.SetAsLastSibling(); // Помещаем в конец детей
                }
            }
            else
            {
                obj.Reference.transform.SetParent(null, false);
            }
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
