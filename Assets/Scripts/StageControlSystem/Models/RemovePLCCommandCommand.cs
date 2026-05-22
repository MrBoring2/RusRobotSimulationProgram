using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class RemovePLCCommandCommand : ICommand
    {
        private PLCCommand _command;
        private PLCData _plcData;
        private IList _sourceList;
        private int _index;

        public string Description => $"Удаление PLC команды: {_command.Id}";

        public RemovePLCCommandCommand(PLCCommand command, IList sourceList)
        {
            _command = command;
            _sourceList = sourceList;
        }

        public void Execute()
        {
            _index = _sourceList.IndexOf(_command);
            if (_index >= 0)
                _sourceList.RemoveAt(_index);
        }

        public void Undo()
        {
            if (_index >= 0 && _index <= _sourceList.Count)
                _sourceList.Insert(_index, _command);
        }
    }
}
