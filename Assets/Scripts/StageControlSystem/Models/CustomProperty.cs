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
        public Dictionary<Type, Attribute> Attributes { get; } = new();

        public CustomProperty(string name, string displayName, Type type,
        Func<object> getter, Action<object> setter)
        {
            Name = name;
            DisplayName = displayName;
            PropertyType = type;
            Getter = getter;
            Setter = setter;
        }
        public CustomProperty WithAttribute<T>(T attribute) where T : Attribute
        {
            Attributes[typeof(T)] = attribute;
            return this;
        }
        public bool TryGetAttribute<T>(out T attribute) where T : Attribute
        {
            if (Attributes.TryGetValue(typeof(T), out var attr))
            {
                attribute = (T)attr;
                return true;
            }
            attribute = null;
            return false;
        }
    }

    public class ButtonProperty : CustomProperty
    {
        public Action OnClick { get; set; }
        public string ButtonText { get; set; }

        public ButtonProperty(string name, string displayName, string buttonText, Action onClick)
            : base(name, displayName, typeof(object), () => null, val => { })
        {
            ButtonText = buttonText;
            OnClick = onClick;
        }
    }
}
