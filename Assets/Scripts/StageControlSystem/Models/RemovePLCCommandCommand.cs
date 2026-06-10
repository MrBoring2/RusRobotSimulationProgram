using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class RemovePLCCommandCommand : ICommand, IDestructiveCommand
    {
        private PLCBase _removedItem;
        private List<PLCBase> _sourceList;
        private int _removedIndex;
        private SceneObjectsManager _sceneObjectManager;

        public RemovePLCCommandCommand(PLCBase item, List<PLCBase> sourceList)
        {
            _removedItem = item;
            _sourceList = sourceList;
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        }
        public void Execute()
        {
            if (_removedItem == null) return;

            // Ищем в каком списке находится элемент (может быть во вложенных Content)
            var actualList = FindListContainingItem(_sourceList, _removedItem.Id);
            if (actualList == null) actualList = _sourceList;

            _removedIndex = -1;
            for (int i = 0; i < actualList.Count; i++)
            {
                if (actualList[i] != null && actualList[i].Id == _removedItem.Id)
                {
                    _removedIndex = i;
                    _sourceList = actualList; // Запоминаем реальный список для Undo
                    break;
                }
            }

            if (_removedIndex >= 0)
            {
                actualList.RemoveAt(_removedIndex);
            }
        }

        private List<PLCBase> FindListContainingItem(List<PLCBase> items, string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].Id == id)
                    return items;

                if (items[i] is PLCBlockCondition block)
                {
                    var found = FindListContainingItem(block.IfCondition.Content, id);
                    if (found != null) return found;
                    foreach (var elif in block.ElifConditions)
                    {
                        found = FindListContainingItem(elif.Content, id);
                        if (found != null) return found;
                    }
                    if (block.ElseCondition != null)
                    {
                        found = FindListContainingItem(block.ElseCondition.Content, id);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        public void Undo()
        {
            if (_removedItem == null || _sourceList == null) return;

            if (_sourceList.Contains(_removedItem)) return;

            if (_removedIndex >= 0 && _removedIndex <= _sourceList.Count)
            {
                _sourceList.Insert(_removedIndex, _removedItem);
            }
            else
            {
                _sourceList.Add(_removedItem);
            }
        }

        public void FinalizeDestroy()
        {
            if (_removedItem != null && _sourceList != null && _sourceList.Contains(_removedItem))
            {
                _sourceList.Remove(_removedItem);
            }

            _removedItem = null;
            _sourceList = null;
        }
    }
}
