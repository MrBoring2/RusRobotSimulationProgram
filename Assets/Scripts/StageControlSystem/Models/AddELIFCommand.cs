using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class AddELIFCommand : ICommand, IDestructiveCommand
    {
        private PLCCondition _elif;
        private PLCBlockCondition _block;
        private int _index;

        public AddELIFCommand(PLCCondition elif, PLCBlockCondition block)
        {
            _elif = elif;
            _block = block;
        }

        public void Execute()
        {
            if (_elif == null || _block == null) return;
            _block.ElifConditions.Add(_elif);
            _index = _block.ElifConditions.Count - 1;
        }

        public void Undo()
        {
            if (_elif == null || _block == null) return;
            if (_index >= 0 && _index < _block.ElifConditions.Count)
                _block.ElifConditions.RemoveAt(_index);
        }

        public void FinalizeDestroy()
        {
            _elif = null;
            _block = null;
        }
    }
}
