using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class CustomProperty
    {
        public string Name;
        public string DisplayName;
        public Type PropertyType;
        public Func<object> Getter;
        public Action<object> Setter;

        public CustomProperty(string name, string displayName, Type type, Func<object> getter, Action<object> setter)
        {
            Name = name;
            DisplayName = displayName;
            PropertyType = type;
            Getter = getter;
            Setter = setter;
        }
    }
}
