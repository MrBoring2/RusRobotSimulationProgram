using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class RemovePLCCommandCommand : ICommand, IDestructiveCommand
    {
        private PLCBase _removedItem;
        private IList<PLCBase> _sourceList;
        private int _removedIndex;
        private SceneObjectsManager _sceneObjectManager;

        public RemovePLCCommandCommand(PLCBase item, IList<PLCBase> sourceList)
        {
            _removedItem = item;
            _sourceList = sourceList;
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        }

        public void Execute()
        {
            if (_removedItem == null) return;

            _removedIndex = _sourceList.IndexOf(_removedItem);

            if (_removedIndex >= 0)
            {
                _sourceList.RemoveAt(_removedIndex);
            }
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
