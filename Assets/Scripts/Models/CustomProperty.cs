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
        public Type PropertyType;
        public Func<object> Getter;
        public Action<object> Setter;

        public CustomProperty(string name, Type type, Func<object> getter, Action<object> setter)
        {
            Name = name;
            PropertyType = type;
            Getter = getter;
            Setter = setter;
        }
    }
}
