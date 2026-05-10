using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Models
{
    public class CustomPropertyChangeCommand : ICommand
    {
        private object _target;
        private string _propertyName;
        private object _oldValue;
        private object _newValue;
        private Action<object> _setter;
        private Func<object> _getter;

        public object Target => _target;

        public CustomPropertyChangeCommand(
            object target,
            string propertyName,
            object oldValue,
            object newValue,
            Action<object> setter,
            Func<object> getter)
        {
            _target = target;
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
            _setter = setter;
            _getter = getter;
        }

        public void Execute()
        {
            _setter?.Invoke(_newValue);
        }

        public void Undo()
        {
            _setter?.Invoke(_oldValue);
        }

        public string GetDescription()
        {
            return $"Изменение '{_propertyName}'";
        }
    }
}
