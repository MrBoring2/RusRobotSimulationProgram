using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.StageControlSystem.Utils
{
    public class DropdownOptionsAttribute : Attribute
    {
        public List<string> Options { get; }
        public List<object> Values { get; }

        public DropdownOptionsAttribute(string[] options, object[] values)
        {
            Options = new List<string>(options);
            Values = new List<object>(values);
        }
    }
}
