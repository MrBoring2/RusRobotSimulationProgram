using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class RemoveELIFCommand : ICommand, IDestructiveCommand
    {
        private PLCCondition _elif;
        private PLCBlockCondition _block;
        private int _index;

        public RemoveELIFCommand(PLCCondition elif, PLCBlockCondition block)
        {
            _elif = elif;
            _block = block;
        }

        public void Execute()
        {
            if (_elif == null || _block == null) return;
            _index = _block.ElifConditions.IndexOf(_elif);
            if (_index >= 0)
                _block.ElifConditions.RemoveAt(_index);
        }

        public void Undo()
        {
            if (_elif == null || _block == null) return;
            if (_block.ElifConditions.Contains(_elif)) return;
            if (_index >= 0 && _index <= _block.ElifConditions.Count)
                _block.ElifConditions.Insert(_index, _elif);
            else
                _block.ElifConditions.Add(_elif);
        }

        public void FinalizeDestroy()
        {
            _elif = null;
            _block = null;
        }
    }
}
