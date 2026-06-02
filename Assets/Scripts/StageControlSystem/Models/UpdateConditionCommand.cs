using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class UpdateConditionCommand : ICommand, IDestructiveCommand
    {
        private PLCCondition _condition;
        private string _oldExpression;
        private string _newExpression;

        public UpdateConditionCommand(PLCCondition condition, string oldExpression, string newExpression)
        {
            _condition = condition;
            _oldExpression = oldExpression;
            _newExpression = newExpression;
        }

        public void Execute()
        {
            if (_condition != null)
                _condition.Expression = _newExpression;
        }

        public void Undo()
        {
            if (_condition != null)
                _condition.Expression = _oldExpression;
        }

        public void FinalizeDestroy()
        {
            _condition = null;
        }
    }
}
