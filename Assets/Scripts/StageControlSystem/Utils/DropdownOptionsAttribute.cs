using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Utils
{
    public class DropdownOptionsAttribute : Attribute
    {
        public List<string> Options { get; }
        public List<object> Values { get; }
        public string DisplayProperty { get; set; }
        public DropdownOptionsAttribute(string[] options, object[] values, string displayProperty)
        {
            Options = new List<string>(options);
            Values = new List<object>(values);
            DisplayProperty = displayProperty;
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ButtonActionAttribute : Attribute
    {
        public string ButtonText { get; }
        public Action Action { get; }

        public ButtonActionAttribute(string buttonText)
        {
            ButtonText = buttonText;
        }
    }
}
