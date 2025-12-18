using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class PropertyChangeCommand : ICommand
    {
        private readonly object target;
        private readonly PropertyInfo propertyInfo;
        private readonly object before;
        private readonly object after;
        public object Target => target;

        public PropertyChangeCommand(object target, string propertyName, object before, object after)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

            this.target = target;
            this.propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null)
                throw new ArgumentException($"Свойство '{propertyName}' не найдено у объекта {target.GetType().Name}");

            this.before = before;
            this.after = after;
        }

        public void Execute()
        {
            propertyInfo.SetValue(target, after);
        }

        public void Undo()
        {
            propertyInfo.SetValue(target, before);
        }
    }
}
