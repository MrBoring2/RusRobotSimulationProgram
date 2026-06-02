using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class RemovePLCInitVariableCommand : ICommand, IDestructiveCommand
    {
        private PLCInitVariable _variable;
        private int _index;
        private SceneObjectsManager _manager;

        public RemovePLCInitVariableCommand(PLCInitVariable variable, int index)
        {
            _variable = variable;
            _index = index;
            _manager = ServiceManager.Current.Get<SceneObjectsManager>();
        }

        public void Execute()
        {
            if (_index >= 0 && _index < _manager.PLCData.InitBlockItems.Count)
                _manager.PLCData.InitBlockItems.RemoveAt(_index);
        }

        public void Undo()
        {
            if (_index >= 0 && _index <= _manager.PLCData.InitBlockItems.Count)
                _manager.PLCData.InitBlockItems.Insert(_index, _variable);
            else
                _manager.PLCData.InitBlockItems.Add(_variable);
        }

        public void FinalizeDestroy()
        {
            _variable = null;
        }
    }
}
