using Assets.Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class AddPLCCommandCommand : ICommand
    {
        private PLCBase _item;
        private IList _targetList;

        public string Description => $"Добавление PLC команды:";

        public AddPLCCommandCommand(PLCBase item, IList targetList)
        {
            _item = item;
            _targetList = targetList;
        }

        public void Execute()
        {
            _targetList.Add(_item);
        }

        public void Undo()
        {
            _targetList.Remove(_item);
        }
    }
}
